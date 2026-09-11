namespace EnterpriseManagement.Domain.Enums;

// Quyết định LeaveBalance được cấp/reset như thế nào cho từng LeaveType:
// - ProratedYearly: cộng dồn theo tháng (AccrualAmount x số tháng còn lại trong năm kể từ
//   ngày tạo hồ sơ nhân viên), 1 dòng LeaveBalance cho cả năm (vd Nghỉ có phép, Nghỉ không lương).
// - MonthlyReset: mỗi tháng có 1 dòng LeaveBalance riêng, luôn được cấp đủ AccrualAmount,
//   không cộng dồn/không giữ lại phần chưa dùng của tháng trước (vd Nghỉ ngắn).
// - FlatYearly: cấp đủ AccrualAmount 1 lần cho cả năm, không tính theo ngày tạo hồ sơ,
//   chỉ reset khi sang năm mới (vd Nghỉ chế độ).
public enum LeaveAccrualPeriod
{
    ProratedYearly,
    MonthlyReset,
    FlatYearly
}
