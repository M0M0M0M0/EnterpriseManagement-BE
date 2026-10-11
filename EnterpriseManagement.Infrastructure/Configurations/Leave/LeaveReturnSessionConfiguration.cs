using EnterpriseManagement.Domain.Entities.Leave;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Leave;

public class LeaveReturnSessionConfiguration : IEntityTypeConfiguration<LeaveReturnSession>
{
    public void Configure(EntityTypeBuilder<LeaveReturnSession> builder)
    {
        builder.ToTable("LeaveReturnSessions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Session).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.State).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.WorkedStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.ReturnRequest)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.ReturnRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.LeaveRequest)
            .WithMany()
            .HasForeignKey(x => x.LeaveRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.LeaveRequestId, x.Date, x.Session });
    }
}
