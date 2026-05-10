public record UserCreatedEvent(
    Guid UserId,
    string Name,
    string Email,
    Guid? RoleId,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "User";
    public Guid EntityId => UserId;
    public string Action => AuditActions.Create;
    public string? RequestId => requestId;

    public object? Before => null;
    public object? After => new { UserId, Name, Email, RoleId };
}

public record UserUpdatedEvent(
    Guid UserId,
    object BeforeData,
    object AfterData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "User";
    public Guid EntityId => UserId;
    public string Action => AuditActions.Update;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => AfterData;
}

public record UserPasswordChangedEvent(
    Guid UserId,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "User";
    public Guid EntityId => UserId;
    public string Action => AuditActions.ChangePassword;
    public string? RequestId => requestId;

    public object? Before => null;
    public object? After => null;
}

public record UserDeletedEvent(
    Guid UserId,
    object BeforeData,
    string? requestId = null
) : IAuditableEvent
{
    public string EntityName => "User";
    public Guid EntityId => UserId;
    public string Action => AuditActions.Delete;
    public string? RequestId => requestId;

    public object? Before => BeforeData;
    public object? After => null;
}
