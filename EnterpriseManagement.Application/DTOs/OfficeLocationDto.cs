namespace EnterpriseManagement.Application.DTOs;

public class OfficeLocationDto
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double AllowedRadiusMeters { get; set; }
}
