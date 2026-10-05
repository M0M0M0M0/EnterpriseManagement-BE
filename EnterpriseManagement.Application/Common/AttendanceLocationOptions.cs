namespace EnterpriseManagement.Application.Common;

// Vị trí công ty được phép chấm công, đọc từ mục "Attendance" trong appsettings.json.
// Bán kính nên để từ khoảng 100m: GPS trong nhà thường lệch 10-50m, đặt quá chặt sẽ từ chối oan nhân viên đang ở công ty.
public class AttendanceLocationOptions
{
    public const string SectionName = "Attendance";

    public string OfficeName { get; set; } = "văn phòng công ty";
    public double OfficeLatitude { get; set; }
    public double OfficeLongitude { get; set; }
    public double AllowedRadiusMeters { get; set; } = 100;
}
