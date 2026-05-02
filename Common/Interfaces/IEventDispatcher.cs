public interface IEventDispatcher
{
    Task DispatchAsync<T>(T @event, CancellationToken ct);
}