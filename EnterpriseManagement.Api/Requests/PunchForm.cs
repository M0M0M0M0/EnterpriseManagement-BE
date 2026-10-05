namespace EnterpriseManagement.Api.Requests;

public class PunchForm
{
    // Toạ độ nhận dạng chuỗi rồi tự đọc theo InvariantCulture: model binding của form dùng culture của máy chủ,
    // máy chủ chạy vi-VN (dấu "." là phân cách hàng nghìn) sẽ đọc "21.0383" thành 210383 mà không báo lỗi.
    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    public bool IsMockLocation { get; set; }
    public IFormFile? Photo { get; set; }
}
