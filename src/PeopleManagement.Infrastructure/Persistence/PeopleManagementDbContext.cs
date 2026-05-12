using Microsoft.EntityFrameworkCore;
using PeopleManagement.Domain.People;

namespace PeopleManagement.Infrastructure.Persistence;

public sealed class PeopleManagementDbContext : DbContext
{
    public PeopleManagementDbContext(DbContextOptions<PeopleManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<Person> People => Set<Person>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PeopleManagementDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        StampPeopleTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampPeopleTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampPeopleTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Person>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.StampAsCreated(utcNow);
                    break;
                case EntityState.Modified:
                    entry.Entity.StampAsModified(utcNow);
                    break;
            }
        }
    }
}
