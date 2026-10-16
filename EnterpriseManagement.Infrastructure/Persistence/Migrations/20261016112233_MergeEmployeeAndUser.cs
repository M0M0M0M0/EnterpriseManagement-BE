using System.Security.Cryptography;
using EnterpriseManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseManagement.Infrastructure.Persistence.Migrations
{
    // Gộp Hồ sơ nhân viên và Tài khoản: mỗi nhân viên đúng 1 tài khoản (Users.EmployeeId bắt buộc + duy nhất),
    // email chỉ còn ở Employees (bắt buộc + duy nhất), admin có hồ sơ hệ thống ADMIN (không chấm công),
    // bỏ menu "Tài khoản" (ADMIN_USERS / page.admin.users). Không đụng PasswordHash của tài khoản đã có.
    /// <inheritdoc />
    public partial class MergeEmployeeAndUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0. Một nhân viên có nhiều tài khoản: dừng hẳn, không tự xoá tài khoản nào. Admin phải tự gộp/xoá trước.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Users WHERE EmployeeId IS NOT NULL GROUP BY EmployeeId HAVING COUNT(*) > 1)
BEGIN
    DECLARE @list nvarchar(max) = (
        SELECT STRING_AGG(CONCAT(e.EmployeeCode, N' (', x.Cnt, N' tài khoản)'), N', ')
        FROM (SELECT EmployeeId, COUNT(*) AS Cnt FROM Users WHERE EmployeeId IS NOT NULL GROUP BY EmployeeId HAVING COUNT(*) > 1) x
        JOIN Employees e ON e.Id = x.EmployeeId);
    DECLARE @msg nvarchar(2048) = CONCAT(
        N'Migration MergeEmployeeAndUser dừng: các nhân viên sau đang có nhiều hơn 1 tài khoản, cần xoá/gộp thủ công để mỗi nhân viên còn đúng 1 tài khoản rồi chạy lại: ',
        @list);
    THROW 50001, @msg, 1;
