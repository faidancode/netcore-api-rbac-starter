using netcore_api_rbac_starter.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace netcore_api_rbac_starter.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        builder.Property(p => p.Subject).HasColumnName("subject").HasMaxLength(100).IsRequired();
        builder.Property(p => p.Conditions).HasColumnName("conditions").HasColumnType("jsonb");
        builder.Property(p => p.Fields).HasColumnName("fields").HasMaxLength(1000);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(p => new { p.Action, p.Subject }).IsUnique().HasFilter("is_deleted = false");
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}