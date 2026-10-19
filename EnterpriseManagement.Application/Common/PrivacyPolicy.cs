namespace EnterpriseManagement.Application.Common;

public sealed record PrivacySection(string Heading, string Body);

// Nội dung chính sách dữ liệu chấm công (vị trí GPS và ảnh chụp). Nguồn DUY NHẤT: web và mobile cùng hiển thị bản này.
// Đổi nội dung thật sự (thu thêm dữ liệu, đổi mục đích, đổi thời hạn lưu) thì tăng [CurrentVersion] để mọi người đồng ý lại.
public static class PrivacyPolicy
{
    public const int CurrentVersion = 1;

    // Số ngày giữ vị trí và ảnh chấm công trước khi tự động xóa (cấu hình Attendance:EvidenceRetentionDays, mặc định như dưới).
    public const int DefaultRetentionDays = 180;

    public static IReadOnlyList<PrivacySection> Sections(int retentionDays) => new[]
    {
        new PrivacySection(
            "Dữ liệu được thu thập",
            "Khi bạn chấm công, hệ thống ghi lại vị trí GPS tại đúng thời điểm bấm nút (không theo dõi liên tục, không đọc vị trí lúc bạn không chấm công) "
            + "và ảnh chụp chân dung khi chấm công vào. Ảnh chỉ để làm bằng chứng, hệ thống không nhận diện khuôn mặt."),
        new PrivacySection(
            "Mục đích sử dụng",
            "Xác nhận bạn có mặt tại nơi làm việc, hạn chế chấm công hộ và làm căn cứ khi có khiếu nại về công. Dữ liệu không dùng cho mục đích khác."),
        new PrivacySection(
            "Ai được truy cập",
            "Ảnh và vị trí lưu trên máy chủ của công ty, không công khai và không hiển thị trong ứng dụng. Chỉ quản trị viên hệ thống được truy cập kỹ thuật "
            + "khi cần giải quyết khiếu nại hoặc yêu cầu của cơ quan có thẩm quyền; mọi thao tác quản trị đều được ghi nhật ký."),
        new PrivacySection(
            "Thời gian lưu trữ",
            $"Ảnh và vị trí chấm công được tự động xóa sau {retentionDays} ngày. Giờ vào/ra và kết quả công vẫn được giữ để tính lương."),
        new PrivacySection(
            "Quyền của bạn",
            "Bạn có quyền biết dữ liệu nào được thu thập, đồng ý hoặc rút lại sự đồng ý (theo Nghị định 13/2023/NĐ-CP về bảo vệ dữ liệu cá nhân), "
            + "yêu cầu xem hoặc xóa dữ liệu của mình bằng cách liên hệ bộ phận Nhân sự. Nếu rút lại đồng ý, bạn sẽ không chấm công được bằng ứng dụng "
            + "và cần liên hệ Nhân sự để được hướng dẫn cách chấm công khác."),
    };
}
