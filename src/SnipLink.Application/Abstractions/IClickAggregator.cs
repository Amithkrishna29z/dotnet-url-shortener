namespace SnipLink.Application.Abstractions;

public interface IClickAggregator
{
    Task<int> AggregateAsync(CancellationToken ct = default);
}
