# Kế hoạch: gộp Hồ sơ nhân viên và Tài khoản đăng nhập

## 1. Vì sao và quyết định thiết kế

Hiện hệ thống tách **Hồ sơ nhân viên** (`Employees`) và **Tài khoản** (`Users`): quan hệ nhiều-một lỏng lẻo (`Users.EmployeeId` cho phép trống, một nhân viên có thể có nhiều tài khoản, `admin` không có hồ sơ), email nằm ở hai nơi, hai trang quản lý riêng (Quản lý nhân viên / Tài khoản), phải tạo hồ sơ rồi mới tạo tài khoản. Việc tách này chưa mang lại lợi ích nào rõ ràng nhưng gây lỗi (admin không nhận được thông báo, nhắc chấm công, "Tạo tài khoản" phải chọn vai trò thủ công...).

**Quyết định (đề xuất của tôi, có thể đổi nếu bạn muốn):**

- **Mỗi người = đúng 1 hồ sơ + đúng 1 tài khoản (quan hệ 1-1, bắt buộc)**, tạo cùng lúc, sửa cùng một màn hình, khoá cùng nhau.
- Vẫn giữ **hai bảng** `Employees` và `Users` (Users coi như "thông tin đăng nhập" của Employee). Lý do: gộp hẳn thành một bảng phải đổi khoá ngoại của hơn 20 bảng (`AuditLogs.UserId`, `UserRoles`, token JWT, `ICurrentUserService.UserId`...) mà không thêm giá trị cho người dùng; ràng buộc 1-1 + giao diện/API gộp đã đạt mục tiêu. Nếu sau này vẫn muốn gộp bảng vật lý thì làm ở đợt riêng.
- **`admin` cũng có hồ sơ nhân viên** (mã cố định `ADMIN`, phòng ban `SYS`, chức vụ `SYSADMIN`), nhưng là **tài khoản hệ thống không chấm công** (`Employees.TracksAttendance = false`): không bị tính "vắng mặt", không nhận nhắc chấm công, không vào bảng công/KPI.
- **Email chỉ còn ở hồ sơ** (`Employees.Email`, bắt buộc, duy nhất). Bỏ cột `Users.Email`.
- Quy tắc nghỉ việc/khoá: **Cho nghỉ việc** = trạng thái làm việc `Resigned` + khoá tài khoản; **Phục hồi** = `Active` + mở tài khoản (bỏ quy tắc cũ "phục hồi không tự mở khoá"). **Khoá tài khoản tạm** (không nghỉ việc) vẫn có, chỉ khoá đăng nhập.
- Bỏ màn hình/menu **"Tài khoản"** (`/admin/users`) trên web và mobile; mọi thao tác nằm ở **"Quản lý nhân viên"**.

## 2. Hợp đồng API (cả 3 agent bám đúng)

### 2.1 `EmployeeDto` (GET `/employees`, `/employees/{code}`, và các API team/org-tree dùng chung DTO này)
Giữ mọi trường hiện có và **thêm**:
```json
{
  "username": "e8",
  "roleCodes": ["EMPLOYEE"],
  "roleNames": ["Nhân viên"],
  "isAccountActive": true,
  "lastLoginAt": "2026-10-16T17:40:00"   // có thể null
}
```
`email` trở thành bắt buộc (chuỗi, không null).

### 2.2 Tạo nhân viên: `POST /employees` (quyền `employee.manage`)
Request = các trường cũ của `CreateEmployeeRequest` (firstName, lastName, dateOfBirth, gender, phone, address, departmentCode, positionCode, managerCode, hireDate, employmentStatus…) **thêm**:
```json
{ "username": "nguyenvana", "email": "a@congty.vn", "roleCode": "EMPLOYEE" }
```
- `username`: 3-50 ký tự `[A-Za-z0-9._-]`, duy nhất. `email`: hợp lệ, duy nhất. `roleCode`: tuỳ chọn; bỏ trống thì lấy `Position.RoleCode` của chức vụ, nếu chức vụ không có thì `EMPLOYEE`.
- Response `200`: `CreateEmployeeResult`
```json
{ "employee": { /* EmployeeDto */ }, "generatedPassword": "AbC123xYz9" }
```
Mật khẩu sinh ngẫu nhiên, chỉ trả lúc tạo (và khi đặt lại).

### 2.3 Sửa nhân viên: `PUT /employees/{code}` (quyền `employee.manage`)
`UpdateEmployeeRequest` = trường cũ **thêm** `email` (tuỳ chọn, bỏ = giữ nguyên) và `roleCodes` (tuỳ chọn, bỏ = giữ nguyên; nếu gửi thì phải ≥ 1 vai trò, mã tồn tại; không được tự gỡ vai trò `ADMIN` của chính mình).

