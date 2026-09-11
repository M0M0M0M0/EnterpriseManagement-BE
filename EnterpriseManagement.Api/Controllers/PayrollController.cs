using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[RequirePermission("payroll.calculate")]
public class PayrollController : ControllerBase
{
    private readonly IPayrollCalculationService _payrollCalculationService;

    public PayrollController(IPayrollCalculationService payrollCalculationService)
    {
        _payrollCalculationService = payrollCalculationService;
    }

    [HttpGet("calculate")]
    public async Task<ActionResult<SalaryCalculationResult>> Calculate(
        [FromQuery] string employeeCode, [FromQuery] int year, [FromQuery] int month)
    {
        try
        {
            var result = await _payrollCalculationService.CalculateAsync(employeeCode, year, month);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
