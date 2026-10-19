using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Domain.Entities.Attendance;
using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Entities.Identity;
using EnterpriseManagement.Domain.Entities.Leave;
using EnterpriseManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseManagement.Infrastructure.Persistence;

// Seed 1 bộ dữ liệu công ty mẫu đầy đủ và liên kết chặt với nhau (phòng ban -> nhân viên ->
// tài khoản -> chấm công/nghỉ phép) để có sẵn data thật dùng thử toàn bộ tính năng, thay vì
// DB gần như trống. Chỉ chạy đúng 1 lần khi Departments đang rỗng (guard ở SeedAsync) —
// Program.cs chỉ gọi seeder này khi IsDevelopment().
public static class DemoDataSeeder
{
    private const string DemoPassword = "Demo@1234";

    private sealed record EmpSeed(
        string Key, string Username, string FullName, Gender Gender, DateOnly Dob, string Phone,
        string Address, string PositionCode, string DeptCode, int HireMonthsAgo,
        EmploymentStatus Status, int Level, string? ManagerKey);

    // Công ty mẫu ~100 người, 10 phòng, 5 cấp: Tổng giám đốc -> Giám đốc khối -> Trưởng phòng -> Trưởng nhóm -> Nhân viên.
    // 20 người đầu (e1-e20) giữ nguyên tài khoản dùng thử cũ; phần còn lại sinh theo sơ đồ cố định (Random có hạt giống) nên mỗi lần dựng lại DB đều giống nhau.
    // Dựng lười (không dùng khởi tạo tĩnh) vì cần các mảng tên khai báo bên dưới.
    private static EmpSeed[]? _employeeSeeds;
    private static EmpSeed[] EmployeeSeeds => _employeeSeeds ??= BuildEmployeeSeeds();

    private static readonly string[] Surnames =
    {
        "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng", "Bùi", "Đỗ", "Hồ", "Ngô", "Dương",
        "Lý", "Đinh", "Trịnh", "Tạ", "Cao", "Chu", "Lương", "Tống", "Kiều", "Thái", "Quách", "Mạc", "Châu",
    };

    private static readonly string[] MaleNames =
    {
        "Hùng", "Long", "Khánh", "Đức", "Tùng", "Phúc", "Sơn", "Minh", "Bình", "Toàn", "Kiên", "Nam", "Hải", "Dũng", "Hiếu", "Thành",
        "Tuấn", "Quân", "Huy", "Việt", "Trung", "Khoa", "Bảo", "Cường", "Đạt", "Lâm", "Phong", "Thắng", "Vinh", "Hậu", "Tài", "Luân",
    };

    private static readonly string[] FemaleNames =
    {
        "Mai", "Lan", "Hương", "Ngọc", "Thu", "Hạnh", "Nga", "Quỳnh", "Yến", "Linh", "Trang", "Phương", "Thảo", "Hà", "Oanh", "Nhung",
        "Vy", "Châu", "My", "Chi", "Diệp", "Giang", "Hằng", "Loan", "Ly", "Nhi", "Thủy", "Uyên", "Vân", "Xuân", "Anh", "Dung",
    };

    private static readonly string[] MaleMiddle = { "Văn", "Đức", "Minh", "Quang", "Hữu", "Thanh", "Anh", "Gia" };
    private static readonly string[] FemaleMiddle = { "Thị", "Ngọc", "Thu", "Thanh", "Bích", "Khánh", "Hồng", "Mỹ" };

    private static readonly string[] Streets =
    {
        "Lê Lợi, Quận 1", "Nguyễn Huệ, Quận 1", "Hai Bà Trưng, Quận 3", "Võ Văn Tần, Quận 3", "Cách Mạng Tháng 8, Quận 10",
        "Điện Biên Phủ, Bình Thạnh", "Phan Xích Long, Phú Nhuận", "Nguyễn Thị Minh Khai, Quận 1", "Trần Hưng Đạo, Quận 5",
        "Lý Thường Kiệt, Quận 10", "Nguyễn Văn Cừ, Quận 5", "Cộng Hòa, Tân Bình", "Phạm Văn Đồng, Thủ Đức", "Quang Trung, Gò Vấp",
    };

