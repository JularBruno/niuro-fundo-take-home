using LoanApp.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.HasKey(c => c.Id);
            customer.Property(c => c.Id).ValueGeneratedNever(); // ids are created by the domain
            customer.Property(c => c.Ssn)
                .HasConversion(ssn => ssn.Value, value => new Ssn(value))
                .HasMaxLength(9);
            customer.HasIndex(c => c.Ssn).IsUnique();
            customer.OwnsOne(c => c.Address, address =>
            {
                address.Property(a => a.State).HasMaxLength(2);
            });

            // The aggregate is always loaded whole: customer + its single application.
            customer.HasOne(c => c.Application)
                .WithOne()
                .HasForeignKey<LoanApplication>(a => a.CustomerId);
            customer.Navigation(c => c.Application).AutoInclude();
        });

        modelBuilder.Entity<LoanApplication>(application =>
        {
            application.ToTable("Applications");
            application.HasKey(a => a.Id);
            application.Property(a => a.Id).ValueGeneratedNever();
            application.Property(a => a.RequestedAmount).HasPrecision(18, 2);
        });
    }
}
