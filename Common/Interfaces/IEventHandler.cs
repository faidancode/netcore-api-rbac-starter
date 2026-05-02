public interface IEventHandler<T>
{
    Task HandleAsync(T @event, CancellationToken ct);
}