# Cài đặt thông báo đẩy (Firebase Cloud Messaging)

Hộp **Thông báo** trong app hoạt động ngay mà **không cần Firebase** (thông báo lưu trong database và app tự làm mới).
Firebase chỉ thêm khả năng **đẩy thông báo về điện thoại khi app đang đóng hoặc chạy nền**.

Tài khoản cần có: **một tài khoản Google** (miễn phí). Không cần thẻ thanh toán, không cần "API key" nhập bằng tay:
app dùng file `google-services.json`, backend dùng một file khoá JSON.

---

## Bước 1. Tạo dự án Firebase

1. Vào https://console.firebase.google.com và đăng nhập bằng tài khoản Google.
2. Bấm **Create a project** (Tạo dự án) → đặt tên, ví dụ `ems-attendance`.
3. Màn hình Google Analytics: tắt đi cũng được (không bắt buộc) → **Create project**.

## Bước 2. Thêm ứng dụng Android và lấy `google-services.json`

1. Trong dự án vừa tạo, bấm biểu tượng **Android** (Add app).
2. **Android package name** điền đúng: `com.example.enterprise_management`
   (là `applicationId` trong `android/app/build.gradle.kts` của app mobile; điền sai thì thông báo không tới).
3. Tên ứng dụng (nickname) tuỳ ý. Bỏ trống SHA-1 (không cần cho thông báo đẩy).
4. Bấm **Register app** → **Download google-services.json**.
5. Chép file đó vào: `EnterpriseManagement-MB/android/app/google-services.json`
6. Các bước tiếp theo trên trang Firebase (thêm SDK, plugin Gradle...) **bỏ qua**: dự án đã cấu hình sẵn.
7. File này nằm trong `.gitignore`, **không đưa lên Git**.

## Bước 3. Lấy khoá cho backend (service account)

1. Firebase Console → biểu tượng bánh răng cạnh **Project Overview** → **Project settings**.
2. Tab **Service accounts** → bấm **Generate new private key** → **Generate key**. Trình duyệt tải về một file `.json`.
3. Đổi tên thành `firebase-service-account.json` và đặt vào thư mục `EnterpriseManagement-Backend/EnterpriseManagement.Api/`.
4. Mở `EnterpriseManagement.Api/appsettings.json` (hoặc `appsettings.Development.json`) và điền:

   ```json
   "Firebase": {
     "CredentialsPath": "firebase-service-account.json"
   }
   ```
   (đường dẫn tương đối tính từ thư mục `EnterpriseManagement.Api`, hoặc điền đường dẫn tuyệt đối).
5. **File khoá này là bí mật** (ai có nó gửi được thông báo thay bạn). Nó đã nằm trong `.gitignore`; tuyệt đối không commit, không gửi qua chat.

> Cloud Messaging API (V1) thường đã bật sẵn. Nếu backend báo lỗi quyền, vào
> https://console.cloud.google.com → chọn đúng dự án → **APIs & Services → Library** → tìm
> **Firebase Cloud Messaging API** → **Enable**.

## Bước 4. Chạy lại

1. Khởi động lại backend. Log sẽ có dòng `Firebase đã sẵn sàng: bật gửi thông báo đẩy.`
   (nếu thấy `Firebase chưa được cấu hình...` nghĩa là file khoá chưa tìm thấy hoặc đường dẫn sai).
2. Build lại app trên điện thoại (cần có `google-services.json` trước khi build):
   ```bash
   flutter run -d <id-điện-thoại> --dart-define=API_HOST=localhost
   ```
3. Đăng nhập trên điện thoại → app xin quyền **Thông báo** (Android 13 trở lên) → bấm **Cho phép**.
4. Thử: dùng tài khoản nhân viên gửi đơn nghỉ, tài khoản quản lý đăng nhập trên điện thoại sẽ nhận thông báo (kể cả khi tắt màn hình/đóng app).

## Nhắc chấm công theo giờ

Backend tự gửi nhắc, cấu hình trong `appsettings.json`:

```json
"Reminders": {
  "Enabled": true,
  "CheckInTime": "07:45",
  "CheckOutTime": "17:00",
  "WindowMinutes": 30
}
```
- `CheckInTime`: nhắc **chấm công vào** cho nhân viên chưa chấm công và buổi sáng không nghỉ phép.
- `CheckOutTime`: nhắc **chấm công ra** cho nhân viên đã chấm công vào nhưng chưa ra.
- Chỉ gửi ngày làm việc (thứ Hai đến thứ Sáu), mỗi người mỗi loại một lần mỗi ngày.
- Backend phải đang chạy vào các giờ đó (nếu backend khởi động muộn trong `WindowMinutes` thì vẫn gửi bù).

## Các thông báo hệ thống gửi

| Sự kiện | Người nhận |
|---|---|
| Có đơn xin nghỉ / yêu cầu đi làm lại / điều chỉnh công mới | Quản lý trực tiếp |
| Đơn nghỉ được duyệt / bị từ chối | Nhân viên |
| Yêu cầu đi làm lại được duyệt / từ chối / được xác nhận | Nhân viên |
| Có yêu cầu đi làm lại hoặc điều chỉnh công chờ xử lý | Người có quyền xử lý (HR) |
| Quản lý đã xác nhận / điều chỉnh công được áp dụng / bị từ chối | Nhân viên |
| Nhân sự ghi nhận chấm công cho bạn | Nhân viên đó |
| Sắp đến giờ chấm công vào / hết giờ làm việc | Nhân viên |

## Lưu ý

- **iPhone** cần thêm tài khoản Apple Developer (trả phí) và cấu hình APNs; hướng dẫn này chỉ làm Android.
- **Chạy app trên Chrome** vẫn dùng được hộp Thông báo, nhưng không nhận thông báo đẩy.
- Thông báo đẩy gửi lỗi (mất mạng, token cũ...) không làm hỏng việc duyệt đơn/chấm công; token không còn hợp lệ tự bị dọn.
- Muốn tắt hẳn thông báo đẩy: để trống `Firebase:CredentialsPath`. Muốn tắt nhắc giờ: đặt `Reminders:Enabled` = `false`.
