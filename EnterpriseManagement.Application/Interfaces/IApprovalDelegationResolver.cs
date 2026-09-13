using EnterpriseManagement.Domain.Entities.HR;

namespace EnterpriseManagement.Application.Interfaces;

public interface IApprovalDelegationResolver
{
    // Ai là người thực sự cần duyệt các đề xuất (nghỉ phép/sale/chấm công...) của "employee"
    // ngay tại thời điểm gọi. Bình thường là quản lý trực tiếp; nếu quản lý trực tiếp đang
    // nghỉ phép (đã duyệt, đang trong thời gian đó, và chưa check-in đi làm lại) thì đẩy lên
    // quản lý của quản lý đó, cứ thế đi lên cho tới khi gặp người không "vắng mặt" hoặc hết chuỗi
    // quản lý. Trả về null nếu employee không có quản lý nào (không xác định được ai duyệt).
    Task<Employee?> ResolveApproverAsync(Employee employee);
}
