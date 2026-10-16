# Việc cần làm (chưa làm)

Danh sách các việc đã được yêu cầu nhưng chưa triển khai. Xong việc nào thì chuyển xuống mục "Đã xong" kèm ngày và commit.

## Đang chờ

### 1. Cơ chế khoá chấm công sau 6h
- **Yêu cầu gốc (16/10/2026):** "thêm cơ chế khoá chấm công sau 6h".
- **Chưa rõ, cần chốt trước khi làm** (hai cách hiểu khác nhau, đổi cách làm hoàn toàn):
  1. **Sau 18:00 (6 giờ chiều)**: hết giờ này nhân viên không chấm công vào/ra được nữa trong ngày đó.
  2. **Sau 6 tiếng kể từ lúc chấm công vào**: hết 6 tiếng thì khoá việc chấm công ra / sửa giờ vào ra.
- **Các câu hỏi cần trả lời:**
  - Khoá áp dụng cho việc nào: chấm công vào, chấm công ra, hay cả hai? (hiện bấm "chấm công ra" nhiều lần sẽ đè giờ ra cũ, xem `AttendanceService.PunchAsync`).
  - Sau khi khoá, nhân viên quên chấm công thì xử lý thế nào: chỉ còn cách gửi **yêu cầu điều chỉnh chấm công** (Quản lý xác nhận → HR áp dụng) hay HR có thể ghi nhận trực tiếp?
  - Mốc khoá cố định hay cấu hình được (ví dụ `appsettings.json`, giống mục `Reminders`)? Có ngoại lệ cho ca làm muộn/ngày nghỉ bù không?
  - Thông báo cho người dùng khi bị khoá: nội dung thông báo lỗi, có hiện ở nút chấm công (Trang chủ mobile, `PunchHero` web) không.
- **Hướng làm gợi ý:** kiểm tra ở backend (`AttendanceService.PunchAsync`, nơi đã chặn chấm công khi đang nghỉ phép) và trả lỗi tiếng Việt rõ ràng; web và mobile chỉ hiển thị lỗi đó (và tắt nút nếu biết đã khoá). Thêm test đơn vị cho `PunchAsync` quanh mốc khoá, và thêm trường cấu hình nếu chọn mốc cấu hình được.

## Đã xong

_(chưa có)_
