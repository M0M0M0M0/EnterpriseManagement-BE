namespace EnterpriseManagement.Application.DTOs;

public class SubmitSaleRequest
{
    // Chọn đúng 1 trong 2: CustomerCode (khách hàng có sẵn) HOẶC NewCustomerName+NewCustomerPhone
    // (khách hàng mới, được tạo ở trạng thái Potential và chỉ hiện trong danh sách chọn sau khi
    // sale này được duyệt — xem SaleService.SubmitAsync/ChangeStatusAsync).
    public string? CustomerCode { get; set; }
    public string? NewCustomerName { get; set; }
    public string? NewCustomerPhone { get; set; }
    public string? NewCustomerEmail { get; set; }
    public string? NewCustomerAddress { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
