using Microsoft.Extensions.Logging;

public class GenericAuditHandler : IEventHandler<IAuditableEvent>
{
    private readonly AuditService _audit;
    private readonly ILogger<GenericAuditHandler> _logger;

    public GenericAuditHandler(AuditService audit, ILogger<GenericAuditHandler> logger)
    {
        _audit = audit;
        _logger = logger;
    }

    public Task HandleAsync(IAuditableEvent e, CancellationToken ct)
    {
        _logger.LogInformation(
            "Audit event received. request_id={RequestId} entity={EntityName} entity_id={EntityId} action={Action}",
            e.RequestId,
            e.EntityName,
            e.EntityId,
            e.Action);

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
