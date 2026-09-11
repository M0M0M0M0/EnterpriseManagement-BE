using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
    }

    [HttpGet("employee/{employeeCode}")]
    public async Task<ActionResult<EmployeeDashboardDto>> GetEmployeeDashboard(string employeeCode)
    {
        if (_currentUserService.IsInRole("EMPLOYEE") && employeeCode != _currentUserService.EmployeeCode)
        {
            return Forbid();
        }

        try
        {
            var dashboard = await _dashboardService.GetEmployeeDashboardAsync(employeeCode);
            return Ok(dashboard);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("manager/{managerEmployeeCode}")]
    [RequirePermission("dashboard.view.team")]
    public async Task<ActionResult<ManagerDashboardDto>> GetManagerDashboard(string managerEmployeeCode)
    {
        if (_currentUserService.IsInRole("MANAGER") && managerEmployeeCode != _currentUserService.EmployeeCode)
        {
            return Forbid();
        }

        try
        {
            var dashboard = await _dashboardService.GetManagerDashboardAsync(managerEmployeeCode);
            return Ok(dashboard);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
