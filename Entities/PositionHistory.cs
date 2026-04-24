namespace netcore_api_rbac_starter.Entities;

public class PositionHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
}