END");

            // 1. Cột mới.
            migrationBuilder.AddColumn<bool>(
                name: "TracksAttendance",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleCode",
                table: "Positions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Vai trò mặc định cho 4 chức vụ mẫu (giống cách tài khoản demo được gán vai trò).
            migrationBuilder.Sql(@"
UPDATE Positions SET RoleCode = N'MANAGER' WHERE RoleCode IS NULL AND PositionCode IN (N'HEAD', N'DEPUTY', N'MANAGER');
UPDATE Positions SET RoleCode = N'EMPLOYEE' WHERE RoleCode IS NULL AND PositionCode = N'STAFF';");

            // 2. Tài khoản chưa có hồ sơ (admin, và nếu có thì các tài khoản rời khác): tạo hồ sơ hệ thống không chấm công
            //    trong phòng ban SYS / chức vụ SYSADMIN. admin nhận mã ADMIN; tài khoản khác nhận mã SYS-U{Id}.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Users WHERE EmployeeId IS NULL)
BEGIN
    DECLARE @now datetime2 = DATEADD(HOUR, 7, SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM Departments WHERE DepartmentCode = N'SYS')
        INSERT INTO Departments (DepartmentCode, DepartmentName, Description, IsActive, CreatedAt)
        VALUES (N'SYS', N'Quản trị hệ thống', N'Phòng ban chứa tài khoản hệ thống (không chấm công).', 1, @now);

    IF NOT EXISTS (SELECT 1 FROM Positions WHERE PositionCode = N'SYSADMIN')
        INSERT INTO Positions (PositionCode, PositionName, RoleCode, IsActive, CreatedAt)
        VALUES (N'SYSADMIN', N'Quản trị viên', N'ADMIN', 1, @now);

    DECLARE @deptId bigint = (SELECT Id FROM Departments WHERE DepartmentCode = N'SYS');
    DECLARE @posId bigint = (SELECT Id FROM Positions WHERE PositionCode = N'SYSADMIN');

    SELECT u.Id AS UserId, u.Username, u.Email,
           CASE WHEN u.Username = N'admin' AND NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeCode = N'ADMIN')
                THEN N'ADMIN' ELSE CONCAT(N'SYS-U', u.Id) END AS Code
    INTO #Orphans
    FROM Users u WHERE u.EmployeeId IS NULL;

    INSERT INTO Employees (EmployeeCode, FirstName, LastName, Email, DepartmentId, PositionId, HireDate,
                           EmploymentStatus, TracksAttendance, CreatedAt)
    SELECT o.Code,
           CASE WHEN o.Code = N'ADMIN' THEN N'Quản trị' ELSE N'Tài khoản' END,
           CASE WHEN o.Code = N'ADMIN' THEN N'Hệ thống' ELSE LEFT(o.Username, 100) END,
           o.Email, @deptId, @posId, CAST(@now AS date), N'Active', 0, @now
    FROM #Orphans o;

    UPDATE u SET u.EmployeeId = e.Id
    FROM Users u
    JOIN #Orphans o ON o.UserId = u.Id
    JOIN Employees e ON e.EmployeeCode = o.Code;

    DROP TABLE #Orphans;
END");

            // 3. Email: email của tài khoản thắng (vốn đã bắt buộc + duy nhất).
            migrationBuilder.Sql(@"
UPDATE e SET e.Email = u.Email
FROM Employees e JOIN Users u ON u.EmployeeId = e.Id;");

            // Nhân viên chưa có tài khoản mà email trống hoặc trùng người khác: gán email tạm theo mã nhân viên để còn duy nhất.
            migrationBuilder.Sql(@"
UPDATE e SET e.Email = CONCAT(LOWER(e.EmployeeCode), N'@chua-co-email.local')
FROM Employees e
WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.EmployeeId = e.Id)
  AND (e.Email IS NULL OR LTRIM(RTRIM(e.Email)) = N''
       OR EXISTS (SELECT 1 FROM Employees e2
                  WHERE e2.Email = e.Email AND e2.Id <> e.Id
                    AND (EXISTS (SELECT 1 FROM Users u2 WHERE u2.EmployeeId = e2.Id) OR e2.Id < e.Id)));");

            // 4. Nhân viên chưa có tài khoản: tạo tài khoản KHOÁ (username = mã nhân viên viết thường), mật khẩu là băm
            //    của một chuỗi ngẫu nhiên không ai biết; Admin đặt lại mật khẩu và mở khoá khi cần.
            //    Vai trò theo chức vụ (Positions.RoleCode), không có thì EMPLOYEE.
            var unknownPasswordHash = new PasswordHasher().Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            migrationBuilder.Sql($@"
IF EXISTS (SELECT 1 FROM Employees e WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.EmployeeId = e.Id))
BEGIN
    DECLARE @now datetime2 = DATEADD(HOUR, 7, SYSUTCDATETIME());

    SELECT e.Id AS EmployeeId,
           CASE WHEN EXISTS (SELECT 1 FROM Users u WHERE u.Username = LOWER(e.EmployeeCode))
                THEN CONCAT(LOWER(e.EmployeeCode), N'-', e.Id) ELSE LOWER(e.EmployeeCode) END AS Username,
           COALESCE(p.RoleCode, N'EMPLOYEE') AS RoleCode
    INTO #NoAccount
    FROM Employees e JOIN Positions p ON p.Id = e.PositionId
    WHERE NOT EXISTS (SELECT 1 FROM Users u WHERE u.EmployeeId = e.Id);

    INSERT INTO Users (EmployeeId, Username, Email, PasswordHash, IsActive, CreatedAt)
    SELECT n.EmployeeId, n.Username, e.Email, N'{unknownPasswordHash}', 0, @now
    FROM #NoAccount n JOIN Employees e ON e.Id = n.EmployeeId;

    INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
    SELECT u.Id, COALESCE(r.Id, rEmp.Id), @now
    FROM #NoAccount n
    JOIN Users u ON u.EmployeeId = n.EmployeeId
    LEFT JOIN Roles r ON r.RoleCode = n.RoleCode
    LEFT JOIN Roles rEmp ON rEmp.RoleCode = N'EMPLOYEE'
    WHERE COALESCE(r.Id, rEmp.Id) IS NOT NULL;

    DROP TABLE #NoAccount;
END");

            // 5. Ràng buộc mới: bỏ Users.Email, Employees.Email bắt buộc + duy nhất, Users.EmployeeId bắt buộc + duy nhất.
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.AlterColumn<long>(
                name: "EmployeeId",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            // 6. Bỏ menu "Tài khoản" và permission trang page.admin.users (giữ nguyên permission hành động user.manage).
            migrationBuilder.Sql(@"
DECLARE @permId bigint = (SELECT Id FROM Permissions WHERE PermissionCode = N'page.admin.users');
DELETE mp FROM MenuPermissions mp JOIN Menus m ON m.Id = mp.MenuId WHERE m.MenuCode = N'ADMIN_USERS';
IF @permId IS NOT NULL
BEGIN
    DELETE FROM MenuPermissions WHERE PermissionId = @permId;
    DELETE FROM RolePermissions WHERE PermissionId = @permId;
    DELETE FROM Permissions WHERE Id = @permId;
END
UPDATE Menus SET ParentMenuId = NULL WHERE ParentMenuId IN (SELECT Id FROM Menus WHERE MenuCode = N'ADMIN_USERS');
DELETE FROM Menus WHERE MenuCode = N'ADMIN_USERS';");
        }

        // Down chỉ khôi phục LƯỢC ĐỒ (Users.Email lấy lại từ Employees.Email, EmployeeId cho phép trống, bỏ cột mới).
        // Không khôi phục được: menu/permission "Tài khoản" (đã xoá), và hồ sơ ADMIN / tài khoản khoá đã tạo vẫn được giữ.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email",
                table: "Employees");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE u SET u.Email = e.Email FROM Users u JOIN Employees e ON e.Id = u.EmployeeId;");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "RoleCode",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "TracksAttendance",
                table: "Employees");

            migrationBuilder.AlterColumn<long>(
                name: "EmployeeId",
                table: "Users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Employees",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId");
        }
    }
}
