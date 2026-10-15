namespace NuxibaApi.DTOs;

// Representación que devuelve la API para un registro de login/logout.
public class LoginDto
{
    public int Id { get; set; }
    public int User_id { get; set; }
    public int Extension { get; set; }
    public int TipoMov { get; set; }
    public DateTime fecha { get; set; }
}
