namespace EnterpriseManagement.Domain.Enums;

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,

    // Manager đã duyệt, đang chờ người có quyền xử lý (HR) áp dụng vào dữ liệu chấm công.
    PendingHr
}
