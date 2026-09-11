using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using EnterpriseManagement.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PositionsController : ControllerBase
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService)
    {
        _positionService = positionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetAll()
    {
        var positions = await _positionService.GetAllAsync();
        return Ok(positions);
    }

    [HttpGet("{positionCode}")]
    public async Task<ActionResult<PositionDto>> GetByCode(string positionCode)
    {
        var position = await _positionService.GetByCodeAsync(positionCode);
        return position is null ? NotFound() : Ok(position);
    }

    [HttpPost]
    [RequirePermission("position.manage")]
    public async Task<ActionResult<PositionDto>> Create(CreatePositionRequest request)
    {
        try
        {
            var created = await _positionService.CreateAsync(request);
            return CreatedAtAction(nameof(GetByCode), new { positionCode = created.PositionCode }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{positionCode}")]
    [RequirePermission("position.manage")]
    public async Task<ActionResult<PositionDto>> Update(string positionCode, UpdatePositionRequest request)
    {
        try
        {
            var updated = await _positionService.UpdateAsync(positionCode, request);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{positionCode}/active")]
    [RequirePermission("position.manage")]
    public async Task<ActionResult<PositionDto>> SetActive(string positionCode, SetActiveRequest request)
    {
        try
        {
            var updated = await _positionService.SetActiveAsync(positionCode, request.IsActive);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{positionCode}/salary")]
    [RequirePermission("position.manage")]
    public async Task<ActionResult<PositionDto>> SetStandardSalary(string positionCode, SetStandardSalaryRequest request)
    {
        try
        {
            var updated = await _positionService.SetStandardSalaryAsync(positionCode, request.StandardSalary);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
