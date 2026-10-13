using System;

namespace PingTrace.Core;

/// <summary>
/// Describes a traced application or dependency, including its identity,
/// diagnostic payload, and execution timing.
/// </summary>
/// <remarks>
/// The producer supplies the descriptive fields and calls
/// <see cref="Finish(string)"/> when its diagnostic work completes.
/// Parent timings may include dependency execution, so durations from
/// multiple results can overlap.
/// </remarks>
public class TraceResult
{
    /// <summary>
    /// Initializes a result with <see cref="StartedAt"/> set to the
    /// current local time.
    /// </summary>
    public TraceResult()
    {
        this.StartedAt = DateTime.Now;
    }

    /// <summary>
    /// Gets or sets the name identifying the traced application or dependency.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the identity reported for the traced operation.
    /// </summary>
    /// <remarks>
    /// Its meaning depends on the producer. It may represent a process
    /// identity, a database login, or an application-configured identity label.
    /// </remarks>
    public string Identity { get; set; }

    /// <summary>
    /// Gets or sets the machine, server, or host reported by the producer.
    /// </summary>
    /// <remarks>
    /// Local application traces use <see cref="Environment.MachineName"/>.
    /// Dependency adapters may report the destination server or host instead.
    /// </remarks>
    public string MachineName { get; set; }

    /// <summary>
    /// Gets or sets the start timestamp, initialized to the current local
    /// time when the result is constructed.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the completion timestamp.
    /// </summary>
    /// <remarks>
    /// Call <see cref="Finish(string)"/> after the diagnostic work and any
    /// included dependency calls complete. Until assigned, this property
    /// retains the default <see cref="DateTime"/> value.
    /// </remarks>
    public DateTime FinishedAt { get; set; }

    /// <summary>
    /// Gets or sets the diagnostic payload supplied by the producer.
    /// </summary>
    /// <remarks>
    /// The payload format depends on the producer. It may contain plain
    /// text or serialized diagnostic data.
    /// </remarks>
    public string Payload { get; set; }

    /// <summary>
    /// Stores the diagnostic payload and records the current local time
    /// as the completion timestamp.
    /// </summary>
    /// <param name="payload">
    /// The diagnostic payload to store.
    /// </param>
    /// <remarks>
    /// Calling this method again replaces both the payload and completion
    /// timestamp. It does not change <see cref="StartedAt"/>.
    /// </remarks>
    public void Finish(string payload)
    {
        this.Payload = payload;
        this.FinishedAt = DateTime.Now;
    }

    /// <summary>
    /// Gets the difference between the completion and start timestamps.
    /// </summary>
    /// <remarks>
    /// Read this property after completion. It subtracts the recorded
    /// timestamps without validating them and does not measure a running
    /// duration. Wall-clock adjustments can affect the result.
    /// </remarks>
    public TimeSpan Elapsed
    {
        get { return FinishedAt - StartedAt; }
    }
}