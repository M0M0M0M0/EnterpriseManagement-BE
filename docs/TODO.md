# Việc cần làm (chưa làm)

Danh sách các việc đã được yêu cầu nhưng chưa triển khai. Xong việc nào thì chuyển xuống mục "Đã xong" kèm ngày và commit.

## Đang chờ

_(không có)_

## Đã xong

### Khoá chấm công sau 18:00 (xong 19/10/2026)
- **Quy tắc đã chốt:** tan làm 17:00, nhân viên chấm công ra **trước 18:00**; từ 18:00 trở đi khoá toàn bộ chấm công (cả vào và ra). Quên chấm công thì gửi yêu cầu điều chỉnh chấm công.
- **Backend:** `EffectiveDayCalculator.PunchLockFrom` (18:00) + `IsPunchLocked`; `AttendanceService.PunchAsync` kiểm tra đầu tiên và trả lỗi tiếng Việt. Có test đơn vị.
- **Web:** `PunchHero` tự khoá nút (đổi chữ "Đã khoá", bỏ hiệu ứng, kèm hướng dẫn). **Mobile:** `CheckButton`/Trang chủ tự khoá đúng 18:00.
- Mốc khoá đang là hằng số trong code; muốn cấu hình được thì chuyển sang `appsettings.json` (như mục `Reminders`).
