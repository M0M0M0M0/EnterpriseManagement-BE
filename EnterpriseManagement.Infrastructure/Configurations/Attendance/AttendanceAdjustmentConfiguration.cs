using EnterpriseManagement.Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Attendance;

public class AttendanceAdjustmentConfiguration : IEntityTypeConfiguration<AttendanceAdjustment>
{
    public void Configure(EntityTypeBuilder<AttendanceAdjustment> builder)
    {
        builder.ToTable("AttendanceAdjustments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasOne(x => x.Attendance)
            .WithMany(x => x.Adjustments)
            .HasForeignKey(x => x.AttendanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Requester)
            .WithMany()
            .HasForeignKey(x => x.RequestedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Approver)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasOne(x => x.Processor)
            .WithMany()
            .HasForeignKey(x => x.ProcessedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
