using EnterpriseManagement.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Leave;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Unit).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.AllocatedTime).HasPrecision(6, 2);
        builder.Property(x => x.UsedTime).HasPrecision(6, 2);
        builder.Property(x => x.RemainingTime).HasPrecision(6, 2);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.LeaveType)
            .WithMany()
            .HasForeignKey(x => x.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
