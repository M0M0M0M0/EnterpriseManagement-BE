namespace EnterpriseManagement.Domain.Enums;

// Trạng thái hiệu lực của một buổi (kết quả sau khi gộp nghỉ phép, đi làm lại, ghi chú HR và chấm công gốc).
public enum SessionStatus
{
    None,
    OnTime,
    Late,
    Absent,
    OnLeave
}
