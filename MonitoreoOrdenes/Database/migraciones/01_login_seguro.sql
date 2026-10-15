-- ============================================================
-- 01_login_seguro.sql
-- Prepara la base de datos para guardar contraseñas con PBKDF2 + sal.
--
-- IMPORTANTE:
--   * Ejecutar ESTE script ANTES de instalar la nueva version de la app.
--   * No modifica ni borra sp_Login: la app actual sigue funcionando hasta
--     que instales la nueva.
--   * Requiere SQL Server 2016 SP1 o superior (CREATE OR ALTER).
-- ============================================================
USE MonitoreoOrdenesCompra;
GO

-- 0) Respaldo de la tabla de usuarios (por si hay que volver atras)
IF OBJECT_ID('dbo.Usuarios_respaldo_20261005') IS NULL
    SELECT * INTO dbo.Usuarios_respaldo_20261005 FROM dbo.Usuarios;
GO

-- 1) Devuelve los datos del usuario ACTIVO y su hash almacenado.
--    La verificacion de la contraseña se hace en la aplicacion.
CREATE OR ALTER PROCEDURE dbo.sp_ObtenerCredenciales
    @username VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.usuario_id,
        u.nombre,
        u.apellido,
        u.email,
        u.username,
        u.rol_id,
        r.nombre AS rol_nombre,
        u.activo,
        u.password_hash
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON u.rol_id = r.rol_id
    WHERE u.username = @username
      AND u.activo   = 1;
END
GO

-- 2) Guarda el nuevo hash (la app lo llama tras un login correcto
--    cuando el hash aun esta en formato antiguo).
CREATE OR ALTER PROCEDURE dbo.sp_ActualizarPasswordHash
    @usuario_id    INT,
    @password_hash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET    password_hash = @password_hash
    WHERE  usuario_id    = @usuario_id;
END
GO

-- 3) Registra el ultimo acceso.
CREATE OR ALTER PROCEDURE dbo.sp_RegistrarAcceso
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET    ultimo_acceso = GETDATE()
    WHERE  usuario_id    = @usuario_id;
END
GO

-- 4) SOLO si la app se conecta con un usuario SQL con permisos limitados
--    (si no hay error de permisos al probar, ignora este bloque).
--    Cambia TU_USUARIO_SQL por el usuario real y quita los "--".
-- GRANT EXECUTE ON dbo.sp_ObtenerCredenciales    TO TU_USUARIO_SQL;
-- GRANT EXECUTE ON dbo.sp_ActualizarPasswordHash TO TU_USUARIO_SQL;
-- GRANT EXECUTE ON dbo.sp_RegistrarAcceso        TO TU_USUARIO_SQL;
GO

-- ============================================================
-- CONSULTAS UTILES (no forman parte de la instalacion)
-- ============================================================

-- Avance de la migracion: cada usuario se migra solo al iniciar sesion.
-- SELECT
--     SUM(CASE WHEN password_hash LIKE 'PBKDF2$%'     THEN 1 ELSE 0 END) AS migrados,
--     SUM(CASE WHEN password_hash NOT LIKE 'PBKDF2$%' THEN 1 ELSE 0 END) AS pendientes
-- FROM dbo.Usuarios;

-- Restablecer la contraseña de un usuario (se migra sola en su siguiente login).
-- Solo para contraseñas con caracteres ASCII (sin acentos ni ñ).
-- UPDATE dbo.Usuarios
-- SET    password_hash = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'NuevaClave123'), 2))
-- WHERE  username = 'usuario';

-- Cuando TODOS los usuarios activos esten migrados, elimina el login antiguo
-- (acepta el hash como si fuera la contraseña, y ya no se usa):
-- DROP PROCEDURE dbo.sp_Login;
