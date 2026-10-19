using EnterpriseManagement.Application.DTOs;

namespace EnterpriseManagement.Application.Interfaces;

public interface IPrivacyConsentService
{
    PrivacyPolicyDto GetPolicy();
    Task<PrivacyConsentDto> GetAsync(string employeeCode);
    Task<PrivacyConsentDto> AcceptAsync(string employeeCode);
    Task<PrivacyConsentDto> WithdrawAsync(string employeeCode);

    // Ném InvalidOperationException nếu chưa đồng ý chính sách hiện hành.
    Task EnsureAcceptedAsync(string employeeCode);
}
