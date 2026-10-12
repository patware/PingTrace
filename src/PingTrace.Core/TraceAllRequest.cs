using System;

namespace PingTrace.Core;

/// <summary>
/// Carries the limits and ancestor nodes for a recursive trace.
/// </summary>
/// <remarks>
/// Each destination receives a new request containing its parent's path.
/// Path-local ancestors allow shared dependencies to be visited through
/// different branches without mistaking those visits for cycles.
///
/// The deadline is preserved across HTTP calls so descendants do not
/// restart the traversal's time budget. Participating hosts should have
/// synchronized clocks.
/// </remarks>
public sealed class TraceAllRequest
{
    /// <summary>
    /// Gets or sets the number of connector hops still permitted.
    /// </summary>
    /// <remarks>
    /// Zero permits a local result only. If the application has registered
    /// destinations, zero causes traversal to fail rather than silently
    /// return an incomplete result.
    /// </remarks>
    public int RemainingDepth { get; set; } = 8;

    /// <summary>
    /// Gets or sets the UTC deadline for the traversal.
    /// </summary>
    public DateTimeOffset DeadlineUtc { get; set; } = DateTimeOffset.UtcNow.AddSeconds(30);

    /// <summary>
    /// Gets or sets the runtime nodes already visited on this path.
    /// </summary>
    public Guid[] Ancestors { get; set; } = Array.Empty<Guid>();

    /// <summary>
    /// Validates the traversal state.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The depth, ancestor path, or deadline is invalid.
    /// </exception>
    /// <remarks>
    /// A maximum path of sixteen hops provides an upper bound even when
    /// callers supply their own requests. An expired deadline is valid
    /// input; execution reports it as cancellation.
    /// </remarks>
    public void Validate()
    {
        if (RemainingDepth < 0 || RemainingDepth > 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RemainingDepth),
                "Remaining depth must be between zero and sixteen.");
        }

        if (Ancestors is null ||
            Ancestors.Length > 16 ||
            Ancestors.Length + RemainingDepth > 16 ||
            Array.Exists(Ancestors, ancestor => ancestor == Guid.Empty))
        {
            throw new ArgumentException(
                "The ancestor path is invalid.",
                nameof(Ancestors));
        }

        if (DeadlineUtc == default)
        {
            throw new ArgumentException(
                "A traversal deadline is required.",
                nameof(DeadlineUtc));
        }
    }
}