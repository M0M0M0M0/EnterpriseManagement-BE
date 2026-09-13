using EnterpriseManagement.Domain.Entities.HR;
using EnterpriseManagement.Domain.Enums;

namespace EnterpriseManagement.Domain.Entities.Sales;

public class Sale
{
    public long Id { get; set; }
    public string SaleCode { get; set; } = string.Empty;

    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public SaleStatus Status { get; set; }
    public string? Note { get; set; }
    public string? RejectionReason { get; set; }

    public long? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
