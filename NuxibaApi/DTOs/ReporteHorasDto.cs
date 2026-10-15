namespace NuxibaApi.DTOs;

// Una fila del reporte de horas trabajadas (ejercicio 3).
public class ReporteHorasDto
{
    public string Usuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public double TotalHorasTrabajadas { get; set; }
}
