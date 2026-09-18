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

    private static readonly EmpSeed[] EmployeeSeeds =
    {
        new("CEO", "e1", "Nguyễn Văn Hùng", Gender.Male, new(1978, 3, 14), "0901000101", "12 Lê Lợi, Quận 1, TP.HCM", "HEAD", "EXE", 36, EmploymentStatus.Active, 0, null),

        new("SALES_HEAD", "e2", "Trần Thị Mai", Gender.Female, new(1985, 7, 22), "0901000102", "45 Nguyễn Huệ, Quận 1, TP.HCM", "DEPUTY", "SALES", 24, EmploymentStatus.Active, 1, "CEO"),
        new("HR_HEAD", "e3", "Dương Thị Thu", Gender.Female, new(1986, 11, 2), "0901000103", "78 Hai Bà Trưng, Quận 3, TP.HCM", "DEPUTY", "HR", 24, EmploymentStatus.Active, 1, "CEO"),
        new("IT_HEAD", "e4", "Hồ Văn Kiên", Gender.Male, new(1984, 1, 19), "0901000104", "23 Cách Mạng Tháng 8, Quận 10, TP.HCM", "DEPUTY", "IT", 24, EmploymentStatus.Active, 1, "CEO"),
        new("ACC_HEAD", "e5", "Tô Thị Nga", Gender.Female, new(1987, 5, 9), "0901000105", "56 Điện Biên Phủ, Bình Thạnh, TP.HCM", "DEPUTY", "ACC", 24, EmploymentStatus.Active, 1, "CEO"),

        new("TEAM_LEAD_A", "e6", "Lê Văn Long", Gender.Male, new(1990, 9, 30), "0901000106", "89 Võ Văn Tần, Quận 3, TP.HCM", "MANAGER", "SALES", 18, EmploymentStatus.Active, 2, "SALES_HEAD"),
        new("TEAM_LEAD_B", "e7", "Phạm Văn Khánh", Gender.Male, new(1991, 2, 11), "0901000107", "34 Pasteur, Quận 1, TP.HCM", "MANAGER", "SALES", 18, EmploymentStatus.Active, 2, "SALES_HEAD"),

        new("SALES_A1", "e8", "Hoàng Thị Lan", Gender.Female, new(1994, 6, 25), "0901000108", "12 Lý Tự Trọng, Quận 1, TP.HCM", "STAFF", "SALES", 14, EmploymentStatus.Active, 3, "TEAM_LEAD_A"),
        new("SALES_A2", "e9", "Vũ Văn Đức", Gender.Male, new(1993, 12, 8), "0901000109", "67 Nam Kỳ Khởi Nghĩa, Quận 3, TP.HCM", "STAFF", "SALES", 10, EmploymentStatus.Active, 3, "TEAM_LEAD_A"),
        new("SALES_A3", "e10", "Đặng Thị Hương", Gender.Female, new(1996, 4, 17), "0901000110", "90 Trần Hưng Đạo, Quận 5, TP.HCM", "STAFF", "SALES", 6, EmploymentStatus.Active, 3, "TEAM_LEAD_A"),
        new("SALES_B1", "e11", "Bùi Văn Tùng", Gender.Male, new(1992, 8, 3), "0901000111", "21 Nguyễn Thị Minh Khai, Quận 1, TP.HCM", "STAFF", "SALES", 12, EmploymentStatus.Active, 3, "TEAM_LEAD_B"),
        new("SALES_B2", "e12", "Đỗ Thị Ngọc", Gender.Female, new(1995, 10, 21), "0901000112", "54 Cống Quỳnh, Quận 1, TP.HCM", "STAFF", "SALES", 8, EmploymentStatus.Active, 3, "TEAM_LEAD_B"),
        new("SALES_B3", "e13", "Ngô Văn Phúc", Gender.Male, new(1999, 1, 30), "0901000113", "76 Lê Văn Sỹ, Phú Nhuận, TP.HCM", "STAFF", "SALES", 2, EmploymentStatus.Probation, 3, "TEAM_LEAD_B"),

        new("HR_1", "e14", "Lý Văn Sơn", Gender.Male, new(1993, 3, 12), "0901000114", "15 Hoàng Diệu, Quận 4, TP.HCM", "STAFF", "HR", 9, EmploymentStatus.Active, 3, "HR_HEAD"),
        new("HR_2", "e15", "Phan Thị Hạnh", Gender.Female, new(1996, 7, 6), "0901000115", "38 Nguyễn Đình Chiểu, Quận 3, TP.HCM", "STAFF", "HR", 5, EmploymentStatus.Active, 3, "HR_HEAD"),

        new("IT_1", "e16", "Trịnh Văn Minh", Gender.Male, new(1992, 5, 27), "0901000116", "60 Ba Tháng Hai, Quận 10, TP.HCM", "STAFF", "IT", 15, EmploymentStatus.Active, 3, "IT_HEAD"),
        new("IT_2", "e17", "Đinh Thị Yến", Gender.Female, new(1994, 9, 14), "0901000117", "82 Sư Vạn Hạnh, Quận 10, TP.HCM", "STAFF", "IT", 11, EmploymentStatus.Active, 3, "IT_HEAD"),
        new("IT_3", "e18", "Cao Văn Bình", Gender.Male, new(1997, 11, 23), "0901000118", "29 Tô Hiến Thành, Quận 10, TP.HCM", "STAFF", "IT", 4, EmploymentStatus.Active, 3, "IT_HEAD"),

        new("ACC_1", "e19", "Mai Văn Toàn", Gender.Male, new(1993, 2, 16), "0901000119", "41 Lê Quang Định, Bình Thạnh, TP.HCM", "STAFF", "ACC", 13, EmploymentStatus.Active, 3, "ACC_HEAD"),
        new("ACC_2", "e20", "Chu Thị Quỳnh", Gender.Female, new(1995, 12, 5), "0901000120", "63 Phan Xích Long, Phú Nhuận, TP.HCM", "STAFF", "ACC", 7, EmploymentStatus.Active, 3, "ACC_HEAD"),
    };

    private static readonly (string DeptCode, string HeadKey)[] DepartmentHeads =
    {
        ("EXE", "CEO"), ("SALES", "SALES_HEAD"), ("HR", "HR_HEAD"), ("IT", "IT_HEAD"), ("ACC", "ACC_HEAD"),
    };

    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Departments.AnyAsync())
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

    // Chức vụ giờ chỉ là chức danh hiển thị (không còn RoleCode) — role cho tài khoản demo được
    // suy ra riêng trong SeedUsersAsync qua RoleCodeByPosition bên dưới, không đọc từ Position.
    private static readonly Dictionary<string, string> RoleCodeByPosition = new()
    {
        ["HEAD"] = "MANAGER",
        ["DEPUTY"] = "MANAGER",
        ["MANAGER"] = "MANAGER",
        ["STAFF"] = "EMPLOYEE",
    };

    // PositionSeeder chỉ seed HEAD/DEPUTY/MANAGER/STAFF khi bảng Positions đang rỗng — nếu Admin
    // đã tự tạo chức vụ khác qua UI trước khi seed này chạy (bảng không còn rỗng), 4 mã trên có
    // thể chưa từng tồn tại. Tự đảm bảo đủ 4 mã cần dùng ở đây (chỉ tạo mã nào còn thiếu, không
    // đụng chức vụ Admin đã tự tạo) thay vì phụ thuộc giả định PositionSeeder đã chạy.
    private static async Task<Dictionary<string, Position>> EnsurePositionsAsync(ApplicationDbContext context, DateTime now)
    {
        var required = new (string Code, string Name, int Rank)[]
        {
            ("HEAD", "Trưởng phòng", 10),
            ("DEPUTY", "Phó phòng", 20),
            ("MANAGER", "Quản lý", 30),
            ("STAFF", "Nhân viên", 40),
        };

        var positions = await context.Positions.ToDictionaryAsync(p => p.PositionCode);
        var changed = false;
        foreach (var (code, name, rank) in required)
        {
            if (positions.ContainsKey(code)) continue;

            var position = new Position
            {
                PositionCode = code,
                PositionName = name,
                RankLevel = rank,
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
            var roleCode = RoleCodeByPosition.GetValueOrDefault(position.PositionCode, "EMPLOYEE");
            if (!roles.TryGetValue(roleCode, out var role)) continue;

            var user = new User
            {
                EmployeeId = employee.Id,
                Username = seed.Username,
                Email = employee.Email!,
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = now,
            };
            user.UserRoles.Add(new UserRole { Role = role, AssignedAt = now });
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
                EmployeeId = salesA2.Id,
                LeaveTypeId = leaveTypes["SHORT"].Id,
                StartDate = today.AddDays(1).ToDateTime(TimeOnly.MinValue),
                EndDate = today.AddDays(1).ToDateTime(TimeOnly.MinValue),
                Unit = LeaveUnit.Hours,
                TotalTime = 2,
                Reason = "Đi khám bệnh buổi sáng",
                Status = LeaveRequestStatus.Pending,
                CreatedAt = now,
            },
            new()
            {
                EmployeeId = it3.Id,
                LeaveTypeId = leaveTypes["UNPAID"].Id,
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
