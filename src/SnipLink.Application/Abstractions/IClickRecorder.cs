using SnipLink.Application.Links;

namespace SnipLink.Application.Abstractions;

/// <summary>
/// Records a click without blocking the redirect. Implementations should enqueue
/// the click for asynchronous persistence and return immediately.
/// </summary>
public interface IClickRecorder
{
    /// <summary>Enqueue a click. Returns false if the queue is full (click dropped).</summary>
    bool Enqueue(ClickInfo click);
}
