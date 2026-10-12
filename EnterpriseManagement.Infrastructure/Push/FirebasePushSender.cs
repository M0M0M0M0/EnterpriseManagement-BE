using EnterpriseManagement.Application.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace EnterpriseManagement.Infrastructure.Push;

// Gửi thông báo đẩy qua Firebase Cloud Messaging bằng khoá tài khoản dịch vụ (file JSON tải từ Firebase Console).
// Chưa cấu hình hoặc file không đọc được thì tự tắt (IsEnabled = false), hộp thông báo trong app vẫn hoạt động bình thường.
public class FirebasePushSender : IPushSender
{
    private readonly ILogger<FirebasePushSender> _logger;

    public bool IsEnabled { get; }

    public FirebasePushSender(string? credentialsPath, ILogger<FirebasePushSender> logger)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
        {
            _logger.LogInformation("Firebase chưa được cấu hình (Firebase:CredentialsPath): chỉ lưu hộp thông báo, không gửi thông báo đẩy.");
            return;
        }

        try
        {
            if (FirebaseApp.DefaultInstance is null)
            {
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credentialsPath) });
            }

            IsEnabled = true;
            _logger.LogInformation("Firebase đã sẵn sàng: bật gửi thông báo đẩy.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không khởi tạo được Firebase từ file {Path}; tắt thông báo đẩy.", credentialsPath);
        }
    }

    public async Task<IReadOnlyList<string>> SendAsync(
        IReadOnlyCollection<string> tokens, string title, string body, IReadOnlyDictionary<string, string> data)
    {
        var invalid = new List<string>();
        if (!IsEnabled) return invalid;

        foreach (var token in tokens)
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification { Title = title, Body = body },
                Data = new Dictionary<string, string>(data),
                Android = new AndroidConfig { Priority = Priority.High }
            };

            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (FirebaseMessagingException ex)
                when (ex.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                // Token không còn dùng được (đã gỡ app, hết hạn): dọn khỏi DB.
                invalid.Add(token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gửi thông báo đẩy thất bại (bỏ qua).");
            }
        }

        return invalid;
    }
}
