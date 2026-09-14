# Setup hướng dẫn chạy dự án (Backend + Frontend)

Dùng file này khi clone lại 2 repo (`EnterpriseManagement-Backend` và
`EnterpriseManagement-FE`) trên máy mới. Không chỉ có mỗi lệnh seed admin —
cần chạy đủ các bước bên dưới theo đúng thứ tự thì mới lên được.

## 0. Yêu cầu cài sẵn trên máy

- Docker Desktop (đang chạy)
- .NET SDK 10.0+
- Node.js 20+ và npm
- Git

## 1. Clone 2 repo (đặt cạnh nhau trong 1 thư mục cha)

```bash
git clone https://github.com/M0M0M0M0/EnterpriseManagement-Backend.git
git clone https://github.com/phanminhnhatPMN/EnterpriseManagement-FE.git
```

## 2. Bật database bằng Docker

```bash
cd EnterpriseManagement-Backend
docker compose up -d
```

Container SQL Server sẽ chạy ở cổng `1433` (user `sa`, password
`YourStrong!Passw0rd` — khai báo sẵn trong `docker-compose.yml` /
`appsettings.json`).

**Quan trọng: đợi khoảng 15–30 giây trước khi sang bước tiếp theo.** Docker
báo container là "Up" gần như ngay lập tức, nhưng SQL Server *bên trong*
container cần thêm thời gian mới thực sự nhận kết nối được. Chạy migration
(bước 4) quá sớm sẽ bị lỗi:

```
A network-related or instance-specific error occurred while establishing a
connection to SQL Server. ... The wait operation timed out.
```

Kiểm tra SQL Server đã sẵn sàng chưa bằng cách xem log, chờ tới khi thấy dòng
`SQL Server is now ready for client connections`:

```bash
docker logs -f enterprise-management-sqlserver
```

(Nhấn `Ctrl+C` để thoát xem log sau khi thấy dòng đó, container vẫn chạy
bình thường.)

## 3. Cài `dotnet-ef` (chỉ cần làm 1 lần trên máy, nếu chưa có)

```bash
dotnet tool install --global dotnet-ef
```

Kiểm tra đã cài chưa:

```bash
dotnet ef --version
```

## 4. Tạo bảng trong database (chạy migration)

Chạy từ thư mục `EnterpriseManagement-Backend`:

```bash
dotnet ef database update --project EnterpriseManagement.Infrastructure --startup-project EnterpriseManagement.Api
```

Lệnh này tạo toàn bộ bảng theo các migration đã có sẵn trong repo. Chỉ cần
chạy lại nếu có migration mới, hoặc database bị xóa/tạo lại từ đầu.

## 5. Chạy Backend API

```bash
cd EnterpriseManagement.Api
dotnet run
```

- API chạy tại `http://localhost:5068`
- **Lần chạy đầu tiên** (khi bảng `Roles`/`Users` đang rỗng), `DataSeeder` sẽ
  tự động tạo:
  - 3 role: `ADMIN`, `MANAGER`, `EMPLOYEE`
  - 1 tài khoản admin: username `admin` / password `admin`
    (mật khẩu yếu, chỉ dùng để dev local — không dùng ngoài môi trường thật)
- **Cũng lần chạy đầu tiên** (khi bảng `Menus` đang rỗng), `MenuSeeder` tự
  seed 18 Permission + Menu + gán vào 3 role, khớp với sidebar hiện tại. Đây
  là dữ liệu cho tính năng Role/Permission/Menu — Admin có thể sửa qua màn
  hình **User/Role/Permission** và **System Administration** sau khi đăng
  nhập, không cần đụng vào code.

Không cần chạy thêm lệnh seed riêng nào — seed tự chạy mỗi lần start API,
và chỉ tạo dữ liệu khi bảng đang rỗng nên chạy lại nhiều lần cũng an toàn.

- **Cũng lần chạy đầu tiên, chỉ khi môi trường là `Development`** (khi bảng
  `Departments` đang rỗng), `DemoDataSeeder` tự tạo 1 bộ dữ liệu công ty mẫu
  đầy đủ để có sẵn data dùng thử toàn bộ tính năng: 5 phòng ban, 20 nhân
  viên (phân cấp CEO → Trưởng phòng → Trưởng nhóm → Nhân viên), 20 tài
  khoản `e1`..`e20` — **mật khẩu chung: `Demo@1234`** (`e1` = CEO, xem trong
  `DemoDataSeeder.cs` để biết `e{n}` nào ứng với ai) — cùng chấm công tuần
  gần nhất, số dư/đơn nghỉ phép, KPI Plan, khách hàng, sale và lịch sử hoa
  hồng đã seed sẵn, liên kết nhất quán với nhau. Chỉ chạy 1 lần; muốn seed
  lại từ đầu thì xoá sạch data (xem mục 5.5) rồi chạy lại.

## 5.5. Khi pull code mới mà seeder thay đổi (permission, position, leave type...)

Seeder (`MenuSeeder`, `ActionPermissionSeeder`, `LeaveTypeSeeder`, `PositionSeeder`...)
chỉ tự chạy khi bảng tương ứng đang **rỗng** — nếu máy đã từng chạy app rồi
(DB đã có data), seeder thấy bảng không rỗng thì bỏ qua, **không** tự sửa lại
theo data mới trong code. Gặp tình huống này khi thấy Permission/Menu/Position
trên máy mình khác với những gì vừa pull về.

Cách nhanh nhất (mất hết data cũ — chỉ dùng cho máy dev/test, không tiếc data):

```bash
dotnet ef database drop -f --project EnterpriseManagement.Infrastructure --startup-project EnterpriseManagement.Api
dotnet ef database update --project EnterpriseManagement.Infrastructure --startup-project EnterpriseManagement.Api
```

