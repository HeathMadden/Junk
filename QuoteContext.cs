using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using PureIP.Portal.Domain.Models.Quote;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace PureIP.Portal.Data.Quote
{
    public class QuoteContext : DbContext
    {
        public virtual DbSet<QuoteDetails> QuoteDetails { get; set; }
        public virtual DbSet<ContractTerm> ContractTerm { get; set; }

        public QuoteContext(DbContextOptions<QuoteContext> options)
               : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Quote");

            modelBuilder.Entity<QuoteDetails>(entity =>
            {
                entity.Property(x => x.Id).ValueGeneratedOnAdd();
                entity.Ignore(x => x.DiscountFee);
                entity.Ignore(x => x.TotalFee);
                entity.Ignore(x => x.TotalPrice);
            });

            modelBuilder.Entity<QuoteLine>(entity =>
            {
                entity.Property(x => x.Price).HasColumnType("Money");
                entity.Property(x => x.Fee).HasColumnType("Money");
                entity.Property(x => x.MinimumCallCharge).HasColumnType("Money");
                entity.Property(x => x.FixedRate).HasColumnType("Money");
                entity.Property(x => x.MobileRate).HasColumnType("Money");
                entity.Property(x => x.PortingFee).HasColumnType("Money");

                entity.Ignore(x => x.Pillar);
                entity.Ignore(x => x.Description);
                entity.Ignore(x => x.QuoteLineProfit);
                entity.Ignore(x => x.Product);
            });

            modelBuilder.Entity<SupplierQuote>(entity =>
            {
                entity.Property(x => x.Price).HasColumnType("Money");
                entity.Property(x => x.Fee).HasColumnType("Money");
                entity.Ignore(x => x.QuoteDocumentFile);
            });

            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContractTerm>()
               .HasData(
                   new ContractTerm { Id = 12, Name = "12 Months" , Lenght = 12, DiscountPercentage = 0 },
                   new ContractTerm { Id = 24, Name = "24 Months" , Lenght = 24, DiscountPercentage = 50 },
                   new ContractTerm { Id = 36, Name = "36 Months" , Lenght = 36, DiscountPercentage = 100 }
                   );
        }
    }
}
