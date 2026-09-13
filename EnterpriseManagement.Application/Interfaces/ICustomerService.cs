using EnterpriseManagement.Application.DTOs;
using EnterpriseManagement.Domain.Entities.Sales;

namespace EnterpriseManagement.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync();
    Task<CustomerDto?> GetByCodeAsync(string customerCode);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, string employeeCode);

    // Dùng riêng cho luồng "khách hàng mới nhập kèm lúc submit sale": nếu SĐT đã tồn tại thì
    // dùng lại đúng khách hàng đó (bất kể trạng thái) thay vì báo lỗi trùng — đây là cơ chế
    // dedupe ngầm, khác với CreateAsync (hành động tạo mới có chủ đích, báo lỗi khi trùng).
    // Khách hàng tạo qua đường này ở trạng thái Potential cho đến khi sale được duyệt.
    Task<Customer> FindOrCreatePotentialAsync(string name, string phone, string? email, string? address, string employeeCode);
}
