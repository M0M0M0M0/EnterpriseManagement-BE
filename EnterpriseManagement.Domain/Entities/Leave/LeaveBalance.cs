using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Leave;

public class LeaveBalance
{
    public long Id { get; set; }

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public long LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; }

    // Chỉ có giá trị với LeaveType có AccrualPeriod = MonthlyReset (vd Nghỉ ngắn) — mỗi
    // tháng 1 dòng riêng, tự reset. Các LeaveType còn lại để null (1 dòng dùng cho cả năm).
    public int? Month { get; set; }

    public LeaveUnit Unit { get; set; }
    public decimal AllocatedTime { get; set; }
    public decimal UsedTime { get; set; }
    public decimal RemainingTime { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