Chạy 2 lệnh trên (từ thư mục `EnterpriseManagement-Backend`, xong đợi Docker
container sẵn sàng như bước 2), rồi `dotnet run` lại — DB rỗng hoàn toàn nên
mọi seeder tự chạy lại từ đầu, ra đúng data mới nhất.

Nếu muốn giữ lại data thật (nhân viên, tài khoản...) và chỉ cần cập nhật
riêng phần Role/Permission/Menu, xóa tay đúng 4 bảng này trước khi `dotnet run`
thay vì drop cả DB:

```sql
DELETE FROM RolePermissions;
DELETE FROM MenuPermissions;
DELETE FROM Menus;
DELETE FROM Permissions;
```

## 6. Chạy Frontend

Mở terminal khác, từ thư mục `EnterpriseManagement-FE`:

```bash
npm install
npm run dev
```

- App chạy tại `http://localhost:5173`
- Đăng nhập bằng `admin` / `admin` ở `/login` — Employee/Manager (tạo qua
  màn hình Admin sau khi đăng nhập) cũng đăng nhập chung ở đây.

Frontend gọi API qua đường dẫn tương đối `/api`, được Vite tự động proxy
sang `http://localhost:5068` (cấu hình trong `vite.config.ts`, áp dụng cho cả
`npm run dev` và `npm run preview`) — không cần chỉnh gì thêm.

## Lưu ý

- Backend đang cấu hình CORS chỉ cho phép `http://localhost:5173` và
  `http://127.0.0.1:5173` (xem `Program.cs`). Nếu FE chạy ở cổng khác thì
  phải thêm origin đó vào `AddCors`.
- Muốn xóa sạch database và làm lại từ đầu:
  ```bash
  docker compose down -v
  ```
  rồi lặp lại từ bước 2.
- Danh sách cổng đang dùng: `1433` (SQL Server), `5068` (API), `5173`
  (Frontend dev server).
- Nếu trình duyệt đang có sẵn phiên đăng nhập từ trước khi pull code mới
  (token cũ lưu trong `localStorage`), sidebar có thể thiếu menu hoặc
  `/api/menus/mine` trả về rỗng — vì quyền (permission) được nhúng vào token
  lúc đăng nhập, token cũ không có claim mới. Chỉ cần **đăng xuất rồi đăng
  nhập lại** là được, không phải lỗi.

## Troubleshooting: lỗi kết nối SQL Server khi chạy migration

```
A network-related or instance-specific error occurred while establishing a
connection to SQL Server. The server was not found or was not accessible...
The wait operation timed out.
```

Lỗi này nghĩa là máy không kết nối được tới `localhost:1433`. Thứ tự kiểm
tra:

1. **Docker Desktop có đang chạy không?** Trên Windows/Mac phải mở app
   Docker Desktop trước, không chỉ có lệnh `docker` là đủ.
2. **Container có đang thực sự chạy, hay đã bị crash/restart liên tục?**
   Dùng `-a` để thấy cả container đã dừng/lỗi, không chỉ container đang chạy:
   ```bash
   docker ps -a
   ```
   - Nếu **không thấy container nào** tên `enterprise-management-sqlserver`:
     quay lại bước 2 trong hướng dẫn, chạy `docker compose up -d` (nhớ đứng
     đúng thư mục `EnterpriseManagement-Backend`, nơi có file
     `docker-compose.yml`).
   - Nếu trạng thái là `Exited` hoặc `Restarting` (không phải `Up`): container
     bị crash ngay sau khi start — xem mục 3 bên dưới, gần như chắc chắn là
     do thiếu RAM cấp cho Docker.
3. **Docker có đủ RAM cho SQL Server không?** SQL Server cần tối thiểu ~2GB
   RAM, nếu Docker Desktop đang giới hạn thấp hơn thì container sẽ khởi động
   rồi tự thoát ngay (crash loop), khiến máy client không bao giờ kết nối
   được dù đợi bao lâu. Kiểm tra log xem có dòng lỗi kiểu `Killed` hay
   container thoát đột ngột không:
   ```bash
   docker logs enterprise-management-sqlserver --tail 50
   ```
   Cách sửa: mở **Docker Desktop → Settings → Resources → Memory**, tăng lên
   tối thiểu **4GB**, Apply & Restart, rồi chạy lại `docker compose up -d`.
4. **SQL Server bên trong container đã sẵn sàng chưa?** Nếu log vẫn đang
   chạy các dòng `Recovery is complete` / `Starting up database` (và
   container ở trạng thái `Up`, không phải crash) thì cứ đợi thêm, sau đó
   chạy lại lệnh migration.
5. **Cổng 1433 có bị chiếm bởi SQL Server khác đã cài sẵn trên máy không?**
   Nếu máy đã cài SQL Server (không qua Docker) và nó cũng dùng cổng 1433,
   hai cái sẽ đụng nhau. Dừng SQL Server cài sẵn đó, hoặc đổi cổng map trong
   `docker-compose.yml` (ví dụ `"14330:1433"`) và sửa lại
   `DefaultConnection` trong `appsettings.json` cho khớp.
6. **Firewall / phần mềm diệt virus có chặn cổng 1433 không?** Một số máy
   công ty có firewall chặn kết nối TCP nội bộ tới cổng lạ. Thử tắt tạm
   firewall/antivirus để loại trừ nguyên nhân này, hoặc thêm exception cho
   cổng 1433 / cho Docker Desktop.
7. Nếu vẫn không được, thử xóa container cũ và tạo lại từ đầu:
   ```bash
   docker compose down -v
   docker compose up -d
   ```
   rồi đợi như bước 2 và chạy lại migration.
