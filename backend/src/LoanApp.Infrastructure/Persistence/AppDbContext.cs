using LoanApp.Application.Abstractions;
using LoanApp.Domain.Customers;
using LoanApp.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace LoanApp.Infrastructure.Persistence;

/// <summary>
/// Also the unit of work: one SaveChanges writes customer, application and outbox rows in a single transaction.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

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

        modelBuilder.Entity<OutboxMessage>(message =>
        {
            message.HasKey(m => m.Id);
            message.Property(m => m.Type).HasMaxLength(200);
            message.Property(m => m.Payload).HasColumnType("jsonb");
            // The processor only ever reads pending messages, oldest first.
            message.HasIndex(m => m.OccurredAt).HasFilter("\"ProcessedAt\" IS NULL");
        });
    }
}
