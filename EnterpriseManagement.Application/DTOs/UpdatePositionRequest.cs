namespace EnterpriseManagement.Application.DTOs;

public class UpdatePositionRequest
{
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Tuỳ chọn: null = giữ nguyên, "" = bỏ vai trò mặc định (dùng EMPLOYEE), còn lại phải là mã vai trò tồn tại.
    public string? RoleCode { get; set; }
}
