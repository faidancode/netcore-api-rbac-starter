// The 'abstract' keyword prevents this class from being instantiated on its own.
// It serves as a blueprint for all other service classes.
public abstract class BaseService
{
    // 'protected' allows derived classes (like EmployeesService) to access the dispatcher.
    // 'readonly' ensures the dispatcher instance cannot be replaced after initialization.
    protected readonly IEventDispatcher _eventDispatcher;

    // The constructor ensures that every service inheriting from this class 
    // provides an IEventDispatcher implementation.
    protected BaseService(IEventDispatcher eventDispatcher)
    {
        _eventDispatcher = eventDispatcher;
    }

    // A helper method to broadcast events asynchronously.
    // Using 'T' makes it generic, so it can handle any type of event.
    // '@event' uses the @ prefix because 'event' is a reserved keyword in C#.
    protected Task DispatchAsync<T>(T @event, CancellationToken ct)
        => _eventDispatcher.DispatchAsync(@event, ct);
}