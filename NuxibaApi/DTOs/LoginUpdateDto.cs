using System.ComponentModel.DataAnnotations;

namespace NuxibaApi.DTOs;

// Datos para actualizar un registro existente (PUT /logins/{id}).
public class LoginUpdateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El User_id debe ser mayor a 0.")]
    public int User_id { get; set; }

    public int Extension { get; set; }

    [Range(0, 1, ErrorMessage = "TipoMov debe ser 1 (login) o 0 (logout).")]
    public int TipoMov { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateTime fecha { get; set; }
}
