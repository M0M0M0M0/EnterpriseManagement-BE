using System.Globalization;
using EnterpriseManagement.Api.Requests;
using EnterpriseManagement.Api.Security;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceAdjustmentService _attendanceAdjustmentService;
    private readonly ICurrentUserService _currentUserService;

    public AttendanceController(
        IAttendanceService attendanceService,
        IAttendanceAdjustmentService attendanceAdjustmentService,
        ICurrentUserService currentUserService)
    {
        _attendanceService = attendanceService;
        _attendanceAdjustmentService = attendanceAdjustmentService;
        _currentUserService = currentUserService;
    }

    private const long MaxPhotoBytes = 5 * 1024 * 1024;

    // Vị trí công ty và bán kính cho phép, để app hiển thị khoảng cách trước khi chấm công.
    [HttpGet("office-location")]
    public ActionResult<OfficeLocationDto> GetOfficeLocation()
    {
        return Ok(_attendanceService.GetOfficeLocation());
    }

    [HttpPost("punch")]
    [RequirePermission("attendance.punch")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<AttendanceRecordDto>> Punch([FromForm] PunchForm form)
    {
        if (!double.TryParse(form.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(form.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return BadRequest(new { message = "Thiếu hoặc sai toạ độ vị trí." });
        }

        // Ảnh chỉ bắt buộc khi check-in (service quyết định); check-out không cần, có gửi cũng không lưu.
        Stream? photoStream = null;
        string? extension = null;
        var photo = form.Photo;
        if (photo is { Length: > 0 })
        {
            if (photo.Length > MaxPhotoBytes)
            {
                return BadRequest(new { message = "Ảnh quá lớn, tối đa 5 MB." });
            }

            photoStream = photo.OpenReadStream();
            extension = await DetectImageExtensionAsync(photoStream);
            if (extension is null)
            {
                await photoStream.DisposeAsync();
                return BadRequest(new { message = "Ảnh phải là định dạng JPEG hoặc PNG." });
            }
        }

        try
        {
            var request = new PunchRequest
            {
                Latitude = latitude,
                Longitude = longitude,
                IsMockLocation = form.IsMockLocation,
                Photo = photoStream,
                PhotoExtension = extension ?? ".jpg"
            };

            var result = await _attendanceService.PunchAsync(_currentUserService.EmployeeCode, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            if (photoStream is not null)
            {
                await photoStream.DisposeAsync();
            }
        }
    }

    // Xác định định dạng theo vài byte đầu của file (không tin tên file hay Content-Type do client gửi),
    // rồi đưa con trỏ về đầu để đọc lại toàn bộ.
    private static async Task<string?> DetectImageExtensionAsync(Stream stream)
    {
        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(0, 4));
        stream.Seek(0, SeekOrigin.Begin);

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        if (read >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            return ".png";
        }

        return null;
    }

    // Xem công của người khác chỉ khi có quyền xem công của team hoặc toàn công ty; ai cũng xem được công của chính mình.
    private bool CanViewOthers(string employeeCode) =>
        employeeCode == _currentUserService.EmployeeCode
        || _currentUserService.IsInRole("ADMIN")
        || _currentUserService.Permissions.Contains("attendance.view.team")
        || _currentUserService.Permissions.Contains("attendance.view.all");

    [HttpGet("{employeeCode}/history")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetHistory(string employeeCode)
    {
        if (!CanViewOthers(employeeCode))
        {
            return Forbid();
        }

        try
        {
            var history = await _attendanceService.GetHistoryAsync(employeeCode);
            return Ok(history);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // Trạng thái hiệu lực từng ngày (tách sáng/chiều) trong khoảng, gồm cả cuối tuần và ngày chưa có dữ liệu. Dùng cho lịch.
    [HttpGet("{employeeCode}/calendar")]
    public async Task<ActionResult<IEnumerable<AttendanceCalendarDayDto>>> GetCalendar(
        string employeeCode, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        if (!CanViewOthers(employeeCode))
        {
            return Forbid();
        }

        try
        {
            return Ok(await _attendanceService.GetCalendarAsync(employeeCode, from, to));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Chấm công toàn công ty (tuỳ chọn lọc theo phòng ban) cho người có quyền xem tất cả.
    [HttpGet("all")]
    [RequirePermission("attendance.view.all")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetAll(
        [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate, [FromQuery] string? departmentCode)
    {
        try
        {
            return Ok(await _attendanceService.GetAllAsync(startDate, endDate, departmentCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Chấm công của cấp dưới (đệ quy) của một quản lý. Chỉ xem được team của chính mình; Admin xem được mọi quản lý.
    [HttpGet("team/{managerEmployeeCode}")]
    [RequirePermission("attendance.view.team")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetByTeam(
        string managerEmployeeCode, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        if (!_currentUserService.IsInRole("ADMIN") && managerEmployeeCode != _currentUserService.EmployeeCode)
        {
            return Forbid();
        }

        try
        {
            return Ok(await _attendanceService.GetByTeamAsync(managerEmployeeCode, startDate, endDate));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("department/{departmentCode}")]
    [RequirePermission("attendance.view.team")]
    public async Task<ActionResult<IEnumerable<AttendanceRecordDto>>> GetByDepartment(
        string departmentCode, [FromQuery] DateOnly startDate, [FromQuery] DateOnly endDate)
    {
        try
        {
            var records = await _attendanceService.GetByDepartmentAsync(departmentCode, startDate, endDate);
            return Ok(records);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("adjustments")]
    [RequirePermission("attendance.adjustment.self")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> SubmitAdjustment(SubmitAdjustmentRequest request)
    {
        try
        {
            var result = await _attendanceAdjustmentService.SubmitAsync(request, _currentUserService.EmployeeCode);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("adjustments/mine")]
    [RequirePermission("attendance.adjustment.self")]
    public async Task<ActionResult<IEnumerable<AttendanceAdjustmentDto>>> GetMyAdjustments()
    {
        var adjustments = await _attendanceAdjustmentService.GetByEmployeeAsync(_currentUserService.EmployeeCode);
        return Ok(adjustments);
    }

    [HttpGet("adjustments/pending")]
    [RequirePermission("attendance.adjustment.view")]
    public async Task<ActionResult<IEnumerable<AttendanceAdjustmentDto>>> GetPendingAdjustments()
    {
        var pending = await _attendanceAdjustmentService.GetPendingAsync(_currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN"));
        return Ok(pending);
    }

    [HttpPut("adjustments/{id}/approve")]
    [RequirePermission("attendance.adjustment.approve")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> ApproveAdjustment(long id)
    {
        try
        {
            var result = await _attendanceAdjustmentService.ApproveAsync(id, _currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN"));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("adjustments/{id}/reject")]
    [RequirePermission("attendance.adjustment.approve")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> RejectAdjustment(long id)
    {
        try
        {
            var result = await _attendanceAdjustmentService.RejectAsync(id, _currentUserService.EmployeeCode, _currentUserService.IsInRole("ADMIN"));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Bước 2 (sau khi Manager đã duyệt): người có quyền xử lý áp dụng đúng giờ đề xuất vào dữ liệu chấm công hoặc từ chối.
    [HttpGet("adjustments/pending-processing")]
    [RequirePermission("attendance.adjustment.apply")]
    public async Task<ActionResult<IEnumerable<AttendanceAdjustmentDto>>> GetPendingProcessing()
    {
        return Ok(await _attendanceAdjustmentService.GetPendingProcessingAsync());
    }

    [HttpPut("adjustments/{id}/apply")]
    [RequirePermission("attendance.adjustment.apply")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> ApplyAdjustment(long id)
    {
        try
        {
            return Ok(await _attendanceAdjustmentService.ApplyAsync(id, _currentUserService.EmployeeCode));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("adjustments/{id}/reject-processing")]
    [RequirePermission("attendance.adjustment.apply")]
    public async Task<ActionResult<AttendanceAdjustmentDto>> RejectAdjustmentProcessing(long id, RejectLeaveRequest request)
    {
        try
        {
            return Ok(await _attendanceAdjustmentService.RejectProcessingAsync(id, _currentUserService.EmployeeCode, request.RejectionReason));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
