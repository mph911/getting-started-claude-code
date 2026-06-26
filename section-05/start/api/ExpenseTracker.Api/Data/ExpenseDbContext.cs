using ExpenseTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Api.Data;

public sealed class ExpenseDbContext(DbContextOptions<ExpenseDbContext> options) : DbContext(options)
{
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var expense = modelBuilder.Entity<Expense>();

        expense.HasKey(e => e.Id);

        expense.Property(e => e.Amount)
            .HasPrecision(18, 2);

        expense.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(200);

        expense.Property(e => e.Date)
            .IsRequired();

        expense.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(now() at time zone 'utc')");

        expense.HasIndex(e => new { e.Date, e.CreatedAt })
            .IsDescending();
    }
}
