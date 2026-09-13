using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/kpi-plans")]
[Authorize]
[RequirePermission("sales.kpi.manage")]
public class KpiPlansController : ControllerBase
{
    private readonly IKpiPlanService _kpiPlanService;
    private readonly ICurrentUserService _currentUserService;

    public KpiPlansController(IKpiPlanService kpiPlanService, ICurrentUserService currentUserService)
    {
        _kpiPlanService = kpiPlanService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<KpiPlanDto>>> GetAll()
    {
        var plans = await _kpiPlanService.GetAllAsync();
        return Ok(plans);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<KpiPlanDto>> GetById(long id)
    {
        try
        {
            var plan = await _kpiPlanService.GetByIdAsync(id);
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<KpiPlanDto>> Create(CreateKpiPlanRequest request)
    {
        try
        {
            var created = await _kpiPlanService.CreateAsync(request);
            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<KpiPlanDto>> Update(long id, UpdateKpiPlanRequest request)
    {
        try
        {
            var updated = await _kpiPlanService.UpdateAsync(id, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/active")]
    public async Task<ActionResult<KpiPlanDto>> SetActive(long id, SetActiveRequest request)
    {
        try
        {
            var updated = await _kpiPlanService.SetActiveAsync(id, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/employees")]
    public async Task<ActionResult<KpiPlanDto>> AssignEmployees(long id, AssignEmployeesRequest request)
    {
        try
        {
            var updated = await _kpiPlanService.AssignEmployeesAsync(
                id, request.EmployeeCodes, _currentUserService.EmployeeCode!, _currentUserService.IsInRole("ADMIN"));
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
