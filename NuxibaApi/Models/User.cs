namespace NuxibaApi.Models;

// Usuario del call center (tabla ccUsers). Solo se mapean las columnas que
// usan los ejercicios (validación de logins y reporte CSV); la tabla real
// trae además Password, TipoUser_id, Status, fCreate y LastLoginAttempt.
public class User
{
    public int User_id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string? Nombres { get; set; }

    public string? ApellidoPaterno { get; set; }

    public string? ApellidoMaterno { get; set; }

    // Referencia al área (ccRIACat_Areas.IDArea).
    public int IDArea { get; set; }
}
