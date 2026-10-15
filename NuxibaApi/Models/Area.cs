namespace NuxibaApi.Models;

// Catálogo de áreas (tabla ccRIACat_Areas). Se mapean solo el Id y el nombre,
// que son los datos que necesita el reporte; la tabla real trae también
// StatusArea y CreateDate.
public class Area
{
    public int IDArea { get; set; }

    public string AreaName { get; set; } = string.Empty;
}
