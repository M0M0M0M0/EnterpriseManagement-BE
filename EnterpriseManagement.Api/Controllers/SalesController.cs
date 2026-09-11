using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleService _saleService;
    private readonly ICurrentUserService _currentUserService;

    public SalesController(ISaleService saleService, ICurrentUserService currentUserService)
    {
        _saleService = saleService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [RequirePermission("sales.self")]
    public async Task<ActionResult<SaleDto>> Submit(SubmitSaleRequest request)
    {
        try
        {
            var result = await _saleService.SubmitAsync(request, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [RequirePermission("sales.self")]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetMine()
    {
        var sales = await _saleService.GetByEmployeeAsync(_currentUserService.EmployeeCode!);
        return Ok(sales);
    }

    [HttpPut("{id}")]
    [RequirePermission("sales.self")]
    public async Task<ActionResult<SaleDto>> Update(long id, UpdateSaleRequest request)
    {
        try
        {
            var result = await _saleService.UpdateAsync(id, _currentUserService.EmployeeCode!, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending")]
    [RequirePermission("sales.view")]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetPending()
    {
        var pending = await _saleService.GetPendingAsync();
        return Ok(pending);
    }

    [HttpGet("history")]
    [RequirePermission("sales.view")]
    public async Task<ActionResult<IEnumerable<SaleDto>>> GetHistory()
    {
        var history = await _saleService.GetHistoryAsync();
        return Ok(history);
    }

    [HttpPut("{id}/approve")]
    [RequirePermission("sales.approve")]
    public async Task<ActionResult<SaleDto>> Approve(long id)
    {
        try
        {
            var result = await _saleService.ApproveAsync(id, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}/reject")]
    [RequirePermission("sales.approve")]
    public async Task<ActionResult<SaleDto>> Reject(long id)
    {
        try
        {
            var result = await _saleService.RejectAsync(id, _currentUserService.EmployeeCode!);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
