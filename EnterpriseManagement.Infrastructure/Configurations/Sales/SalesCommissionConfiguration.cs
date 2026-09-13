using EnterpriseManagement.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Sales;

public class SalesCommissionConfiguration : IEntityTypeConfiguration<SalesCommission>
{
    public void Configure(EntityTypeBuilder<SalesCommission> builder)
    {
        builder.ToTable("SalesCommissions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TotalRevenue).HasPrecision(18, 2);
        builder.Property(x => x.CommissionRate).HasPrecision(5, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.KpiLevel)
            .WithMany()
            .HasForeignKey(x => x.KpiLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Approver)
            .WithMany()
            .HasForeignKey(x => x.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
