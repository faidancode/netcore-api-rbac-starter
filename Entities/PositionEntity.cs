namespace netcore_api_rbac_starter.Entities;

public class Position : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<PositionHistory> PositionHistories { get; set; } = new List<PositionHistory>();
}