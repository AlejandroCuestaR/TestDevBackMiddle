using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuxibaApi.Data;
using NuxibaApi.DTOs;
using NuxibaApi.Models;

namespace NuxibaApi.Controllers;

[ApiController]
[Route("logins")]
public class LoginsController : ControllerBase
{
    // Rango válido para la columna datetime de SQL Server.
    private static readonly DateTime SqlDateTimeMin = new(1753, 1, 1);
    private static readonly DateTime SqlDateTimeMax = new(9999, 12, 31, 23, 59, 59);

    private readonly AppDbContext _db;

    public LoginsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /logins -> todos los registros de login/logout.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<LoginDto>>> GetAll()
    {
        var logins = await _db.Logins
            .AsNoTracking()
            .OrderBy(l => l.User_id)
            .ThenBy(l => l.fecha)
            .Select(l => new LoginDto
            {
                Id = l.Id,
                User_id = l.User_id,
                Extension = l.Extension,
                TipoMov = l.TipoMov,
                fecha = l.fecha
            })
            .ToListAsync();

        return Ok(logins);
    }

    // GET /logins/{id} -> un registro por Id.
    [HttpGet("{id:int}", Name = "GetLoginById")]
    public async Task<ActionResult<LoginDto>> GetById(int id)
    {
        var login = await _db.Logins.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        if (login is null)
            return NotFound($"No existe el registro con Id {id}.");

        return Ok(ToDto(login));
    }

    // POST /logins -> registra un nuevo login/logout.
    [HttpPost]
    public async Task<ActionResult<LoginDto>> Create(LoginCreateDto dto)
    {
        var error = await ValidarMovimiento(dto.User_id, dto.TipoMov, dto.fecha, validarSecuencia: true);
        if (error is not null)
            return BadRequest(error);

        var login = new Login
        {
            User_id = dto.User_id,
            Extension = dto.Extension,
            TipoMov = dto.TipoMov,
            fecha = dto.fecha
        };

        _db.Logins.Add(login);
        await _db.SaveChangesAsync();

        return CreatedAtRoute("GetLoginById", new { id = login.Id }, ToDto(login));
    }

