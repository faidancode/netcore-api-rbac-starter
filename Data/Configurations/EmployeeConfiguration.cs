using netcore_api_rbac_starter.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace netcore_api_rbac_starter.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(300).IsRequired();
        builder.Property(e => e.Nip).HasColumnName("nip").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Gender).HasColumnName("gender").HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.EmploymentType).HasColumnName("employment_type").HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.EmployeeStatus).HasColumnName("employee_status").HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(e => e.DateOfJoining).HasColumnName("date_of_joining");
        builder.Property(e => e.DateOfActivePosition).HasColumnName("date_of_active_position");
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.DepartmentId).HasColumnName("department_id");
        builder.Property(e => e.PositionId).HasColumnName("position_id");
        builder.Property(e => e.ManagerId).HasColumnName("manager_id");
        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(e => e.Nip).IsUnique().HasFilter("is_deleted = false");
        builder.HasIndex(e => e.UserId).IsUnique().HasFilter("user_id IS NOT NULL AND is_deleted = false");

        builder.HasOne(e => e.User)
            .WithOne(u => u.Employee)
            .HasForeignKey<Employee>(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}