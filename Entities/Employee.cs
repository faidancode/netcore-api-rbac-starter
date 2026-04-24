namespace netcore_api_rbac_starter.Entities;

public enum Gender { Male, Female }
public enum EmployeeStatus { Active, Inactive, Terminated, OnLeave }

public class Employee : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Nip { get; set; } = string.Empty;         // Employee ID number (unique)
    public Gender Gender { get; set; }
    public EmployeeStatus EmployeeStatus { get; set; } = EmployeeStatus.Active;
    public bool IsActive { get; set; } = true;
    public DateOnly DateOfJoining { get; set; }
    public DateOnly? DateOfActivePosition { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;

    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    public ICollection<PositionHistory> PositionHistories { get; set; } = new List<PositionHistory>();
}