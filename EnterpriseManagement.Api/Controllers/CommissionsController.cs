using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/commissions")]
[Authorize]
public class CommissionsController : ControllerBase
{
    private readonly ISalesCommissionService _commissionService;
    private readonly ICurrentUserService _currentUserService;

    public CommissionsController(ISalesCommissionService commissionService, ICurrentUserService currentUserService)
    {
        _commissionService = commissionService;
        _currentUserService = currentUserService;
    }

    [HttpPost("calculate")]
    [RequirePermission("sales.kpi.manage")]
    public async Task<ActionResult<CommissionDto>> Calculate(CalculateCommissionRequest request)
    {
        try
        {
            var result = await _commissionService.CalculateAsync(request, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [RequirePermission("sales.self")]
    public async Task<ActionResult<IEnumerable<CommissionDto>>> GetMine()
    {
        var commissions = await _commissionService.GetMineAsync(_currentUserService.EmployeeCode!);
        return Ok(commissions);
    }

    [HttpGet("pending")]
    [RequirePermission("sales.kpi.manage")]
    public async Task<ActionResult<IEnumerable<CommissionDto>>> GetPending()
    {
        var pending = await _commissionService.GetPendingAsync();
        return Ok(pending);
    }

    [HttpGet("history")]
    [RequirePermission("sales.kpi.manage")]
    public async Task<ActionResult<IEnumerable<CommissionDto>>> GetHistory()
    {
        var history = await _commissionService.GetHistoryAsync();
        return Ok(history);
    }

    [HttpPut("{id}/approve")]
    [RequirePermission("sales.kpi.manage")]
    public async Task<ActionResult<CommissionDto>> Approve(long id)
    {
        try
        {
            var result = await _commissionService.ApproveAsync(id, _currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN"));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
