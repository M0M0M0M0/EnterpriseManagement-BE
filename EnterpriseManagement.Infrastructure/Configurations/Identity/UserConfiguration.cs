using EnterpriseManagement.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseManagement.Infrastructure.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => x.Username).IsUnique();

        // 1 hồ sơ = đúng 1 tài khoản: khoá ngoại bắt buộc + chỉ mục duy nhất trên EmployeeId.
        builder.HasOne(x => x.Employee)
            .WithOne(x => x.User)
            .HasForeignKey<User>(x => x.EmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EmployeeId).IsUnique();
    }
}
