namespace EnterpriseManagement.Application.Interfaces;

// Gửi thông báo đẩy tới thiết bị (hiện cài đặt bằng Firebase Cloud Messaging). Khi chưa cấu hình Firebase,
// bản cài đặt rỗng không làm gì, hộp thông báo trong app vẫn hoạt động.
public interface IPushSender
{
    // Có thật sự gửi được không (đã cấu hình Firebase).
    bool IsEnabled { get; }

    // Trả về các token không còn hợp lệ (máy đã gỡ app, token hết hạn...) để dọn khỏi DB.
    Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyCollection<string> tokens, string title, string body, IReadOnlyDictionary<string, string> data);
}
