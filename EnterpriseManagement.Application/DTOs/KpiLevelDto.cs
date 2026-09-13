namespace EnterpriseManagement.Application.DTOs;

public class KpiLevelDto
{
    public long Id { get; set; }
    public int LevelOrder { get; set; }
    public decimal MinimumRevenue { get; set; }
    public decimal CommissionRate { get; set; }
}

// Dùng khi tạo/sửa Plan — không có Id vì level có thể là mới hoàn toàn (UpdateAsync xoá hết
// level cũ và tạo lại theo danh sách này mỗi lần sửa).
public class KpiLevelInput
{
    public int LevelOrder { get; set; }
    public decimal MinimumRevenue { get; set; }
    public decimal CommissionRate { get; set; }
}
