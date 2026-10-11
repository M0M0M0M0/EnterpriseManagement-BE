using EnterpriseManagement.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Leave;

public class LeaveReturnRequestConfiguration : IEntityTypeConfiguration<LeaveReturnRequest>
{
    public void Configure(EntityTypeBuilder<LeaveReturnRequest> builder)
    {
        builder.ToTable("LeaveReturnRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromSession).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(500);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Processor).WithMany().HasForeignKey(x => x.ProcessedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EmployeeId, x.Status });
    }
}
