namespace EnterpriseManagement.Application.DTOs;

public class PrivacySectionDto
{
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class PrivacyPolicyDto
{
    public int Version { get; set; }
    public int RetentionDays { get; set; }
    public List<PrivacySectionDto> Sections { get; set; } = new();
}

public class PrivacyConsentDto
{
    public int CurrentVersion { get; set; }
    public int? AcceptedVersion { get; set; }
    public DateTime? AcceptedAt { get; set; }

    // Đã đồng ý đúng bản chính sách hiện hành.
    public bool Accepted { get; set; }
}
