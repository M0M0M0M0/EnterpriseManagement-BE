using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Controllers;

// Chính sách dữ liệu chấm công (vị trí GPS, ảnh) và sự đồng ý của từng người. Ai đăng nhập cũng dùng được cho chính mình.
[ApiController]
[Route("api/privacy")]
[Authorize]
public class PrivacyController : ControllerBase
{
    private readonly IPrivacyConsentService _service;
    private readonly ICurrentUserService _currentUserService;

    public PrivacyController(IPrivacyConsentService service, ICurrentUserService currentUserService)
    {
        _service = service;
        _currentUserService = currentUserService;
    }

    [HttpGet("policy")]
    public ActionResult<PrivacyPolicyDto> GetPolicy() => Ok(_service.GetPolicy());

    [HttpGet("consent")]
    public async Task<ActionResult<PrivacyConsentDto>> GetConsent() =>
        Ok(await _service.GetAsync(_currentUserService.EmployeeCode));

    [HttpPost("consent")]
    public async Task<ActionResult<PrivacyConsentDto>> Accept() =>
        Ok(await _service.AcceptAsync(_currentUserService.EmployeeCode));

    [HttpDelete("consent")]
    public async Task<ActionResult<PrivacyConsentDto>> Withdraw() =>
        Ok(await _service.WithdrawAsync(_currentUserService.EmployeeCode));
}
