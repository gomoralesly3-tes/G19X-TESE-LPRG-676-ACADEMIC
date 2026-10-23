-- ============================================================
-- 09_incidencias.sql  (migracion para la base de datos existente)
--
-- Consultas de la pestaña "Incidencias" del detalle de una orden:
--   * sp_ObtenerIncidencias  : lista las incidencias de una orden
--   * sp_RegistrarIncidencia : alta (Administrador, Comprador, Almacen, Supervisor)
--   * sp_ResolverIncidencia  : cierre con nota (Administrador, Comprador, Supervisor)
-- El Comprador (rol 2) solo trabaja con las incidencias de sus propias ordenes.
--
-- Ejecutar ANTES de instalar los archivos nuevos de la aplicacion.
-- Requiere SQL Server 2016 SP1 o superior. Se puede repetir sin problema.
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

-- ── Incidencias de una orden ────────────────────────────────────────────
-- Registrar: Administrador, Comprador (solo sus ordenes), Almacen y Supervisor.
-- Resolver : Administrador, Comprador (solo sus ordenes) y Supervisor.
-- No se registran incidencias en ordenes Canceladas (8) ni Cerradas (10).
-- Gravedad (TiposIncidencia): 1 Alta, 2 Media, 3 Baja.

CREATE OR ALTER PROCEDURE dbo.sp_ObtenerIncidencias
    @orden_id   INT,
    @usuario_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- El Comprador (rol 2) solo consulta las incidencias de sus propias ordenes
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
       AND NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompra
                       WHERE orden_id = @orden_id AND usuario_creador_id = @usuario_id)
        THROW 50040, N'Solo puede ver las incidencias de las órdenes que usted creó.', 1;

    SELECT i.incidencia_id,
           i.fecha_reporte,
           t.nombre   AS tipo,
           t.gravedad,
           i.descripcion,
           u.nombre + N' ' + u.apellido AS reportada_por,
           i.resuelta,
           i.fecha_resolucion,
           i.resolucion_nota
    FROM dbo.Incidencias i
    INNER JOIN dbo.TiposIncidencia t ON t.tipo_id    = i.tipo_id
    INNER JOIN dbo.Usuarios        u ON u.usuario_id = i.usuario_id
    WHERE i.orden_id = @orden_id
    ORDER BY i.resuelta, i.fecha_reporte DESC;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_RegistrarIncidencia
    @usuario_id  INT,
    @orden_id    INT,
    @tipo_id     INT,
    @descripcion VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @rol_id INT;
    SELECT @rol_id = rol_id FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND activo = 1;

    IF @rol_id IS NULL OR @rol_id NOT IN (1, 2, 3, 4)
        THROW 50041, N'No tiene permisos para registrar incidencias.', 1;

    DECLARE @estatus INT, @creador INT;
    SELECT @estatus = estatus_id, @creador = usuario_creador_id
    FROM   dbo.OrdenesCompra
    WHERE  orden_id = @orden_id;

    IF @estatus IS NULL
        THROW 50042, N'Orden de compra no encontrada.', 1;

    IF @rol_id = 2 AND ISNULL(@creador, 0) <> @usuario_id
        THROW 50043, N'Solo puede registrar incidencias en las órdenes que usted creó.', 1;

    IF @estatus IN (8, 10)
        THROW 50044, N'No se pueden registrar incidencias en una orden cancelada o cerrada.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.TiposIncidencia WHERE tipo_id = @tipo_id)
        THROW 50045, N'El tipo de incidencia no existe.', 1;

    SET @descripcion = LTRIM(RTRIM(ISNULL(@descripcion, '')));
    IF @descripcion = ''
        THROW 50046, N'La descripción es obligatoria.', 1;

    INSERT INTO dbo.Incidencias (orden_id, tipo_id, usuario_id, descripcion)
    VALUES (@orden_id, @tipo_id, @usuario_id, @descripcion);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS incidencia_id;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_ResolverIncidencia
    @usuario_id    INT,
    @incidencia_id INT,
    @resolucion    VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @rol_id INT;
    SELECT @rol_id = rol_id FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND activo = 1;

    IF @rol_id IS NULL OR @rol_id NOT IN (1, 2, 4)
        THROW 50047, N'No tiene permisos para resolver incidencias.', 1;

    DECLARE @orden INT, @resuelta BIT, @creador INT;
    SELECT @orden = i.orden_id, @resuelta = i.resuelta, @creador = o.usuario_creador_id
    FROM   dbo.Incidencias i
    INNER JOIN dbo.OrdenesCompra o ON o.orden_id = i.orden_id
    WHERE  i.incidencia_id = @incidencia_id;

    IF @orden IS NULL
        THROW 50048, N'La incidencia no existe.', 1;

    IF @rol_id = 2 AND ISNULL(@creador, 0) <> @usuario_id
        THROW 50050, N'Solo puede resolver incidencias de las órdenes que usted creó.', 1;

    IF @resuelta = 1
        THROW 50049, N'La incidencia ya estaba resuelta.', 1;

    SET @resolucion = LTRIM(RTRIM(ISNULL(@resolucion, '')));
    IF @resolucion = ''
        THROW 50051, N'La resolución es obligatoria.', 1;

    UPDATE dbo.Incidencias
    SET    resuelta         = 1,
           fecha_resolucion = GETDATE(),
           resolucion_nota  = @resolucion
    WHERE  incidencia_id = @incidencia_id
      AND  resuelta = 0;
END
GO
