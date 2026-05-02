public class GenericAuditHandler : IEventHandler<IAuditableEvent>
{
    private readonly AuditService _audit;

    public GenericAuditHandler(AuditService audit)
    {
        _audit = audit;
    }

    public Task HandleAsync(IAuditableEvent e, CancellationToken ct)
    {
        return _audit.LogAsync(
            e.EntityName,
            e.EntityId,
            e.Action,
            e.Before,
            e.After,
            ct
        );
    }
}