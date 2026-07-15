using SnipLink.Application.Links;

namespace SnipLink.Application.Abstractions;

public interface IClickRecorder
{
    bool Enqueue(ClickInfo click);
}
