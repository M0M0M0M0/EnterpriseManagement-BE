namespace EnterpriseManagement.Application.DTOs;

public class PunchRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Thiết bị báo vị trí đang bị giả lập (app giả GPS). Có thể bị qua mặt trên máy đã root, chỉ là một lớp chặn cơ bản.
    public bool IsMockLocation { get; set; }

    // Ảnh chụp lúc check-in (bắt buộc), đã được tầng API kiểm tra kích thước và định dạng. Check-out không cần ảnh.
    public Stream? Photo { get; set; }
    public string PhotoExtension { get; set; } = ".jpg";
}
