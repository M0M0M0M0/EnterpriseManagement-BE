using EnterpriseManagement.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Sales;

public class KpiPlanConfiguration : IEntityTypeConfiguration<KpiPlan>
{
    public void Configure(EntityTypeBuilder<KpiPlan> builder)
    {
        builder.ToTable("KpiPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