    // PUT /logins/{id} -> actualiza un registro existente.
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LoginUpdateDto dto)
    {
        var login = await _db.Logins.FirstOrDefaultAsync(l => l.Id == id);
        if (login is null)
            return NotFound($"No existe el registro con Id {id}.");

        // En la actualización se validan fecha, usuario y TipoMov, pero no la
        // secuencia login/logout: reordenar un registro histórico no debería
        // bloquearse por el estado actual del usuario.
        var error = await ValidarMovimiento(dto.User_id, dto.TipoMov, dto.fecha, validarSecuencia: false);
        if (error is not null)
            return BadRequest(error);

        login.User_id = dto.User_id;
        login.Extension = dto.Extension;
        login.TipoMov = dto.TipoMov;
        login.fecha = dto.fecha;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // DELETE /logins/{id} -> elimina un registro.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var login = await _db.Logins.FirstOrDefaultAsync(l => l.Id == id);
        if (login is null)
            return NotFound($"No existe el registro con Id {id}.");

        _db.Logins.Remove(login);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // GET /logins/reporte/csv -> descarga el reporte de horas trabajadas (ejercicio 3).
    [HttpGet("reporte/csv")]
    public async Task<IActionResult> ReporteCsv()
    {
        // Segundos trabajados por usuario: se empareja cada login con el logout
        // que le sigue inmediatamente (misma lógica que el LEAD del ejercicio 2).
        var movimientos = await _db.Logins
            .AsNoTracking()
            .OrderBy(l => l.User_id).ThenBy(l => l.fecha).ThenBy(l => l.Id)
            .Select(l => new { l.User_id, l.TipoMov, l.fecha })
            .ToListAsync();

        var segundosPorUsuario = new Dictionary<int, long>();
        for (int i = 0; i < movimientos.Count - 1; i++)
        {
            var actual = movimientos[i];
            var siguiente = movimientos[i + 1];

            if (actual.User_id == siguiente.User_id && actual.TipoMov == 1 && siguiente.TipoMov == 0)
            {
                var segundos = (long)(siguiente.fecha - actual.fecha).TotalSeconds;
                segundosPorUsuario[actual.User_id] =
                    segundosPorUsuario.GetValueOrDefault(actual.User_id) + segundos;
            }
        }

        // Usuario + área (left join para no perder usuarios sin área asignada).
        var usuarios = await (
            from u in _db.Users.AsNoTracking()
            join a in _db.Areas.AsNoTracking() on u.IDArea equals a.IDArea into ga
            from a in ga.DefaultIfEmpty()
            orderby u.User_id
            select new
            {
                u.User_id,
                u.Login,
                u.Nombres,
                u.ApellidoPaterno,
                u.ApellidoMaterno,
                Area = a != null ? a.AreaName : string.Empty
            }).ToListAsync();

        var filas = usuarios.Select(u => new ReporteHorasDto
        {
            Usuario = u.Login,
            NombreCompleto = string.Join(" ", new[] { u.Nombres, u.ApellidoPaterno, u.ApellidoMaterno }
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.Trim())),
            Area = u.Area,
            TotalHorasTrabajadas = Math.Round(segundosPorUsuario.GetValueOrDefault(u.User_id) / 3600.0, 2)
        });

        var csv = ConstruirCsv(filas);

        // BOM UTF-8 para que Excel respete los acentos al abrir el archivo.
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", "reporte_horas.csv");
    }

    // Validaciones comunes de un movimiento. Devuelve el mensaje de error o null si todo está bien.
    private async Task<string?> ValidarMovimiento(int userId, int tipoMov, DateTime fecha, bool validarSecuencia)
    {
        // Fecha dentro del rango válido de SQL Server y sin fechas futuras.
        if (fecha < SqlDateTimeMin || fecha > SqlDateTimeMax)
            return "La fecha está fuera del rango permitido por SQL Server.";

        if (fecha > DateTime.Now.AddMinutes(5))
            return "La fecha no puede estar en el futuro.";

        // El usuario debe existir en ccUsers.
        var existeUsuario = await _db.Users.AnyAsync(u => u.User_id == userId);
        if (!existeUsuario)
            return $"El usuario {userId} no existe en ccUsers.";

        // No permitir dos logins seguidos sin un logout de por medio.
        if (validarSecuencia && tipoMov == 1)
        {
            var ultimo = await _db.Logins
                .Where(l => l.User_id == userId)
                .OrderByDescending(l => l.fecha)
                .ThenByDescending(l => l.Id)
                .FirstOrDefaultAsync();

            if (ultimo is not null && ultimo.TipoMov == 1)
                return "El usuario ya tiene un login abierto; registre primero el logout.";
        }

        return null;
    }

    private static LoginDto ToDto(Login l) => new()
    {
        Id = l.Id,
        User_id = l.User_id,
        Extension = l.Extension,
        TipoMov = l.TipoMov,
        fecha = l.fecha
    };

    private static string ConstruirCsv(IEnumerable<ReporteHorasDto> filas)
    {
        var sb = new StringBuilder();
        sb.Append("Usuario,NombreCompleto,Area,Total horas trabajadas\r\n");

        foreach (var f in filas)
        {
            sb.Append(Escapar(f.Usuario)).Append(',')
              .Append(Escapar(f.NombreCompleto)).Append(',')
              .Append(Escapar(f.Area)).Append(',')
              .Append(f.TotalHorasTrabajadas.ToString("0.00", CultureInfo.InvariantCulture))
              .Append("\r\n");
        }

        return sb.ToString();
    }

    // Escapa un campo según la convención CSV (comillas si contiene coma, comilla o salto de línea).
    private static string Escapar(string? valor)
    {
        valor ??= string.Empty;
        if (valor.Contains(',') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            return "\"" + valor.Replace("\"", "\"\"") + "\"";
        return valor;
    }
}