    private static EmpSeed[] BuildEmployeeSeeds()
    {
        var list = new List<EmpSeed>();
        var rng = new Random(20261019);
        var usedNames = new HashSet<string>();
        var n = 0;
        const int baselineYear = 2026;

        string Phone() => (901000100L + n).ToString("0000000000");
        string Address() => $"{rng.Next(1, 300)} {Streets[rng.Next(Streets.Length)]}, TP.HCM";
        DateOnly Dob(int minAge, int maxAge) => new(baselineYear - rng.Next(minAge, maxAge + 1), rng.Next(1, 13), rng.Next(1, 29));

        // Người có sẵn: giữ tên, tài khoản và quan hệ báo cáo; chỉ đưa vào sơ đồ mới.
        void Fixed(string key, string fullName, Gender gender, DateOnly dob, string address, string position, string dept,
            int months, EmploymentStatus status, int level, string? manager)
        {
            n++;
            usedNames.Add(fullName);
            list.Add(new EmpSeed(key, $"e{n}", fullName, gender, dob, Phone(), address, position, dept, months, status, level, manager));
        }

        // Người sinh thêm: tên Việt không trùng, tuổi và thâm niên hợp với cấp bậc.
        string Gen(string key, string dept, string position, int level, string manager, int minMonths, int maxMonths, int minAge, int maxAge)
        {
            n++;
            string fullName;
            Gender gender;
            do
            {
                gender = rng.Next(100) < 48 ? Gender.Female : Gender.Male;
                var given = gender == Gender.Female ? FemaleNames[rng.Next(FemaleNames.Length)] : MaleNames[rng.Next(MaleNames.Length)];
                var middle = gender == Gender.Female ? FemaleMiddle[rng.Next(FemaleMiddle.Length)] : MaleMiddle[rng.Next(MaleMiddle.Length)];
                fullName = $"{Surnames[rng.Next(Surnames.Length)]} {middle} {given}";
            }
            while (!usedNames.Add(fullName));

            var months = rng.Next(minMonths, maxMonths + 1);
            var status = months <= 2 ? EmploymentStatus.Probation : EmploymentStatus.Active;
            list.Add(new EmpSeed(key, $"e{n}", fullName, gender, Dob(minAge, maxAge), Phone(), Address(), position, dept, months, status, level, manager));
            return key;
        }

        void Staff(string prefix, int count, string dept, string position, int level, string manager)
        {
            for (var i = 1; i <= count; i++) Gen($"{prefix}{i}", dept, position, level, manager, 1, 48, 22, 42);
        }

        // ---- e1-e20: tài khoản dùng thử cũ ----
        Fixed("CEO", "Nguyễn Văn Hùng", Gender.Male, new(1978, 3, 14), "12 Lê Lợi, Quận 1, TP.HCM", "CEO", "EXE", 36, EmploymentStatus.Active, 0, null);
        Fixed("SALES_HEAD", "Trần Thị Mai", Gender.Female, new(1985, 7, 22), "45 Nguyễn Huệ, Quận 1, TP.HCM", "HEAD", "SALES", 24, EmploymentStatus.Active, 2, "DIR_COM");
        Fixed("HR_HEAD", "Dương Thị Thu", Gender.Female, new(1986, 11, 2), "78 Hai Bà Trưng, Quận 3, TP.HCM", "HEAD", "HR", 24, EmploymentStatus.Active, 2, "DIR_FIN");
        Fixed("IT_HEAD", "Hồ Văn Kiên", Gender.Male, new(1984, 1, 19), "23 Cách Mạng Tháng 8, Quận 10, TP.HCM", "HEAD", "IT", 24, EmploymentStatus.Active, 2, "DIR_TECH");
        Fixed("ACC_HEAD", "Tô Thị Nga", Gender.Female, new(1987, 5, 9), "56 Điện Biên Phủ, Bình Thạnh, TP.HCM", "HEAD", "ACC", 24, EmploymentStatus.Active, 2, "DIR_FIN");
        Fixed("TEAM_LEAD_A", "Lê Văn Long", Gender.Male, new(1990, 9, 30), "89 Võ Văn Tần, Quận 3, TP.HCM", "LEAD", "SALES", 18, EmploymentStatus.Active, 3, "SALES_HEAD");
        Fixed("TEAM_LEAD_B", "Phạm Văn Khánh", Gender.Male, new(1991, 2, 11), "34 Pasteur, Quận 1, TP.HCM", "LEAD", "SALES", 18, EmploymentStatus.Active, 3, "SALES_HEAD");
        Fixed("SALES_A1", "Hoàng Thị Lan", Gender.Female, new(1994, 6, 25), "12 Lý Tự Trọng, Quận 1, TP.HCM", "SALESREP", "SALES", 14, EmploymentStatus.Active, 4, "TEAM_LEAD_A");
        Fixed("SALES_A2", "Vũ Văn Đức", Gender.Male, new(1993, 12, 8), "67 Nam Kỳ Khởi Nghĩa, Quận 3, TP.HCM", "SALESREP", "SALES", 10, EmploymentStatus.Active, 4, "TEAM_LEAD_A");
        Fixed("SALES_A3", "Đặng Thị Hương", Gender.Female, new(1996, 4, 17), "90 Trần Hưng Đạo, Quận 5, TP.HCM", "SALESREP", "SALES", 6, EmploymentStatus.Active, 4, "TEAM_LEAD_A");
        Fixed("SALES_B1", "Bùi Văn Tùng", Gender.Male, new(1992, 8, 3), "21 Nguyễn Thị Minh Khai, Quận 1, TP.HCM", "SALESREP", "SALES", 12, EmploymentStatus.Active, 4, "TEAM_LEAD_B");
        Fixed("SALES_B2", "Đỗ Thị Ngọc", Gender.Female, new(1995, 10, 21), "54 Cống Quỳnh, Quận 1, TP.HCM", "SALESREP", "SALES", 8, EmploymentStatus.Active, 4, "TEAM_LEAD_B");
        Fixed("SALES_B3", "Ngô Văn Phúc", Gender.Male, new(1999, 1, 30), "76 Lê Văn Sỹ, Phú Nhuận, TP.HCM", "SALESREP", "SALES", 2, EmploymentStatus.Probation, 4, "TEAM_LEAD_B");
        Fixed("HR_1", "Lý Văn Sơn", Gender.Male, new(1993, 3, 12), "15 Hoàng Diệu, Quận 4, TP.HCM", "HRSTAFF", "HR", 9, EmploymentStatus.Active, 3, "HR_HEAD");
        Fixed("HR_2", "Phan Thị Hạnh", Gender.Female, new(1996, 7, 6), "38 Nguyễn Đình Chiểu, Quận 3, TP.HCM", "HRSTAFF", "HR", 5, EmploymentStatus.Active, 3, "HR_HEAD");
        Fixed("IT_1", "Trịnh Văn Minh", Gender.Male, new(1992, 5, 27), "60 Ba Tháng Hai, Quận 10, TP.HCM", "DEV", "IT", 15, EmploymentStatus.Active, 4, "LEAD_BE");
        Fixed("IT_2", "Đinh Thị Yến", Gender.Female, new(1994, 9, 14), "82 Sư Vạn Hạnh, Quận 10, TP.HCM", "DEV", "IT", 11, EmploymentStatus.Active, 4, "LEAD_BE");
        Fixed("IT_3", "Cao Văn Bình", Gender.Male, new(1997, 11, 23), "29 Tô Hiến Thành, Quận 10, TP.HCM", "DEV", "IT", 4, EmploymentStatus.Active, 4, "LEAD_FE");
        Fixed("ACC_1", "Mai Văn Toàn", Gender.Male, new(1993, 2, 16), "41 Lê Quang Định, Bình Thạnh, TP.HCM", "ACCSTAFF", "ACC", 13, EmploymentStatus.Active, 3, "ACC_HEAD");
        Fixed("ACC_2", "Chu Thị Quỳnh", Gender.Female, new(1995, 12, 5), "63 Phan Xích Long, Phú Nhuận, TP.HCM", "ACCSTAFF", "ACC", 7, EmploymentStatus.Active, 3, "ACC_HEAD");

        // ---- Cấp giám đốc (3 khối) và trợ lý Tổng giám đốc ----
        Gen("DIR_COM", "EXE", "DIRECTOR", 1, "CEO", 48, 72, 40, 50);   // Khối Thương mại: Kinh doanh, Marketing, CSKH
        Gen("DIR_TECH", "EXE", "DIRECTOR", 1, "CEO", 48, 72, 40, 50);  // Khối Công nghệ: Kỹ thuật, Triển khai & Tư vấn
        Gen("DIR_FIN", "EXE", "DIRECTOR", 1, "CEO", 48, 72, 40, 50);   // Khối Tài chính - Vận hành: Kế toán, Nhân sự, Hành chính, Vận hành
        Gen("ASSIST", "EXE", "ASSISTANT", 1, "CEO", 12, 36, 26, 34);

        // ---- Trưởng phòng mới ----
        Gen("MKT_HEAD", "MKT", "HEAD", 2, "DIR_COM", 30, 60, 34, 44);
        Gen("CS_HEAD", "CS", "HEAD", 2, "DIR_COM", 30, 60, 34, 44);
        Gen("IMP_HEAD", "IMP", "HEAD", 2, "DIR_TECH", 30, 60, 34, 44);
        Gen("ADM_HEAD", "ADM", "HEAD", 2, "DIR_FIN", 30, 60, 34, 46);
        Gen("OPS_HEAD", "OPS", "HEAD", 2, "DIR_FIN", 30, 60, 34, 46);

        // ---- Kinh doanh: 3 nhóm + 1 trợ lý kinh doanh ----
        Gen("TEAM_LEAD_C", "SALES", "LEAD", 3, "SALES_HEAD", 18, 36, 30, 38);
        Staff("SALES_A_N", 3, "SALES", "SALESREP", 4, "TEAM_LEAD_A");
        Staff("SALES_B_N", 3, "SALES", "SALESREP", 4, "TEAM_LEAD_B");
        Staff("SALES_C_N", 6, "SALES", "SALESREP", 4, "TEAM_LEAD_C");
        Gen("SALES_ADMIN", "SALES", "ASSISTANT", 3, "SALES_HEAD", 6, 30, 24, 32);

        // ---- Kỹ thuật: 5 nhóm (Backend, Frontend/Mobile, QA, Hạ tầng, Sản phẩm/BA) ----
        foreach (var (key, _) in new[] { ("LEAD_BE", 0), ("LEAD_FE", 0), ("LEAD_QA", 0), ("LEAD_INFRA", 0), ("LEAD_BA", 0) })
        {
            Gen(key, "IT", "LEAD", 3, "IT_HEAD", 20, 48, 30, 40);
        }
        Staff("BE_N", 3, "IT", "DEV", 4, "LEAD_BE");
        Staff("FE_N", 5, "IT", "DEV", 4, "LEAD_FE");
        Staff("QA_N", 3, "IT", "QA", 4, "LEAD_QA");
        Staff("INFRA_N", 2, "IT", "INFRA", 4, "LEAD_INFRA");
        Staff("BA_N", 3, "IT", "BA", 4, "LEAD_BA");

        // ---- Nhân sự (3-4 người, đúng tỷ lệ ~2,5-3,5 trên 100 nhân viên), Hành chính, Kế toán ----
        Staff("HR_N", 1, "HR", "HRSTAFF", 3, "HR_HEAD");
        Staff("ACC_N", 4, "ACC", "ACCSTAFF", 3, "ACC_HEAD");
        Staff("ADM_N", 3, "ADM", "ADMSTAFF", 3, "ADM_HEAD");

        // ---- Marketing, Chăm sóc khách hàng, Vận hành, Triển khai & Tư vấn ----
        Staff("MKT_N", 6, "MKT", "MKTSTAFF", 3, "MKT_HEAD");
        Gen("CS_LEAD", "CS", "LEAD", 3, "CS_HEAD", 18, 36, 28, 36);
        Gen("CS_LEAD2", "CS", "LEAD", 3, "CS_HEAD", 18, 36, 28, 36);
        Staff("CS_N", 4, "CS", "CSAGENT", 4, "CS_LEAD");
        Staff("CS2_N", 3, "CS", "CSAGENT", 4, "CS_LEAD2");
        Staff("OPS_N", 4, "OPS", "OPSSTAFF", 3, "OPS_HEAD");
        Gen("IMP_LEAD1", "IMP", "LEAD", 3, "IMP_HEAD", 18, 40, 30, 38);
        Gen("IMP_LEAD2", "IMP", "LEAD", 3, "IMP_HEAD", 18, 40, 30, 38);
        Staff("IMP_A_N", 4, "IMP", "CONSULT", 4, "IMP_LEAD1");
        Staff("IMP_B_N", 3, "IMP", "CONSULT", 4, "IMP_LEAD2");

        return list.ToArray();
    }

