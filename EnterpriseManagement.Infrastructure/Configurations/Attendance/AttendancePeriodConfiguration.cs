using EnterpriseManagement.Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Attendance;

public class AttendancePeriodConfiguration : IEntityTypeConfiguration<AttendancePeriod>
{
    public void Configure(EntityTypeBuilder<AttendancePeriod> builder)
    {
        builder.ToTable("AttendancePeriods");
        builder.HasKey(x => x.Id);
        builder.HasOne(x => x.Closer).WithMany().HasForeignKey(x => x.ClosedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Year, x.Month }).IsUnique();
    }
}
