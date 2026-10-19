using EnterpriseManagement.Application.Common;
using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Application.Interfaces;

namespace EnterpriseManagement.Application.Services;

// Đồng ý thu thập vị trí/ảnh chấm công. Phải đồng ý bản chính sách hiện hành trước khi chấm công; rút lại được bất cứ lúc nào.
public class PrivacyConsentService : IPrivacyConsentService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly int _retentionDays;

    public PrivacyConsentService(
        IUserRepository userRepository, IAuditLogService auditLogService, ICurrentUserService currentUserService, int retentionDays)
    {
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _currentUserService = currentUserService;
        _retentionDays = retentionDays;
    }

    public PrivacyPolicyDto GetPolicy() => new()
    {
        Version = PrivacyPolicy.CurrentVersion,
        RetentionDays = _retentionDays,
        Sections = PrivacyPolicy.Sections(_retentionDays).Select(s => new PrivacySectionDto { Heading = s.Heading, Body = s.Body }).ToList()
    };

    public async Task<PrivacyConsentDto> GetAsync(string employeeCode)
    {
        var user = await _userRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy tài khoản của nhân viên '{employeeCode}'.");
        return ToDto(user.PrivacyConsentVersion, user.PrivacyConsentAt);
    }

    public async Task<PrivacyConsentDto> AcceptAsync(string employeeCode)
    {
        var user = await _userRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy tài khoản của nhân viên '{employeeCode}'.");

        user.PrivacyConsentVersion = PrivacyPolicy.CurrentVersion;
        user.PrivacyConsentAt = VietnamClock.Now;
        await _userRepository.SaveChangesAsync();
        await _auditLogService.LogAsync(_currentUserService.UserId, "PrivacyConsentAccept", "User", user.Id, _currentUserService.IpAddress);
        return ToDto(user.PrivacyConsentVersion, user.PrivacyConsentAt);
    }

    public async Task<PrivacyConsentDto> WithdrawAsync(string employeeCode)
    {
        var user = await _userRepository.GetByEmployeeCodeAsync(employeeCode)
            ?? throw new InvalidOperationException($"Không tìm thấy tài khoản của nhân viên '{employeeCode}'.");

        user.PrivacyConsentVersion = null;
        user.PrivacyConsentAt = null;
        await _userRepository.SaveChangesAsync();
        await _auditLogService.LogAsync(_currentUserService.UserId, "PrivacyConsentWithdraw", "User", user.Id, _currentUserService.IpAddress);
        return ToDto(null, null);
    }

    public async Task EnsureAcceptedAsync(string employeeCode)
    {
        var consent = await GetAsync(employeeCode);
        if (!consent.Accepted)
        {
            throw new InvalidOperationException(
                "Bạn cần đọc và đồng ý chính sách dữ liệu vị trí và ảnh chấm công trước khi chấm công (mục Quyền riêng tư).");
        }
    }

    private static PrivacyConsentDto ToDto(int? version, DateTime? at) => new()
    {
        CurrentVersion = PrivacyPolicy.CurrentVersion,
        AcceptedVersion = version,
        AcceptedAt = at,
        Accepted = version == PrivacyPolicy.CurrentVersion
    };
}
