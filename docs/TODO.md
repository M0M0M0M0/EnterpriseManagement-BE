# Việc cần làm (chưa làm)

Danh sách các việc đã được yêu cầu nhưng chưa triển khai. Xong việc nào thì chuyển xuống mục "Đã xong" kèm ngày và commit.

## Đang chờ

_(không có)_

## Đã xong

### Khoá chấm công sau 22:00 (xong 19/10/2026, đổi từ 18:00 sang 22:00)
- **Quy tắc đã chốt:** tan làm 17:00, nhân viên chấm công ra **trước 22:00**; từ 22:00 trở đi khoá toàn bộ chấm công (cả vào và ra). Quên chấm công thì gửi yêu cầu điều chỉnh chấm công.
- **Backend:** `EffectiveDayCalculator.PunchLockFrom` (22:00) + `IsPunchLocked`; `AttendanceService.PunchAsync` kiểm tra đầu tiên và trả lỗi tiếng Việt. Có test đơn vị.
- **Web:** `PunchHero` tự khoá nút (đổi chữ "Đã khoá", bỏ hiệu ứng, kèm hướng dẫn). **Mobile:** `CheckButton`/Trang chủ tự khoá đúng 22:00.
- Mốc khoá đang là hằng số trong code; muốn cấu hình được thì chuyển sang `appsettings.json` (như mục `Reminders`).

## Sau đợt rà soát BA/UX (2026-10-19)
- Giám đốc (CEO) không thuộc diện chấm công (TracksAttendance=false): không chấm công/xin nghỉ/OT/điều chỉnh, không có trong bảng công, vẫn là quản lý của cấp dưới. Admin chỉ làm kỹ thuật, không duyệt đơn.
- Ngày lễ 2026 đã đối chiếu công văn 9859/VPCP-KGVX (Tết 16-20/2, Giỗ Tổ 26-27/4, 30/4-1/5, Quốc khánh 1-2/9); 31/8 chỉ là hoán đổi ngày làm của khu vực công nên không seed.
- Đã làm: vai trò HR; tách quyền Quản lý/HR; OT; ngày lễ; bảng công tháng có chốt kỳ; đồng ý GPS/ảnh + tự xóa sau 180 ngày; xếp lại menu.
- Ngày lễ: seed 2026 đủ, 2027 chỉ ngày cố định. Tết Âm lịch/Giỗ Tổ/nghỉ bù các năm sau do HR bổ sung ở trang Ngày nghỉ lễ khi có thông báo chính thức.
- Lịch ngày lễ nạp vào bộ nhớ (WorkCalendar); chạy nhiều bản backend thì cần restart để nhận thay đổi ngày lễ từ bản khác.
- Dữ liệu demo: công ty ~100 người, 10 phòng, 5 cấp (Tổng giám đốc, 3 giám đốc khối, trưởng phòng, trưởng nhóm, nhân viên); tài khoản e1-e100 mật khẩu Demo@1234 khi dựng DB mới. Chốt bảng công chỉ trưởng nhân sự (vai trò HR_LEAD, quyền timesheet.close).
- Chưa làm: ca làm việc, tính tiền OT, geofence theo nhiều địa điểm, phép năm theo thâm niên/chuyển phép, tương phản/chế độ tối.
