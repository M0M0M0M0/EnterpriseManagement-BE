namespace EnterpriseManagement.Application.DTOs;

public class HolidayDto
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CreateHolidayRequest
{
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
}
