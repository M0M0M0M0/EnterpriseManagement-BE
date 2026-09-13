using EnterpriseManagement.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Sales;

public class KpiLevelConfiguration : IEntityTypeConfiguration<KpiLevel>
{
    public void Configure(EntityTypeBuilder<KpiLevel> builder)
    {
        builder.ToTable("KpiLevels");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MinimumRevenue).HasPrecision(18, 2);
        builder.Property(x => x.CommissionRate).HasPrecision(5, 2);

        // Level thuộc hẳn về 1 Plan — xoá Plan thì xoá luôn các Level của nó (khác với các FK
        // Restrict còn lại trong app vì đây là quan hệ sở hữu thật, không phải tham chiếu).
        builder.HasOne(x => x.KpiPlan)
            .WithMany(x => x.Levels)
            .HasForeignKey(x => x.KpiPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
