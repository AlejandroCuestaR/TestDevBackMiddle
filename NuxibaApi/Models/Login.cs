namespace NuxibaApi.Models;

// Registro de entrada/salida de un usuario (tabla ccloglogin).
// El Id es la llave primaria que agrega EF Core; en el archivo de ejemplo
// la tabla original no la trae, pero la necesitamos para el CRUD.
public class Login
{
    public int Id { get; set; }

    public int User_id { get; set; }

    // La extensión puede venir en negativo en los datos de ejemplo, así que no se restringe el signo.
    public int Extension { get; set; }

    // 1 = login, 0 = logout
    public int TipoMov { get; set; }

    public DateTime fecha { get; set; }
}
