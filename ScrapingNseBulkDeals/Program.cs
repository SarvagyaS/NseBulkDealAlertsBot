using Azure.Core;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrapingNseBulkDeals;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Text.Json;

class Program
{
    public static string _filePath = "D:\\Practice\\ScrapingNseBulkDeals\\chat_subscribers.json";
    public static string _botToken = "8338715441:AAHzWg01ulY9BP-FBZjGdk7JfgUH_FEg49o";

    static void Main(string[] args)
    {
        var deals = GetDeals();
        SaveDeals(deals);
        NotifyUsers();
    }

    public static List<BulkDeal> GetDeals() {
        var deals = new List<BulkDeal>();

        var options = new ChromeOptions();
        options.AddArgument("--headless"); // Optional

        using (IWebDriver driver = new ChromeDriver(options))
        {
            try
            {
                driver.Navigate().GoToUrl("https://www.chittorgarh.com/report/stock-nse-bulk-deals/119/");

                // Wait for the table to be visible
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("table.table-bordered")));

                // Get all tables with .table-bordered class
                var tables = driver.FindElements(By.CssSelector("table.table-bordered"));

                // The correct table is usually the second one (index 1), first is header/explanation
                var table = tables[0];

                var rows = table.FindElements(By.TagName("tbody"))[0].FindElements(By.TagName("tr"));

                // Skip header row
                for (int i = 1; i < rows.Count; i++)
                {
                    var cells = rows[i].FindElements(By.TagName("td"));

                    if (cells.Count == 8)
                    {
                        var deal = new BulkDeal
                        {
                            SecurityName = cells[0].Text.Trim(),
                            Symbol = cells[1].Text.Trim(),
                            ClientName = cells[2].Text.Trim(),
                            DealType = cells[3].Text.Trim(),
                            Quantity = cells[4].Text.Trim(),
                            Price = cells[5].Text.Trim(),
                            TradedDate = cells[6].Text.Trim(),
                        };

                        deals.Add(deal);
                    }
                }

                // Output result
                Console.WriteLine("Extracted Deals:\n");
                foreach (var deal in deals)
                {
                    Console.WriteLine($"{deal.TradedDate} | {deal.SecurityName} | {deal.ClientName} | {deal.DealType} | {deal.Quantity} | {deal.Price}");
                }

                Console.WriteLine($"\nTotal deals: {deals.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        return deals;
    }



    public static void SaveDeals(List<BulkDeal> scrapedDeals)
    {
        using (var db = new BulkDealContext())
        {
            db.Database.EnsureCreated(); // Create DB if it doesn't exist

            foreach (var deal in scrapedDeals)
            {
                bool exists = db.Deals.Any(d =>
                    d.TradedDate == deal.TradedDate &&
                    d.SecurityName == deal.SecurityName &&
                    d.ClientName == deal.ClientName &&
                    d.DealType == deal.DealType &&
                    d.Quantity == deal.Quantity &&
                    d.Price == deal.Price);

                if (!exists)
                {

                    db.Deals.Add(deal);
                    Console.WriteLine($"Added: {deal.SecurityName} on {deal.TradedDate}");
                }
                else
                {
                    Console.WriteLine($"Skipped duplicate: {deal.SecurityName} on {deal.TradedDate}");
                }
            }

            db.SaveChanges();
        }
    }

    public static List<BulkDeal> AnalyseData()
    {
        var db = new BulkDealContext();
        var allDeals = db.Deals.GroupBy(c => c.SecurityName).ToDictionary(a => a.Key, a => a.ToList());
        var dealsToSend = new List<BulkDeal>();
        foreach (var item in allDeals)
        {
            var buyCount = item.Value.Where(a => a.DealType == "BUY" && a.CreatedOn >= DateTime.Now.AddDays(-5)).ToList();
            var sellCount = item.Value.Where(a => a.DealType == "SELL" && a.CreatedOn >= DateTime.Now.AddDays(-5)).ToList();
            if (buyCount?.Count - sellCount?.Count > 4)
            {
                dealsToSend.Add(item.Value.FirstOrDefault());
            }
        }
        return dealsToSend;
    }


    public static void NotifyUsers()
    {
        CollectSubscribersAsync().Wait();
        var subscribers = LoadSubscribers();
        var dealsToSend = AnalyseData();

        string htmlMessage = FormatDealsAsHtml(dealsToSend);

        foreach (var user in subscribers)
        {
            SendHtmlMessageAsync(user.ChatId, htmlMessage).Wait();
            Console.WriteLine($"✅ Sent message to: {user.FirstName} (@{user.Username})");
        }

    }

    public static string FormatDealsAsHtml(List<BulkDeal> deals)
    {
        if (deals == null || deals.Count == 0)
            return "<b>No new bulk deals found.</b>";

        string html = "<b>🚨 New NSE Bulk Deals</b>\n\n";

        foreach (var deal in deals)
        {
            html += $"<b>Company:</b> {Escape(deal.SecurityName)}\n" +
                    $"<b>Type:</b> {Escape(deal.DealType)}\n" +
                    $"<b>Date:</b> {Escape(deal.TradedDate)}\n" +
                    $"<b>Client:</b> {Escape(deal.ClientName)}\n" +
                    $"<b>Qty:</b> {Escape(deal.Quantity)} @ <b>₹{Escape(deal.Price)}</b>\n\n";
        }

        return html;
    }

    private static string Escape(string input)
    {
        // HTML-escape user content
        return System.Net.WebUtility.HtmlEncode(input ?? "");
    }
    public static List<TelegramUser> LoadSubscribers()
    {
        if (!File.Exists(_filePath)) return new List<TelegramUser>();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<TelegramUser>>(json) ?? new List<TelegramUser>();
    }


    public static async Task SendHtmlMessageAsync(string chatId, string htmlMessage)
    {
        using (var http = new HttpClient())
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage" +
                      $"?chat_id={chatId}&text={Uri.EscapeDataString(htmlMessage)}&parse_mode=HTML";

            var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Failed to send message to {chatId}. Status: {response.StatusCode}");
            }
        }
    }

    public static async Task CollectSubscribersAsync()
    {

        using (var http = new HttpClient())
        {
            var url = $"https://api.telegram.org/bot{_botToken}/getUpdates";

            var response = await http.GetStringAsync(url);

            using JsonDocument jsonDoc = JsonDocument.Parse(response);
            var updates = jsonDoc.RootElement.GetProperty("result");

            var existing = LoadExistingSubscribers();

            foreach (var update in updates.EnumerateArray())
            {
                if (!update.TryGetProperty("message", out var message)) continue;
                if (!message.TryGetProperty("chat", out var chat)) continue;

                string chatId = chat.GetProperty("id").ToString();
                string firstName = chat.GetProperty("first_name").GetString() ?? "";
                string username = chat.TryGetProperty("username", out var uname) ? uname.GetString() ?? "" : "";

                var user = new TelegramUser
                {
                    ChatId = chatId,
                    FirstName = firstName,
                    Username = username
                };

                if (!existing.Any(u => u.ChatId == chatId))
                {
                    existing.Add(user);
                    Console.WriteLine($"✅ Added: {firstName} ({username}) [{chatId}]");
                }
            }

            SaveSubscribers(existing);
        }
    }

    private static List<TelegramUser> LoadExistingSubscribers()
    {
        if (!File.Exists(_filePath)) return new List<TelegramUser>();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<TelegramUser>>(json) ?? new List<TelegramUser>();
    }

    private static void SaveSubscribers(List<TelegramUser> users)
    {
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }


}
