using BillingService.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingService.Data;

public class BillingDb : DbContext
{
    public BillingDb(DbContextOptions<BillingDb> options) : base(options) { }

    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillLineItem> BillLineItems => Set<BillLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bill>().ToTable("Bills").HasKey(b => b.BillId);
        modelBuilder.Entity<BillLineItem>().ToTable("BillLineItems").HasKey(li => li.LineId);

        modelBuilder.Entity<Bill>()
            .HasMany(b => b.LineItems)
            .WithOne(li => li.Bill)
            .HasForeignKey(li => li.BillId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Bill>().Property(b => b.AmountSubtotal).HasPrecision(18,2);
        modelBuilder.Entity<Bill>().Property(b => b.AmountTotal).HasPrecision(18,2);
        modelBuilder.Entity<Bill>().Property(b => b.TaxAmount).HasPrecision(18,2);
        modelBuilder.Entity<Bill>().Property(b => b.TaxPercent).HasPrecision(5,2);

        modelBuilder.Entity<BillLineItem>().Property(li => li.UnitPrice).HasPrecision(18,2);
        modelBuilder.Entity<BillLineItem>().Property(li => li.LineTotal).HasPrecision(18,2);
    }
}