### 2.4 Nghỉ việc/phục hồi: `PUT /employees/{code}/active` `{ "isActive": bool }` (quyền `employee.manage`)
`false` → `EmploymentStatus = Resigned` + `Users.IsActive = false`; `true` → `Active` + `Users.IsActive = true`. Không cho tự khoá/cho nghỉ chính mình.

### 2.5 Tài khoản (giữ `UsersController`, quyền `user.manage`)
- `PUT /users/{username}/active` `{ "isActive": bool }`: khoá/mở đăng nhập (không đổi trạng thái làm việc).
- `POST /users/{username}/reset-password` → `{ "username": "...", "generatedPassword": "..." }`.
- `PUT /users/{username}/roles` `{ "roleCodes": [...] }` (giữ cho tiện, UI chính dùng `PUT /employees/{code}` có `roleCodes`).
- **Bỏ**: `POST /users` (tạo tài khoản rời) và `PUT /users/{username}` (liên kết hồ sơ/đổi email). `GET /users` có thể giữ (trả như cũ nhưng `employeeCode` luôn có giá trị) hoặc bỏ nếu không còn nơi nào dùng.

### 2.6 Đăng nhập / token
`LoginResult` và JWT **luôn có `employeeCode`** (kể cả admin). `ICurrentUserService.EmployeeCode` không còn null.

## 3. Dữ liệu và migration

Một migration EF `MergeEmployeeAndUser` (đặt tên tuỳ agent, phải có `Down` hợp lý hoặc ghi rõ không thể quay lại):
1. Thêm `Employees.TracksAttendance bit NOT NULL DEFAULT 1`.
2. Seed dữ liệu cho admin: phòng ban `SYS` ("Quản trị hệ thống"), chức vụ `SYSADMIN` ("Quản trị viên", `RoleCode = ADMIN`, không cấp bậc lương nếu bắt buộc thì để 0), nhân viên `ADMIN` (họ "Quản trị", tên "Hệ thống", `HireDate` = ngày chạy, `TracksAttendance = 0`, `Email` = email của user admin), gán `Users.EmployeeId` của `admin`.
3. Nhân viên chưa có tài khoản: tạo tài khoản **khoá** (`IsActive = 0`, username = mã nhân viên viết thường, mật khẩu băm của một chuỗi ngẫu nhiên không ai biết) để Admin đặt lại mật khẩu khi cần. (DB dev hiện có 20 nhân viên đều đã có tài khoản.)
4. Nhân viên có **nhiều** tài khoản: migration phải **dừng với thông báo rõ** (không xoá âm thầm). (DB dev hiện không có trường hợp này.)
5. Đồng bộ email: `Employees.Email := Users.Email` (email tài khoản thắng vì đã bắt buộc + duy nhất), rồi `Employees.Email NOT NULL` + chỉ mục duy nhất, bỏ cột `Users.Email`.
6. `Users.EmployeeId NOT NULL` + chỉ mục duy nhất.
7. Bỏ menu `ADMIN_USERS` ("Tài khoản") và permission `page.admin.users` (và `MenuPermissions`/`RolePermissions` liên quan); cập nhật `MenuSeeder` để không seed lại. Giữ nguyên permission `user.manage`.

**Trước khi chạy migration trên DB dev: sao lưu** (ví dụ `BACKUP DATABASE` trong container SQL Server hoặc xuất đếm số bản ghi các bảng chính), sau khi chạy so sánh lại số lượng và đăng nhập thử.

## 4. Hệ quả cho các tính năng khác (do `TracksAttendance = false` và admin có hồ sơ)
- `AttendanceReminderService`: bỏ qua nhân viên không chấm công.
- `EffectiveDayCalculator`/`EffectiveAttendanceService`: ngày làm việc đã qua mà không có dữ liệu → **không** tính `Absent` cho nhân viên không chấm công (trả `None`). Chấm công thật nếu có vẫn được hiển thị.
- Danh sách bảng công (`GetAllAsync`, theo phòng ban, theo team, HR) bỏ nhân viên không chấm công.
- Xin nghỉ phép: tài khoản hệ thống không xin nghỉ phép (báo "Tài khoản hệ thống không dùng chức năng nghỉ phép."). Không tạo số dư phép cho họ.
- Dashboard/Cây tổ chức/KPI/danh sách chọn nhân viên (quản lý, trưởng phòng...): không đưa tài khoản hệ thống vào danh sách chọn làm quản lý/trưởng phòng/gán KPI; trong cây tổ chức của Admin vẫn hiện nút gốc bình thường (không cần che).
- Thông báo (`NotificationService`): admin giờ có `EmployeeCode` nên hộp thông báo hoạt động (Admin nhận thông báo như mọi người, nếu có phát sinh).
- `UsersController`/`AuthController`/`CurrentUserService`: bỏ các nhánh xử lý "không có hồ sơ".

## 5. Giao diện

