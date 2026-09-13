using EnterpriseManagement.Domain.Entities.HR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.HR;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PositionCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.PositionName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.RankLevel).IsRequired();
        builder.Property(x => x.RoleCode).HasMaxLength(50);

        builder.HasIndex(x => x.PositionCode).IsUnique();

        builder.HasOne(x => x.Salary)
            .WithOne(x => x.Position)
            .HasForeignKey<PositionSalary>(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
