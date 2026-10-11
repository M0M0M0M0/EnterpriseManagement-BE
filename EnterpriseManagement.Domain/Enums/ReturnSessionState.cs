namespace EnterpriseManagement.Domain.Enums;

// Trạng thái của từng buổi trong một yêu cầu đi làm lại. Chỉ Active mới làm buổi đó hết hiệu lực nghỉ phép.
public enum ReturnSessionState
{
    Pending,
    AwaitingHr,
    Active,
    Rejected
}
