using netcore_api_rbac_starter.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace netcore_api_rbac_starter.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false);
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(d => d.Name).IsUnique().HasFilter("is_deleted = false");
        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}