### Web (`EnterpriseManagement-FE/src`)
- Trang duy nhất **"Quản lý nhân viên"** (`/admin/employees`, `AdminEmployeesPage` trong `pages/AdminPages.tsx`):
  - Bảng: Mã, Họ tên, Tên đăng nhập, Vai trò, Phòng ban, Chức vụ, Trạng thái làm việc, Tài khoản (Hoạt động/Đã khoá), Lần đăng nhập cuối (nếu có).
  - **Tạo** nhân viên: form hồ sơ + phần "Tài khoản" (tên đăng nhập, email, vai trò — mặc định theo chức vụ, đổi được). Sau khi tạo mở hộp thoại hiển thị **mật khẩu vừa sinh** (nút sao chép, cảnh báo chỉ hiện một lần).
  - **Sửa**: hồ sơ + email + vai trò (chọn nhiều).
  - Thao tác trên dòng: Sửa, Đặt lại mật khẩu (xác nhận → hiện mật khẩu mới), Khoá/Mở tài khoản, Cho nghỉ việc/Phục hồi (nội dung xác nhận nói rõ tài khoản bị khoá/mở theo).
  - Xoá nút "Tạo tài khoản" cũ và toàn bộ trang **"Tài khoản"** (`AdminUsersApiPage` trong `AdminApiFeedbackPages.tsx`, route `/admin/users`, mục menu tĩnh trong `AppShell.tsx`, route trong `App.tsx`). `/admin/users` chuyển hướng sang `/admin/employees`.
- Trang Hồ sơ của tôi: hiện tên đăng nhập và vai trò.
- Cập nhật `types/domain.ts`, `services/api.ts`, test `src/test/routes.test.tsx`.

### Mobile (`EnterpriseManagement-MB/lib`)
- Màn **"Quản lý nhân viên" của Admin** (`features/admin/screens/admin_employees_screen.dart` + provider + model) gộp toàn bộ chức năng như web (tạo kèm tài khoản + hiện mật khẩu sinh, sửa kèm email/vai trò, đặt lại mật khẩu, khoá/mở tài khoản, nghỉ việc/phục hồi).
- Xoá màn **"Tài khoản"** (`admin_users_screen.dart`, `admin_users_provider.dart`, `models/admin_user.dart`), bỏ route `/admin/users` khỏi `app/feature_registry.dart` và mọi chỗ tham chiếu (`admin_service.dart`, `system_menus_tab.dart` nếu liệt kê route).
- Giữ co giãn theo cỡ màn hình/cỡ chữ (xem `test/*_overflow_test.dart` làm mẫu, thêm test cho form mới).

## 6. Phân công 3 agent (làm song song, bám hợp đồng mục 2)

| Agent | Phạm vi | Không được đụng |
|---|---|---|
| **A – Backend** | Toàn bộ `EnterpriseManagement-Backend`: entity, cấu hình EF, migration + dữ liệu, `EmployeeService`/`UserService`/`AuthController`/`UsersController`/`EmployeesController`, `DataSeeder`, `MenuSeeder`, JWT, loại trừ `TracksAttendance` (mục 4), test đơn vị, `SETUP.md`, **kịch bản E2E gọi API thật** | `EnterpriseManagement-FE`, `EnterpriseManagement-MB` |
| **B – Web** | `EnterpriseManagement-FE/src` (mục 5) | Backend, mobile |
| **C – Mobile** | `EnterpriseManagement-MB/lib` + `test` (mục 5) | Backend, web |

Agent B và C **chưa gọi được API mới** cho tới khi A xong; họ kiểm tra bằng `tsc`/`oxlint`/`vitest` và `flutter analyze`/`flutter test`. Sau khi A xong, người điều phối (tôi) chạy backend mới và kiểm tra tích hợp web/mobile bằng giao diện thật.

## 7. Tiêu chí nghiệm thu
1. Tạo nhân viên mới ở web → hiện mật khẩu sinh → đăng nhập được bằng tài khoản đó, vai trò đúng theo chức vụ.
2. `admin` có hồ sơ (`ADMIN`), đăng nhập được, nhận được thông báo nếu có; **không** hiện "vắng mặt" ở bảng công, không nhận nhắc chấm công, không chọn được làm quản lý/trưởng phòng/KPI.
3. Cho nghỉ việc → tài khoản bị khoá và không đăng nhập được; Phục hồi → đăng nhập lại được.
4. Không còn trang/menu "Tài khoản" trên web và mobile; `/admin/users` trên web chuyển sang Quản lý nhân viên.
5. Không có nhân viên nào không có tài khoản hoặc ngược lại (kiểm bằng truy vấn); email duy nhất.
6. Mọi test đạt: backend `dotnet test`, web `tsc` + `vitest`, mobile `flutter analyze` + `flutter test`; E2E của Agent A đạt.
7. Dữ liệu cũ (nghỉ phép, chấm công, thông báo, KPI, bán hàng) nguyên vẹn sau migration (so sánh số lượng bản ghi trước/sau).
