using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapingNseBulkDeals
{
    public class BulkDeal
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public string DealType { get; set; }
        public string Quantity { get; set; }
        public string Price { get; set; }
        public string TradedDate { get; set; }
        public string SecurityName { get; set; }
        public string Symbol { get; set; }
        public DateTime? CreatedOn { get; set; }

    }



    public class TelegramUser
    {
        public int Id { get; set; }
        public string ChatId { get; set; }
        public string FirstName { get; set; }
        public string Username { get; set; }
    }
}
