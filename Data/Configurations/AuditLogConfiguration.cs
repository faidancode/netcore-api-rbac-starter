using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Table Name
        builder.ToTable("audit_logs");

        // Primary Key
        builder.HasKey(x => x.Id);

        // Properties Mapping
        builder.Property(x => x.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Action)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.UserId)
            .HasMaxLength(100);

        builder.Property(x => x.UserName)
            .HasMaxLength(255);

        // Large Text for JSON Columns
        // Menggunakan .HasColumnType("nvarchar(max)") atau "text" tergantung database
        builder.Property(x => x.Before);
        builder.Property(x => x.After);

        // Indexing for Performance
        // Sangat penting karena Audit Log biasanya dicari berdasarkan Entity atau Tanggal
        builder.HasIndex(x => x.EntityId);
        builder.HasIndex(x => x.EntityName);
        builder.HasIndex(x => x.CreatedAt);

        // Composite Index untuk audit trail per record
        builder.HasIndex(x => new { x.EntityName, x.EntityId });
    }
}