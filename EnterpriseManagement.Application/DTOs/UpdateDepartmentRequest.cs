namespace EnterpriseManagement.Application.DTOs;

public class UpdateDepartmentRequest
{
    // Mã mới của phòng ban (để trống hoặc giống mã hiện tại thì giữ nguyên). Không được trùng mã phòng ban khác.
    public string? DepartmentCode { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    // Mã nhân viên làm trưởng phòng: null = giữ nguyên, chuỗi rỗng = bỏ trưởng phòng.
    public string? ManagerCode { get; set; }

    public string? Description { get; set; }
}
