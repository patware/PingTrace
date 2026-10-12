namespace AspireAppSample.ApiService.Entities;

/// <summary>
/// Represents a person stored by the API service.
/// </summary>
/// <remarks>
/// Database mapping is configured by ApiServiceDbContext.
/// Application input validation should enforce a nonblank name and
/// an acceptable date of birth before saving.
/// </remarks>
public class Person
{
    /// <summary>
    /// Gets or sets the person's primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the person's name.
    /// </summary>
    /// <remarks>
    /// An empty initial value satisfies C# initialization requirements,
    /// but does not constitute a valid name. Required database mapping
    /// prevents null values; it does not reject blank text.
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the person's date of birth.
    /// </summary>
    /// <remarks>
    /// DateOnly represents a calendar date without a time or time zone.
    /// Its default value still requires application validation.
    /// </remarks>
    public DateOnly DateOfBirth { get; set; }
}