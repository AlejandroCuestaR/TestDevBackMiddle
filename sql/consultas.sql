/* =====================================================================
   EJERCICIO 2 - Consultas sobre ccloglogin
   SQL Server (contenedor Docker del README)

   Idea general:
   Cada "login" (TipoMov = 1) se empareja con el siguiente movimiento del
   mismo usuario usando LEAD() OVER (PARTITION BY User_id ORDER BY fecha).
   Si ese movimiento siguiente es un "logout" (TipoMov = 0), la diferencia
   en segundos (DATEDIFF) es la duración de la sesión.

   DATEDIFF(SECOND, ...) devuelve int y con miles de sesiones la suma se
   desborda, por eso se castea a BIGINT antes de sumar.
   ===================================================================== */


/* ---------------------------------------------------------------------
   2.1  Usuario que MÁS tiempo ha estado logueado (10 pts)
   --------------------------------------------------------------------- */
;WITH Emparejados AS (
    SELECT
        User_id,
        TipoMov,
        fecha,
        LEAD(fecha)   OVER (PARTITION BY User_id ORDER BY fecha, Id) AS fechaSiguiente,
        LEAD(TipoMov) OVER (PARTITION BY User_id ORDER BY fecha, Id) AS tipoSiguiente
    FROM ccloglogin
),
Sesiones AS (
    -- Solo cuentan los login seguidos inmediatamente de un logout.
    SELECT
        User_id,
        fecha,
        CAST(DATEDIFF(SECOND, fecha, fechaSiguiente) AS BIGINT) AS segundos
    FROM Emparejados
    WHERE TipoMov = 1 AND tipoSiguiente = 0
),
Totales AS (
    SELECT User_id, SUM(segundos) AS totalSegundos
    FROM Sesiones
    GROUP BY User_id
)
SELECT TOP (1) WITH TIES
    User_id,
    totalSegundos,
    CONCAT(
        totalSegundos / 86400,          ' días, ',
        (totalSegundos % 86400) / 3600, ' horas, ',
        (totalSegundos % 3600)  / 60,   ' minutos, ',
        totalSegundos % 60,             ' segundos'
    ) AS tiempoTotal
FROM Totales
ORDER BY totalSegundos DESC;
GO


/* ---------------------------------------------------------------------
   2.2  Usuario que MENOS tiempo ha estado logueado (10 pts)
   Misma consulta, cambiando el ORDER BY a ascendente.
   Supuesto: se consideran solo usuarios con al menos una sesión login->logout
   emparejada; quien nunca cerró sesión no entra al cálculo.
   --------------------------------------------------------------------- */
;WITH Emparejados AS (
    SELECT
        User_id,
        TipoMov,
        fecha,
        LEAD(fecha)   OVER (PARTITION BY User_id ORDER BY fecha, Id) AS fechaSiguiente,
        LEAD(TipoMov) OVER (PARTITION BY User_id ORDER BY fecha, Id) AS tipoSiguiente
    FROM ccloglogin
),
Sesiones AS (
    SELECT
        User_id,
        CAST(DATEDIFF(SECOND, fecha, fechaSiguiente) AS BIGINT) AS segundos
    FROM Emparejados
    WHERE TipoMov = 1 AND tipoSiguiente = 0
),
Totales AS (
    SELECT User_id, SUM(segundos) AS totalSegundos
    FROM Sesiones
    GROUP BY User_id
)
SELECT TOP (1) WITH TIES
    User_id,
    totalSegundos,
    CONCAT(
        totalSegundos / 86400,          ' días, ',
        (totalSegundos % 86400) / 3600, ' horas, ',
        (totalSegundos % 3600)  / 60,   ' minutos, ',
        totalSegundos % 60,             ' segundos'
    ) AS tiempoTotal
FROM Totales
ORDER BY totalSegundos ASC;
GO


/* ---------------------------------------------------------------------
   2.3  Promedio de logueo por usuario y por mes (10 pts)

   SUPUESTO / INTERPRETACIÓN:
   "promedio de logueo por mes" se entiende como la DURACIÓN PROMEDIO de
   una sesión (AVG de la duración de cada login→logout) agrupada por usuario
   y mes, y NO como el tiempo total del mes. El mes se toma de la fecha del
   login (inicio de la sesión).
   Si se quisiera el TOTAL por mes, bastaría con cambiar AVG(segundos) por
   SUM(segundos) en la CTE "Promedios".
   --------------------------------------------------------------------- */
;WITH Emparejados AS (
    SELECT
        User_id,
        TipoMov,
        fecha,
        LEAD(fecha)   OVER (PARTITION BY User_id ORDER BY fecha, Id) AS fechaSiguiente,
        LEAD(TipoMov) OVER (PARTITION BY User_id ORDER BY fecha, Id) AS tipoSiguiente
    FROM ccloglogin
),
Sesiones AS (
    SELECT
        User_id,
        fecha,
        CAST(DATEDIFF(SECOND, fecha, fechaSiguiente) AS BIGINT) AS segundos
    FROM Emparejados
    WHERE TipoMov = 1 AND tipoSiguiente = 0
),
Promedios AS (
    SELECT
        User_id,
        YEAR(fecha)  AS anio,
        MONTH(fecha) AS mes,
        CAST(AVG(segundos) AS BIGINT) AS promedioSegundos
    FROM Sesiones
    GROUP BY User_id, YEAR(fecha), MONTH(fecha)
)
SELECT
    User_id,
    anio,
    mes,
    promedioSegundos,
    CONCAT(
        promedioSegundos / 86400,          ' días, ',
        (promedioSegundos % 86400) / 3600, ' horas, ',
        (promedioSegundos % 3600)  / 60,   ' minutos, ',
        promedioSegundos % 60,             ' segundos'
    ) AS promedioFormateado
FROM Promedios
ORDER BY User_id, anio, mes;
GO
