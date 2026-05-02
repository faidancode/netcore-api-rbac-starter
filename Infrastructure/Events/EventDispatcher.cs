public class EventDispatcher : IEventDispatcher
{
    private readonly IServiceProvider _sp;

    public EventDispatcher(IServiceProvider sp)
    {
        _sp = sp;
    }

    public async Task DispatchAsync<T>(T @event, CancellationToken ct)
    {
        var handlers = _sp.GetServices<IEventHandler<T>>();

        foreach (var handler in handlers)
        {
            await handler.HandleAsync(@event, ct);
        }

        // Allow handlers registered against a shared interface, such as
        // IEventHandler<IAuditableEvent>, to receive concrete auditable events.
        if (@event is IAuditableEvent auditableEvent && typeof(T) != typeof(IAuditableEvent))
        {
            var auditHandlers = _sp.GetServices<IEventHandler<IAuditableEvent>>();

            foreach (var handler in auditHandlers)
            {
                await handler.HandleAsync(auditableEvent, ct);
            }
        }
    }
}
