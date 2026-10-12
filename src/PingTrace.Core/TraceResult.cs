using System;

namespace PingTrace.Core;


public class TraceResult
{
    public TraceResult()
    {
        this.StartedAt = DateTime.Now;
    }

    public string Name { get; set; }

    public string Identity { get; set; }

    /// <summary>
    /// Make use of the <see cref="System.Environment.MachineName"/>
    /// </summary>
    public string MachineName { get; set; }

    public DateTime StartedAt { get; set; }
    /// <summary>
    /// Use the <see cref="Finish(string)"/> method set this.  Preferrably, call it when the "child" traces have been called.
    /// </summary>
    public DateTime FinishedAt { get; set; }

    public string Payload { get; set; }

    public void Finish(string payload)
    {
        this.Payload = payload;
        this.FinishedAt = DateTime.Now;
    }
    public TimeSpan Elapsed
    {
        get { return FinishedAt - StartedAt; }
    }

}
