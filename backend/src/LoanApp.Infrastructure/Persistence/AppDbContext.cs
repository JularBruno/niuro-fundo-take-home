using LoanApp.Domain.Applications;
using LoanApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoanApplication> Applications => Set<LoanApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.HasKey(c => c.Id);
            customer.HasIndex(c => c.Ssn).IsUnique();
            customer.Property(c => c.Ssn).HasMaxLength(11);
            customer.OwnsOne(c => c.Address, address =>
            {
                address.Property(a => a.State).HasMaxLength(2);
            });
        });

        modelBuilder.Entity<LoanApplication>(application =>
        {
            application.HasKey(a => a.Id);
            application.Property(a => a.RequestedAmount).HasPrecision(18, 2);
            application.HasIndex(a => a.CustomerId).IsUnique();
            application.HasOne<Customer>().WithMany().HasForeignKey(a => a.CustomerId);
        });
    }
}
