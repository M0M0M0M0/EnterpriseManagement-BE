using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [RequirePermission("audit.view")]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAll(
        [FromQuery] string? username, [FromQuery] string? module, [FromQuery] string? action, [FromQuery] DateOnly? date)
    {
        var logs = await _auditLogService.GetFilteredAsync(username, module, action, date);
        return Ok(logs);
    }
}
