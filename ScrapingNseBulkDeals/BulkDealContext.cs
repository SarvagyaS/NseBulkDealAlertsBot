using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrapingNseBulkDeals
{
    public class BulkDealContext : DbContext
    {
        public DbSet<BulkDeal> Deals { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // 👇 Replace with your local SQL Server connection string
            //options.UseSqlServer(@"Server=(LocalDb)\MSSQLLocalDB;Database=BulkDeals;Trusted_Connection=True;");
            options.UseSqlServer(@"Data Source=bulkdeals.cy9yymygw0nx.us-east-1.rds.amazonaws.com;Initial Catalog=BulkDeals;uid=admin;Password=Riya1234;TrustServerCertificate=True");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Create unique index to prevent duplicates
            modelBuilder.Entity<BulkDeal>()
                .HasIndex(b => new { b.TradedDate, b.SecurityName, b.ClientName, b.DealType, b.Quantity, b.Price, b.Symbol })
                .IsUnique();
        }
    }
}
