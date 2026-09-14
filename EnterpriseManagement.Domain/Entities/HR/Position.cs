namespace EnterpriseManagement.Domain.Entities.HR;

public class Position
{
    public long Id { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    // Bậc thưa để so sánh cấp trên/dưới: số càng nhỏ càng cao cấp. Để hở khoảng cách
    // (vd 10, 20, 30...) khi seed, để sau này chèn thêm chức vụ mới ở giữa mà không phải
    // đổi số của các chức vụ khác. Không mô tả cây tổ chức thật — cây đó lấy từ
    // Employee.ManagerId; RankLevel chỉ để so sánh cấp bậc và gợi ý mặc định.
    public int RankLevel { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public PositionSalary? Salary { get; set; }
}
