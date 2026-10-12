using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;

namespace EnterpriseManagement.Api.Workers;

// Mỗi phút kiểm tra xem đã đến khung giờ nhắc chấm công chưa (cấu hình ở mục "Reminders" trong appsettings.json).
// Chống gửi trùng nằm ở AttendanceReminderService (mỗi nhân viên mỗi loại một lần mỗi ngày), nên khởi động lại backend vẫn an toàn.
public class AttendanceReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceReminderWorker> _logger;

    public AttendanceReminderWorker(IServiceScopeFactory scopeFactory, ILogger<AttendanceReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ backend khởi động xong (seed dữ liệu...) rồi mới bắt đầu.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IAttendanceReminderService>();
                var sent = await service.RunAsync(VietnamClock.Now);
                if (sent > 0) _logger.LogInformation("Đã gửi {Count} thông báo nhắc chấm công.", sent);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lỗi khi gửi nhắc chấm công (sẽ thử lại ở phút sau).");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
