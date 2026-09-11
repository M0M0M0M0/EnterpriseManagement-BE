using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly ILeaveBalanceService _leaveBalanceService;
    private readonly ICurrentUserService _currentUserService;

    public LeaveBalancesController(ILeaveBalanceService leaveBalanceService, ICurrentUserService currentUserService)
    {
        _leaveBalanceService = leaveBalanceService;
        _currentUserService = currentUserService;
    }

    [HttpGet("{employeeCode}")]
    public async Task<ActionResult<IEnumerable<LeaveBalanceDto>>> GetByEmployee(string employeeCode, [FromQuery] int year)
    {
        if (_currentUserService.IsInRole("EMPLOYEE") && employeeCode != _currentUserService.EmployeeCode)
        {
            return Forbid();
        }

        try
        {
            var balances = await _leaveBalanceService.GetByEmployeeAsync(employeeCode, year);
            return Ok(balances);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut]
    [RequirePermission("leavebalance.manage")]
    public async Task<ActionResult<LeaveBalanceDto>> SetAllocatedTime(SetLeaveBalanceRequest request)
    {
        try
        {
            var result = await _leaveBalanceService.SetAllocatedTimeAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
