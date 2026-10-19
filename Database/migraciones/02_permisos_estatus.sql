-- ============================================================
-- 02_permisos_estatus.sql
-- Endurece sp_CambiarEstatusOrden:
--   1) Solo pueden cambiar estatus: 1 Administrador, 2 Comprador, 4 Supervisor.
--      (3 Almacén y 5 Consulta quedan sin permiso.)
--   2) Valida que el nuevo estatus exista y sea distinto al actual.
--   3) Hace el UPDATE y el historial en una sola transaccion
--      (antes, si fallaba el historial, la orden ya habia cambiado).
--   4) Bloquea la fila para que dos usuarios no la cambien a la vez.
--
-- Ejecutar ANTES de instalar la nueva version de FrmDetalleOrden.cs.
-- Requiere SQL Server 2016 SP1 o superior.
-- Para cambiar los roles permitidos, edita la lista IN (1, 2, 4) y
-- la lista RolesQueCambianEstatus en FrmDetalleOrden.cs (deben coincidir).
-- ============================================================
USE MonitoreoOrdenesCompra;
GO

CREATE OR ALTER PROCEDURE dbo.sp_CambiarEstatusOrden
    @orden_id         INT,
    @nuevo_estatus_id INT,
    @usuario_id       INT,
    @comentario       VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- 1) Permiso por rol (el usuario debe existir y estar activo)
    DECLARE @rol_id INT;

    SELECT @rol_id = rol_id
    FROM   dbo.Usuarios
    WHERE  usuario_id = @usuario_id
      AND  activo     = 1;

    IF @rol_id IS NULL OR @rol_id NOT IN (1, 2, 4)
        THROW 50001, 'No tiene permisos para cambiar el estatus de una orden.', 1;

    -- 2) El estatus destino debe existir
    IF NOT EXISTS (SELECT 1 FROM dbo.EstatusOrden WHERE estatus_id = @nuevo_estatus_id)
        THROW 50002, 'El estatus seleccionado no existe.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @estatus_actual INT;

        -- Bloquea la orden mientras se cambia
        SELECT @estatus_actual = estatus_id
        FROM   dbo.OrdenesCompra WITH (UPDLOCK, ROWLOCK)
        WHERE  orden_id = @orden_id;

        IF @estatus_actual IS NULL
            THROW 50003, 'Orden de compra no encontrada.', 1;

        IF @estatus_actual = @nuevo_estatus_id
            THROW 50004, 'La orden ya tiene ese estatus. Actualice la pantalla.', 1;

        UPDATE dbo.OrdenesCompra
        SET    estatus_id = @nuevo_estatus_id,
               fecha_entrega_real = CASE
                   WHEN @nuevo_estatus_id = 7 THEN GETDATE()  -- Recibida Completa
                   ELSE fecha_entrega_real
               END
        WHERE  orden_id = @orden_id;

        INSERT INTO dbo.HistorialEstatusOrden
               (orden_id, estatus_ant_id, estatus_nuevo_id, usuario_id, comentario)
        VALUES (@orden_id, @estatus_actual, @nuevo_estatus_id, @usuario_id, @comentario);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;   -- vuelve a lanzar el mismo error hacia la aplicacion
    END CATCH
END
GO

-- Prueba rapida (opcional): con un usuario de rol 5 (Consulta) debe fallar con
-- "No tiene permisos...". Cambia los numeros por datos reales de tu BD.
-- EXEC dbo.sp_CambiarEstatusOrden @orden_id = 1, @nuevo_estatus_id = 2, @usuario_id = 99;