    private static readonly (string DeptCode, string HeadKey)[] DepartmentHeads =
    {
        ("EXE", "CEO"), ("SALES", "SALES_HEAD"), ("HR", "HR_HEAD"), ("IT", "IT_HEAD"), ("ACC", "ACC_HEAD"),
        ("MKT", "MKT_HEAD"), ("ADM", "ADM_HEAD"), ("CS", "CS_HEAD"), ("OPS", "OPS_HEAD"), ("IMP", "IMP_HEAD"),
    };

    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        // Phòng ban SYS của tài khoản hệ thống admin (DataSeeder tạo) không tính là "đã có dữ liệu".
        if (await context.Departments.AnyAsync(d => d.DepartmentCode != DataSeeder.SystemDepartmentCode))
        {
            return;
        }

        var now = VietnamClock.Now;
        var today = VietnamClock.Today;

        var positions = await EnsurePositionsAsync(context, now);

        var departments = await SeedDepartmentsAsync(context, now);
        var employees = await SeedEmployeesAsync(context, now, departments, positions);
        await BackfillDepartmentManagersAsync(context, departments, employees);
        await SeedUsersAsync(context, passwordHasher, now, employees);

        var leaveTypes = await context.LeaveTypes.ToDictionaryAsync(lt => lt.LeaveTypeCode);
        var approvedLeaveDates = await SeedLeaveAsync(context, now, today, employees, leaveTypes);
        await SeedAttendanceAsync(context, now, today, employees, approvedLeaveDates);
    }

    // Vai trò mặc định theo chức vụ (cũng ghi vào Position.RoleCode khi chức vụ được tạo ở đây).
    // Tài khoản demo dùng bảng này để không phụ thuộc vào RoleCode Admin đã sửa tay.
    private static readonly HashSet<string> ManagerPositions = new() { "CEO", "DIRECTOR", "HEAD", "DEPUTY", "MANAGER", "LEAD" };

    private static string RoleCodeFor(string positionCode) =>
        positionCode == "HRSTAFF" ? "HR" : ManagerPositions.Contains(positionCode) ? "MANAGER" : "EMPLOYEE";

    // PositionSeeder chỉ seed HEAD/DEPUTY/MANAGER/STAFF khi bảng Positions đang rỗng — nếu Admin
    // đã tự tạo chức vụ khác qua UI trước khi seed này chạy (bảng không còn rỗng), 4 mã trên có
    // thể chưa từng tồn tại. Tự đảm bảo đủ 4 mã cần dùng ở đây (chỉ tạo mã nào còn thiếu, không
    // đụng chức vụ Admin đã tự tạo) thay vì phụ thuộc giả định PositionSeeder đã chạy.
    private static async Task<Dictionary<string, Position>> EnsurePositionsAsync(ApplicationDbContext context, DateTime now)
    {
        var required = new (string Code, string Name)[]
        {
            ("HEAD", "Trưởng phòng"),
            ("DEPUTY", "Phó phòng"),
            ("MANAGER", "Quản lý"),
            ("STAFF", "Nhân viên"),
            ("HRSTAFF", "Chuyên viên nhân sự"),
            ("CEO", "Tổng giám đốc"),
            ("DIRECTOR", "Giám đốc khối"),
            ("LEAD", "Trưởng nhóm"),
            ("ASSISTANT", "Trợ lý"),
            ("SALESREP", "Chuyên viên kinh doanh"),
            ("DEV", "Lập trình viên"),
            ("QA", "Kiểm thử viên"),
            ("BA", "Phân tích nghiệp vụ"),
            ("INFRA", "Kỹ sư hạ tầng"),
            ("MKTSTAFF", "Chuyên viên Marketing"),
            ("CSAGENT", "Chuyên viên chăm sóc khách hàng"),
            ("CONSULT", "Chuyên viên triển khai"),
            ("ACCSTAFF", "Kế toán viên"),
            ("ADMSTAFF", "Chuyên viên hành chính"),
            ("OPSSTAFF", "Chuyên viên vận hành"),
        };

        var positions = await context.Positions.ToDictionaryAsync(p => p.PositionCode);
        var changed = false;
        foreach (var (code, name) in required)
        {
            if (positions.ContainsKey(code)) continue;

            var position = new Position
            {
                PositionCode = code,
                PositionName = name,
                RoleCode = RoleCodeFor(code),
                IsActive = true,
                CreatedAt = now,
            };
            context.Positions.Add(position);
            positions[code] = position;
            changed = true;
        }

        if (changed)
        {
            await context.SaveChangesAsync();
        }

        return positions;
    }

    private static async Task<Dictionary<string, Department>> SeedDepartmentsAsync(ApplicationDbContext context, DateTime now)
    {
        var departments = new Dictionary<string, Department>
        {
            ["EXE"] = new() { DepartmentCode = "EXE", DepartmentName = "Ban Giám đốc", IsActive = true, CreatedAt = now },
            ["SALES"] = new() { DepartmentCode = "SALES", DepartmentName = "Phòng Kinh doanh", IsActive = true, CreatedAt = now },
            ["HR"] = new() { DepartmentCode = "HR", DepartmentName = "Phòng Nhân sự", IsActive = true, CreatedAt = now },
            ["IT"] = new() { DepartmentCode = "IT", DepartmentName = "Phòng Kỹ thuật", IsActive = true, CreatedAt = now },
            ["ACC"] = new() { DepartmentCode = "ACC", DepartmentName = "Phòng Kế toán", IsActive = true, CreatedAt = now },
            ["MKT"] = new() { DepartmentCode = "MKT", DepartmentName = "Phòng Marketing", IsActive = true, CreatedAt = now },
            ["ADM"] = new() { DepartmentCode = "ADM", DepartmentName = "Phòng Hành chính", IsActive = true, CreatedAt = now },
            ["CS"] = new() { DepartmentCode = "CS", DepartmentName = "Phòng Chăm sóc khách hàng", IsActive = true, CreatedAt = now },
            ["OPS"] = new() { DepartmentCode = "OPS", DepartmentName = "Phòng Vận hành & Mua hàng", IsActive = true, CreatedAt = now },
            ["IMP"] = new() { DepartmentCode = "IMP", DepartmentName = "Phòng Triển khai & Tư vấn", IsActive = true, CreatedAt = now },
        };

        context.Departments.AddRange(departments.Values);
        await context.SaveChangesAsync();
        return departments;
    }

    private static async Task<Dictionary<string, Employee>> SeedEmployeesAsync(
        ApplicationDbContext context, DateTime now, Dictionary<string, Department> departments, Dictionary<string, Position> positions)
    {
        var usedCodes = new HashSet<string>();
        var byKey = new Dictionary<string, Employee>();

        foreach (var level in EmployeeSeeds.Select(s => s.Level).Distinct().OrderBy(l => l))
        {
            var batch = EmployeeSeeds.Where(s => s.Level == level).ToList();
            foreach (var seed in batch)
            {
                var spaceIndex = seed.FullName.LastIndexOf(' ');
                var firstName = seed.FullName[..spaceIndex];
                var lastName = seed.FullName[(spaceIndex + 1)..];

                var employee = new Employee
                {
                    EmployeeCode = NextEmployeeCode(usedCodes),
                    FirstName = firstName,
                    LastName = lastName,
                    DateOfBirth = seed.Dob,
                    Gender = seed.Gender,
                    Phone = seed.Phone,
                    Email = $"{seed.Username}@ems-demo.vn",
                    Address = seed.Address,
                    DepartmentId = departments[seed.DeptCode].Id,
                    PositionId = positions[seed.PositionCode].Id,
                    ManagerId = seed.ManagerKey is null ? null : byKey[seed.ManagerKey].Id,
                    HireDate = DateOnly.FromDateTime(now).AddMonths(-seed.HireMonthsAgo),
                    EmploymentStatus = seed.Status,
                    // Tổng giám đốc và giám đốc khối không thuộc diện chấm công: giờ giấc do họ tự quyết, chỉ cấp dưới mới chấm công thực tế.
                    TracksAttendance = seed.Level > 1 || seed.Key == "ASSIST",
                    CreatedAt = now,
                };

                context.Employees.Add(employee);
                byKey[seed.Key] = employee;
            }

            await context.SaveChangesAsync();
        }

        return byKey;
    }

    private static string NextEmployeeCode(HashSet<string> used)
    {
        string code;
        do
        {
            code = RandomCodeGenerator.Generate(8);
        }
        while (!used.Add(code));

        return code;
    }

    private static async Task BackfillDepartmentManagersAsync(
        ApplicationDbContext context, Dictionary<string, Department> departments, Dictionary<string, Employee> employees)
    {
        foreach (var (deptCode, headKey) in DepartmentHeads)
        {
            departments[deptCode].ManagerId = employees[headKey].Id;
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(
        ApplicationDbContext context, IPasswordHasher passwordHasher, DateTime now, Dictionary<string, Employee> employees)
    {
        var roles = await context.Roles.ToDictionaryAsync(r => r.RoleCode);
        var positionsById = await context.Positions.ToDictionaryAsync(p => p.Id);
        var passwordHash = passwordHasher.Hash(DemoPassword);

        foreach (var seed in EmployeeSeeds)
        {
            var employee = employees[seed.Key];
            var position = positionsById[employee.PositionId];
            var roleCode = RoleCodeFor(position.PositionCode);
            if (!roles.TryGetValue(roleCode, out var role)) continue;

            var user = new User
            {
                EmployeeId = employee.Id,
                Username = seed.Username,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = now,
            };
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = now });
            // Trưởng phòng nhân sự (vừa là quản lý của team HR) là người duy nhất chốt bảng công tháng.
            if (seed.Key == "HR_HEAD" && roles.TryGetValue(HrRoleSeeder.LeadRoleCode, out var hrLead))
            {
                user.UserRoles.Add(new UserRole { Role = hrLead, AssignedAt = now });
            }

            context.Users.Add(user);
        }

        await context.SaveChangesAsync();
    }

    // Trả về map EmployeeId -> danh sách ngày đã được duyệt nghỉ, để bước chấm công phía sau
    // biết ngày nào phải đánh dấu OnLeave thay vì Present/Late/Absent — 2 bảng luôn khớp nhau.
    private static async Task<Dictionary<long, List<DateOnly>>> SeedLeaveAsync(
        ApplicationDbContext context, DateTime now, DateOnly today,
        Dictionary<string, Employee> employees, Dictionary<string, LeaveType> leaveTypes)
    {
        var weekdays = LastWeekdays(today, 5);
        var approvedDate1 = weekdays[1];
        var approvedDate2 = weekdays[3];

        var balances = new List<LeaveBalance>();
        foreach (var seed in EmployeeSeeds)
        {
            var employee = employees[seed.Key];
            if (!employee.TracksAttendance) continue;
            foreach (var leaveType in leaveTypes.Values)
            {
                var month = leaveType.AccrualPeriod == LeaveAccrualPeriod.MonthlyReset ? today.Month : (int?)null;
                var allocated = CalculateDefaultAllocation(leaveType, employee.CreatedAt, today.Year);

                var used = 0m;
                if (leaveType.LeaveTypeCode == "ANNUAL" && seed.Key == "HR_1") used = 1m;
                if (leaveType.LeaveTypeCode == "ANNUAL" && seed.Key == "IT_2") used = 0.5m;

                balances.Add(new LeaveBalance
                {
                    EmployeeId = employee.Id,
                    LeaveTypeId = leaveType.Id,
                    Year = today.Year,
                    Month = month,
                    Unit = leaveType.AccrualUnit,
                    AllocatedTime = allocated,
                    UsedTime = used,
                    RemainingTime = allocated - used,
                    CreatedAt = now,
                });
            }
        }
        context.LeaveBalances.AddRange(balances);

        var hr1 = employees["HR_1"];
        var it2 = employees["IT_2"];
        var acc2 = employees["ACC_2"];
        var salesA2 = employees["SALES_A2"];
        var it3 = employees["IT_3"];
        var acc1 = employees["ACC_1"];

        var requests = new List<LeaveRequest>
        {
            new()
            {
                EmployeeId = hr1.Id,
                LeaveTypeId = leaveTypes["ANNUAL"].Id,
                StartDate = approvedDate1.ToDateTime(TimeOnly.MinValue),
                EndDate = approvedDate1.ToDateTime(TimeOnly.MinValue),
                Session = LeaveSession.FullDay,
                Unit = LeaveUnit.Days,
                TotalTime = 1,
                Reason = "Việc gia đình",
                Status = LeaveRequestStatus.Approved,
                ApprovedBy = employees["HR_HEAD"].Id,
                ApprovedAt = now,
                CreatedAt = now,
            },
            new()
            {
                EmployeeId = it2.Id,
                LeaveTypeId = leaveTypes["ANNUAL"].Id,
                StartDate = approvedDate2.ToDateTime(TimeOnly.MinValue),
                EndDate = approvedDate2.ToDateTime(TimeOnly.MinValue),
                Session = LeaveSession.Morning,
                Unit = LeaveUnit.Days,
                TotalTime = 0.5m,
                Reason = "Khám sức khoẻ định kỳ",
                Status = LeaveRequestStatus.Approved,
                ApprovedBy = employees["IT_HEAD"].Id,
                ApprovedAt = now,
                CreatedAt = now,
            },
            new()
            {
                EmployeeId = acc2.Id,
                LeaveTypeId = leaveTypes["ANNUAL"].Id,
                StartDate = today.AddDays(3).ToDateTime(TimeOnly.MinValue),
                EndDate = today.AddDays(3).ToDateTime(TimeOnly.MinValue),
                Session = LeaveSession.FullDay,
                Unit = LeaveUnit.Days,
                TotalTime = 1,
                Reason = "Đám cưới người thân",
                Status = LeaveRequestStatus.Pending,
                CreatedAt = now,
            },
            new()
            {
                EmployeeId = it3.Id,
                LeaveTypeId = leaveTypes["ANNUAL"].Id,
                StartDate = today.AddDays(-10).ToDateTime(TimeOnly.MinValue),
                EndDate = today.AddDays(-9).ToDateTime(TimeOnly.MinValue),
                Session = LeaveSession.FullDay,
                Unit = LeaveUnit.Days,
                TotalTime = 2,
                Reason = "Việc cá nhân",
                Status = LeaveRequestStatus.Rejected,
                RejectionReason = "Phòng đang thiếu người giai đoạn này, vui lòng dời lịch nghỉ.",
                ApprovedBy = employees["IT_HEAD"].Id,
                ApprovedAt = now,
                CreatedAt = now,
            },
            new()
            {
                EmployeeId = acc1.Id,
                LeaveTypeId = leaveTypes["ANNUAL"].Id,
                StartDate = today.AddDays(-15).ToDateTime(TimeOnly.MinValue),
                EndDate = today.AddDays(-15).ToDateTime(TimeOnly.MinValue),
                Session = LeaveSession.FullDay,
                Unit = LeaveUnit.Days,
                TotalTime = 1,
                Reason = "Bận việc đột xuất, xin huỷ",
                Status = LeaveRequestStatus.Cancelled,
                CreatedAt = now,
            },
        };
        context.LeaveRequests.AddRange(requests);
        await context.SaveChangesAsync();

        var approvedByEmployee = new Dictionary<long, List<DateOnly>>
        {
            [hr1.Id] = new() { approvedDate1 },
            [it2.Id] = new() { approvedDate2 },
        };
        return approvedByEmployee;
    }

    private static decimal CalculateDefaultAllocation(LeaveType leaveType, DateTime employeeCreatedAt, int year)
    {
        switch (leaveType.AccrualPeriod)
        {
            case LeaveAccrualPeriod.MonthlyReset:
                return leaveType.AccrualAmount;
            case LeaveAccrualPeriod.FlatYearly:
                return leaveType.AccrualAmount;
            case LeaveAccrualPeriod.ProratedYearly:
                if (year < employeeCreatedAt.Year) return 0;
                var monthsRemaining = year == employeeCreatedAt.Year ? 13 - employeeCreatedAt.Month : 12;
                return leaveType.AccrualAmount * monthsRemaining;
            default:
                return 0;
        }
    }

    private static List<DateOnly> LastWeekdays(DateOnly today, int count)
    {
        var result = new List<DateOnly>();
        var cursor = today;
        while (result.Count < count)
        {
            if (cursor.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                result.Add(cursor);
            }
            cursor = cursor.AddDays(-1);
        }
        result.Reverse();
        return result;
    }

    private static async Task SeedAttendanceAsync(
        ApplicationDbContext context, DateTime now, DateOnly today,
        Dictionary<string, Employee> employees, Dictionary<long, List<DateOnly>> approvedLeaveDates)
    {
        var weekdays = LastWeekdays(today, 5);
        var random = Random.Shared;
        var records = new Dictionary<(string Key, DateOnly Date), AttendanceRecord>();

        foreach (var seed in EmployeeSeeds)
        {
            var employee = employees[seed.Key];
            if (!employee.TracksAttendance) continue;
            foreach (var date in weekdays)
            {
                var onLeave = approvedLeaveDates.TryGetValue(employee.Id, out var dates) && dates.Contains(date);

                AttendanceRecord record;
                if (onLeave)
                {
                    record = new AttendanceRecord
                    {
                        EmployeeId = employee.Id,
                        AttendanceDate = date,
                        Status = AttendanceStatus.OnLeave,
                        CreatedAt = now,
                    };
                }
                else
                {
                    // ~6% vắng không phép, ~15% đi trễ, còn lại đúng giờ — đủ đa dạng để test UI
                    // lọc/hiển thị trạng thái mà không cần random thật (roll theo Id+ngày để ổn định).
                    var roll = (employee.Id * 7 + date.DayNumber) % 100;
                    if (roll < 6)
                    {
                        record = new AttendanceRecord
                        {
                            EmployeeId = employee.Id,
                            AttendanceDate = date,
                            Status = AttendanceStatus.Absent,
                            Note = "Vắng không báo trước",
                            CreatedAt = now,
                        };
                    }
                    else
                    {
                        var isLate = roll < 21;
                        var checkIn = date.ToDateTime(isLate
                            ? new TimeOnly(8, 35).AddMinutes(random.Next(0, 25))
                            : new TimeOnly(8, 0).AddMinutes(random.Next(0, 25)));
                        var checkOut = date.ToDateTime(new TimeOnly(17, 0).AddMinutes(random.Next(0, 60)));

                        record = new AttendanceRecord
                        {
                            EmployeeId = employee.Id,
                            AttendanceDate = date,
                            CheckInTime = checkIn,
                            CheckOutTime = checkOut,
                            WorkingHours = (decimal)(checkOut - checkIn).TotalHours,
                            Status = isLate ? AttendanceStatus.Late : AttendanceStatus.Present,
                            CreatedAt = now,
                        };
                    }
                }

                context.AttendanceRecords.Add(record);
                records[(seed.Key, date)] = record;
            }
        }

        await context.SaveChangesAsync();

        var adjustmentRequester = employees["SALES_A3"];
        var pendingTarget = records[("SALES_A3", weekdays[0])];
        var approvedRequester = employees["IT_1"];
        var approvedTarget = records[("IT_1", weekdays[2])];

        context.AttendanceAdjustments.AddRange(
            new AttendanceAdjustment
            {
                AttendanceId = pendingTarget.Id,
                RequestedBy = adjustmentRequester.Id,
                Reason = "Check-in bị ghi nhận trễ do lỗi mạng, thực tế đã có mặt đúng giờ.",
                OldCheckInTime = pendingTarget.CheckInTime,
                NewCheckInTime = weekdays[0].ToDateTime(new TimeOnly(8, 5)),
                Status = ApprovalStatus.Pending,
                CreatedAt = now,
            },
            new AttendanceAdjustment
            {
                AttendanceId = approvedTarget.Id,
                RequestedBy = approvedRequester.Id,
                Reason = "Quên bấm check-out, nhờ điều chỉnh lại đúng giờ ra về thực tế.",
                OldCheckOutTime = approvedTarget.CheckOutTime,
                NewCheckOutTime = weekdays[2].ToDateTime(new TimeOnly(18, 15)),
                Status = ApprovalStatus.Approved,
                ApprovedBy = employees["IT_HEAD"].Id,
                ApprovedAt = now,
                CreatedAt = now,
            });

        await context.SaveChangesAsync();
    }
}
