using EnterpriseManagement.Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Attendance;

public class AttendanceSessionNoteConfiguration : IEntityTypeConfiguration<AttendanceSessionNote>
{
    public void Configure(EntityTypeBuilder<AttendanceSessionNote> builder)
    {
        builder.ToTable("AttendanceSessionNotes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Session).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500).IsRequired();

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Creator).WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Revoker).WithMany().HasForeignKey(x => x.RevokedBy).OnDelete(DeleteBehavior.Restrict);

        // Mỗi (nhân viên, ngày, buổi) chỉ được có một ghi chú còn hiệu lực (RevokedAt IS NULL); ghi chú đã thu hồi giữ lại làm lịch sử.
        builder.HasIndex(x => new { x.EmployeeId, x.Date, x.Session })
            .IsUnique()
            .HasFilter("[RevokedAt] IS NULL");
    }
}
