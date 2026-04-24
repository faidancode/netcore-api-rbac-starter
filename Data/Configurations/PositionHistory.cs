using netcore_api_rbac_starter.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace netcore_api_rbac_starter.Data.Configurations;

public class PositionHistoryConfiguration : IEntityTypeConfiguration<PositionHistory>
{
    public void Configure(EntityTypeBuilder<PositionHistory> builder)
    {
        builder.ToTable("position_histories");
        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.Id).HasColumnName("id");
        builder.Property(ph => ph.StartDate).HasColumnName("start_date");
        builder.Property(ph => ph.EndDate).HasColumnName("end_date");
        builder.Property(ph => ph.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(ph => ph.Notes).HasColumnName("notes").HasMaxLength(1000);
        builder.Property(ph => ph.CreatedAt).HasColumnName("created_at");
        builder.Property(ph => ph.EmployeeId).HasColumnName("employee_id");
        builder.Property(ph => ph.PositionId).HasColumnName("position_id");

        builder.HasOne(ph => ph.Employee)
            .WithMany(e => e.PositionHistories)
            .HasForeignKey(ph => ph.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ph => ph.Position)
            .WithMany(p => p.PositionHistories)
            .HasForeignKey(ph => ph.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}