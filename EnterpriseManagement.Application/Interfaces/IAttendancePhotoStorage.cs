namespace EnterpriseManagement.Application.Interfaces;

public interface IAttendancePhotoStorage
{
    // Lưu ảnh và trả về đường dẫn tương đối (dùng để lưu vào AttendanceRecord).
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);

    void Delete(string relativePath);
}
