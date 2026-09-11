namespace EnterpriseManagement.Application.DTOs;

public class SubmitLeaveRequest
{
    public string LeaveTypeCode { get; set; } = string.Empty;

    // Với loại nghỉ chọn giờ cụ thể (Nghỉ ngắn): StartDate/EndDate là mốc giờ thật, chọn
    // trực tiếp. Với loại nghỉ chọn buổi (các loại còn lại): chỉ cần phần Ngày là đúng,
    // backend sẽ tự gán giờ theo Session và bỏ qua phần giờ client gửi lên.
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Bắt buộc với loại nghỉ chọn buổi (Morning/Afternoon/FullDay), bỏ trống với Nghỉ ngắn.
    public string? Session { get; set; }

    public string? Reason { get; set; }
}
