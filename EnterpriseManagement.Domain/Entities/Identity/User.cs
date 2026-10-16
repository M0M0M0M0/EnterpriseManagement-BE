using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Domain.Entities.Identity;

public class User
{
    public long Id { get; set; }

    // Mỗi tài khoản gắn đúng một hồ sơ nhân viên và ngược lại (1-1, bắt buộc). Email nằm ở hồ sơ.
    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
