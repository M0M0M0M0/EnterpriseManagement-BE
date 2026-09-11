using EnterpriseManagement.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Leave;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LeaveTypeCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.LeaveTypeName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AccrualAmount).HasPrecision(6, 2);
        builder.Property(x => x.AccrualUnit).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.AccrualPeriod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.LeaveTypeCode).IsUnique();
    }
}
