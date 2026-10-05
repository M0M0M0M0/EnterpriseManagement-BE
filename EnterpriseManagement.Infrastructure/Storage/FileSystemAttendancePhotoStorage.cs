using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;

namespace EnterpriseManagement.Infrastructure.Storage;

// Lưu ảnh chấm công vào thư mục trên máy chủ, chia theo tháng, tên file ngẫu nhiên.
// Thư mục này không được phục vụ công khai: muốn xem ảnh phải qua API có kiểm tra quyền.
public class FileSystemAttendancePhotoStorage : IAttendancePhotoStorage
{
    private readonly string _rootDirectory;

    public FileSystemAttendancePhotoStorage(string rootDirectory)
    {
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var monthDirectory = VietnamClock.Today.ToString("yyyy-MM");
        var fileName = $"{Guid.NewGuid():N}{extension}";

        Directory.CreateDirectory(Path.Combine(_rootDirectory, monthDirectory));
        await using var file = File.Create(Path.Combine(_rootDirectory, monthDirectory, fileName));
        await content.CopyToAsync(file, cancellationToken);

        return $"{monthDirectory}/{fileName}";
    }

    public void Delete(string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));

        // Chỉ xoá file nằm trong thư mục lưu ảnh, phòng đường dẫn chứa ".." trỏ ra ngoài.
        if (!fullPath.StartsWith(_rootDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
