namespace PingTrace.Core;

public class TraceDestination
{
    public TraceDestination()
    {
    }
    public TraceDestination(string name)
    {
        this.Name = name;
    }

    /// <summary>
    /// Friendly name of the target for this "project"
    /// </summary>
    public string Name { get; set; }

    public string ExpectedIdentity { get; set; }

    /// <summary>
    /// List of Machines names because of load balancing scenarios...
    /// </summary>
    /// <remarks>Try semi-hard-coding this, like with a config file so that when the
    /// actual trace is called, the current running values are returned</remarks>
    public string[] ExpectedMachineNames { get; set; }

    public int ExpectedElapsedMilisecondsMax { get; set; }

    public int ExpectedElapsedMilisecondsAverage { get; set; }

    /// <summary>
    /// Used to explain in plain words what to expect in the payload
    /// </summary>
    public string PayloadDescription { get; set; }

    /// <summary>
    /// A regex string that will be used to validate the payload.
    /// </summary>
    public string PayloadRegex { get; set; }
}
