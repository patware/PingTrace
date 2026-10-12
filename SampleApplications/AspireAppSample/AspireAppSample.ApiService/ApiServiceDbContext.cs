using AspireAppSample.ApiService.Entities;

using Microsoft.EntityFrameworkCore;

namespace AspireAppSample.ApiService;

/// <summary>
/// Provides access to the API service's persistent data.
/// </summary>
/// <param name="options">
/// The database provider and options supplied by dependency injection.
/// </param>
/// <remarks>
/// Provider configuration belongs in application startup so the context
/// can receive different configurations without changing its implementation.
/// </remarks>
public sealed class ApiServiceDbContext(
    DbContextOptions<ApiServiceDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets the people available for queries and updates.
    /// </summary>
    internal DbSet<Person> People => Set<Person>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var people = modelBuilder.Entity<Person>();

        people.HasKey(person => person.Id);

        people.Property(person => person.Name)
            .HasMaxLength(100)
            .IsRequired();
    }
}