using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using ScrapingNseBulkDeals;
using SeleniumExtras.WaitHelpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

class Program
{
    //public static string _filePath = "D:\\Practice\\ScrapingNseBulkDeals\\chat_subscribers.json";
    public static string _filePath = Path.Combine(AppContext.BaseDirectory, "Json", "chat_subscribers.json");
    public static string _fileDownloadPath = Path.Combine(AppContext.BaseDirectory,"Downloads");
    public static string _botToken = "8338715441:AAHzWg01ulY9BP-FBZjGdk7JfgUH_FEg49o";

    static void Main(string[] args)
    {
        var deals = GetDeals();
        SaveDeals(deals);
        NotifyUsers();
    }

    public static List<BulkDeal> GetDeals() {
        var deals = new List<BulkDeal>();
        var chromeOptions = new ChromeOptions();

        //chromeOptions.AddArgument("--headless"); // Optional
        chromeOptions.AddUserProfilePreference("download.default_directory", _fileDownloadPath);
        chromeOptions.AddUserProfilePreference("intl.accept_languages", "nl");
        chromeOptions.AddUserProfilePreference("disable-popup-blocking", "true");
        //chromeOptions.AddArgument("--headless");  // Optional: Run in headless mode (no UI)
        chromeOptions.AddArgument("--disable-blink-features=AutomationControlled");
        chromeOptions.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        using (IWebDriver driver = new ChromeDriver(chromeOptions))
        {
            try
            {
                //driver.Navigate().GoToUrl("https://www.chittorgarh.com/report/stock-nse-bulk-deals/119/");
                //// Wait for the table to be visible
                //wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("table.table-bordered")));

                //// Get all tables with .table-bordered class
                //var tables = driver.FindElements(By.CssSelector("table.table-bordered"));

                //// The correct table is usually the second one (index 1), first is header/explanation
                //var table = tables[0];

                //var rows = table.FindElements(By.TagName("tbody"))[0].FindElements(By.TagName("tr"));

                //// Skip header row
                //for (int i = 1; i < rows.Count; i++)
                //{
                //    var cells = rows[i].FindElements(By.TagName("td"));

                //    if (cells.Count == 8)
                //    {
                //        var deal = new BulkDeal
                //        {
                //            SecurityName = cells[0].Text.Trim('"', ' ', '\t'),
                //            Symbol = cells[1].Text.Trim('"', ' ', '\t'),
                //            ClientName = cells[2].Text.Trim('"', ' ', '\t'),
                //            DealType = cells[3].Text.Trim('"', ' ', '\t'),
                //            Quantity = cells[4].Text.Trim('"', ' ', '\t'),
                //            Price = cells[5].Text.Trim('"', ' ', '\t'),
                //            TradedDate = cells[6].Text.Trim('"', ' ', '\t'),
                //        };

                //        deals.Add(deal);
                //    }
                //}

                //// Output result
                //Console.WriteLine("Extracted Deals:\n");
                //foreach (var deal in deals)
                //{
                //    Console.WriteLine($"{deal.TradedDate} | {deal.SecurityName} | {deal.ClientName} | {deal.DealType} | {deal.Quantity} | {deal.Price}");
                //}
               
                
                
                driver.Navigate().GoToUrl("https://www.nseindia.com/report-detail/display-bulk-and-block-deals");
                // Scroll down slowly to make it appear more human
                IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                for (int i = 0; i < 3; i++)
                {
                    js.ExecuteScript("window.scrollBy(0, 500);");
                    Thread.Sleep(new Random().Next(2000, 4000));  // Random delay to simulate human behavior
                }

                var LeaseID = driver.FindElement(By.Id( "HistBulkBlockDeals-download"));
                driver.ExecuteJavaScript("arguments[0].click();", LeaseID);
                Thread.Sleep(5000);

                DirectoryInfo info = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory) + "\\Downloads");
                var FileDownloaded = info.GetFiles().OrderByDescending(p => p.CreationTime).FirstOrDefault();

                deals = ReadCsvFile(FileDownloaded.FullName);


                foreach (FileInfo file in info.GetFiles())
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        return deals;
    }

    public static List<BulkDeal> ReadCsvFile(string filePath)
    {
        var bulkDeals = new List<BulkDeal>();

        // Ensure the file exists
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found!");
            return bulkDeals;
        }

        try
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                int id = 1; // To generate sequential Id for each row

