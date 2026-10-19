using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Api.Workers;

// Mỗi ngày xóa ảnh và vị trí chấm công cũ hơn thời hạn lưu (Attendance:EvidenceRetentionDays, mặc định 180 ngày;
// đặt 0 để tắt). Giờ vào/ra và kết quả công vẫn giữ nguyên, chỉ xóa bằng chứng ảnh/vị trí. Khớp nội dung chính sách quyền riêng tư.
public class AttendanceEvidenceRetentionWorker : BackgroundService
{
    private const int BatchSize = 500;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AttendanceEvidenceRetentionWorker> _logger;
    private readonly int _retentionDays;

    public AttendanceEvidenceRetentionWorker(
        IServiceScopeFactory scopeFactory, ILogger<AttendanceEvidenceRetentionWorker> logger, int retentionDays)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _retentionDays = retentionDays;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_retentionDays <= 0) return;
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleared = await PurgeAsync(
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(),
                    scope.ServiceProvider.GetRequiredService<IAttendancePhotoStorage>(),
                    VietnamClock.Today.AddDays(-_retentionDays));
                if (cleared > 0) _logger.LogInformation("Đã xóa bằng chứng (ảnh/vị trí) của {Count} bản ghi chấm công quá hạn lưu.", cleared);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lỗi khi dọn ảnh/vị trí chấm công quá hạn (sẽ thử lại hôm sau).");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    // Trả về số bản ghi đã được xóa bằng chứng.
    public static async Task<int> PurgeAsync(ApplicationDbContext context, IAttendancePhotoStorage storage, DateOnly cutoff)
    {
        var total = 0;
        while (true)
        {
            var batch = await context.AttendanceRecords
                .Where(a => a.AttendanceDate < cutoff
                    && (a.CheckInPhotoPath != null || a.CheckOutPhotoPath != null
                        || a.CheckInLatitude != null || a.CheckOutLatitude != null))
                .OrderBy(a => a.Id)
                .Take(BatchSize)
                .ToListAsync();
            if (batch.Count == 0) return total;

            foreach (var record in batch)
            {
                if (record.CheckInPhotoPath is not null) storage.Delete(record.CheckInPhotoPath);
                if (record.CheckOutPhotoPath is not null) storage.Delete(record.CheckOutPhotoPath);
                record.CheckInPhotoPath = null;
                record.CheckOutPhotoPath = null;
                record.CheckInLatitude = null;
                record.CheckInLongitude = null;
                record.CheckOutLatitude = null;
                record.CheckOutLongitude = null;
            }

            await context.SaveChangesAsync();
            total += batch.Count;
        }
    }
}