                // Read each line from the CSV file
                while ((line = reader.ReadLine()) != null)
                {
                    if (id != 1)
                    {
                        // Split the line by commas (assuming no commas within values)
                        var values = line.Split(',');

                        // Ensure that the row has the expected number of columns (8 in this case)
                        if (values.Length >= 8)
                        {
                            var bulkDeal = new BulkDeal
                            {
                                TradedDate = values[0].Trim('"', ' ', '\t'), // Column 1: Date (Traded Date)
                                Symbol = values[1].Trim('"', ' ', '\t'), // Column 2: Symbol
                                SecurityName = values[2].Trim('"', ' ', '\t'), // Column 3: Security Name
                                ClientName = values[3].Trim('"', ' ', '\t'), // Column 4: Client Name
                                DealType = values[4].Trim('"', ' ', '\t'), // Column 5: Buy/Sell
                                Quantity = values[5].Trim('"', ' ', '\t'), // Column 6: Quantity
                                Price = values[6].Trim('"', ' ', '\t') // Column 7: Price
                            };

                            bulkDeals.Add(bulkDeal);
                        }
                    }
                    else
                    {
                        id++;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading the CSV file: " + ex.Message);
        }

        return bulkDeals;
    }


    public static void SaveDeals(List<BulkDeal> scrapedDeals)
    {
        using (var db = new BulkDealContext())
        {
            var last10DaysDeals = db.Deals.Where(c => c.CreatedOn >= DateTime.Now.AddDays(-10)).ToList();
            var newDeals = new List<BulkDeal>();
            foreach (var deal in scrapedDeals)
            {
                bool exists = last10DaysDeals.Any(d =>
                    d.TradedDate == deal.TradedDate &&
                    d.SecurityName == deal.SecurityName &&
                    d.ClientName == deal.ClientName &&
                    d.DealType == deal.DealType &&
                    d.Quantity == deal.Quantity &&
                    d.Price == deal.Price);

                if (!exists)
                {

                    newDeals.Add(deal);
                    Console.WriteLine($"Added: {deal.SecurityName} on {deal.TradedDate}");
                }
                else
                {
                    Console.WriteLine($"Skipped duplicate: {deal.SecurityName} on {deal.TradedDate}");
                }
            }
            db.Deals.AddRange(newDeals.DistinctBy(a => new { a.ClientName, a.DealType, a.Quantity, a.SecurityName, a.Price }).ToList());
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
            var buyCount = item.Value.Where(a => a.DealType == "BUY" && DateTime.Parse(a.TradedDate) >= DateTime.Now.AddDays(-5)).ToList();
            var sellCount = item.Value.Where(a => a.DealType == "SELL" && DateTime.Parse(a.TradedDate) >= DateTime.Now.AddDays(-5)).ToList();
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
        var subscribers = LoadExistingSubscribers();
        var dealsToSend = AnalyseData();
        if (dealsToSend.Count>0)
        {
            string htmlMessage = FormatDealsAsHtml(dealsToSend);

            foreach (var user in subscribers)
            {
                SendHtmlMessageAsync(user.ChatId, htmlMessage).Wait();
                Console.WriteLine($"✅ Sent message to: {user.FirstName} (@{user.Username})");
            }
        }
        else
        {
            SendHtmlMessageAsync("612419324", "App Ran. Nothing usefull.").Wait();
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

            var existingSubscribers = LoadExistingSubscribers();

            // Create a list of new subscribers to be added
            var newSubscribers = new List<TelegramUser>();

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

                // Only add new subscribers to the list
                if (!existingSubscribers.Any(u => u.ChatId == chatId))
                {
                    newSubscribers.Add(user);
                    Console.WriteLine($"✅ New Subscriber: {firstName} ({username}) [{chatId}]");
                }
            }

            // Bulk insert new subscribers into the database
            if (newSubscribers.Any())
            {
                SaveSubscribersBulk(newSubscribers);
            }
        }
    }


    private static List<TelegramUser> LoadExistingSubscribers()
    {
        using (var context = new BulkDealContext())
        {
            return context.TelegramUsers.ToList();
        }
    }


    private static void SaveSubscribersBulk(List<TelegramUser> newSubscribers)
    {
        using (var context = new BulkDealContext())
        {
            context.AddRange(newSubscribers);
            context.SaveChanges();
        }
    }



}
