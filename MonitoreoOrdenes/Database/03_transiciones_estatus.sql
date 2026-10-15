-- ============================================================
-- 03_transiciones_estatus.sql
-- Define que cambios de estatus son validos y los exige en la BD.
--
--   * Crea la tabla EstatusTransiciones (origen -> destino permitido).
--   * Carga el recorrido propuesto (ver abajo; es editable).
--   * Reemplaza sp_CambiarEstatusOrden: ahora rechaza cambios no permitidos.
--     (Incluye todo lo de 02_permisos_estatus.sql: roles, transaccion, bloqueo.)
--
-- Ejecutar DESPUES de 02_permisos_estatus.sql y ANTES de instalar
-- la nueva version de FrmDetalleOrden.cs.
-- Se puede ejecutar varias veces sin duplicar datos.
-- ============================================================
USE MonitoreoOrdenesCompra;
GO

IF OBJECT_ID('dbo.EstatusTransiciones') IS NULL
BEGIN
    CREATE TABLE dbo.EstatusTransiciones
    (
        estatus_origen_id  INT NOT NULL,
        estatus_destino_id INT NOT NULL,
        CONSTRAINT PK_EstatusTransiciones PRIMARY KEY (estatus_origen_id, estatus_destino_id),
        CONSTRAINT CK_EstatusTransiciones_distintos CHECK (estatus_origen_id <> estatus_destino_id)
    );
END
GO

-- Recorrido propuesto:
--   1 Borrador          -> 2 Enviada, 8 Cancelada
--   2 Enviada           -> 3 Confirmada, 8 Cancelada, 9 En Disputa
--   3 Confirmada        -> 4 En Proceso, 8 Cancelada, 9 En Disputa
--   4 En Proceso        -> 5 En Transito, 8 Cancelada, 9 En Disputa
--   5 En Transito       -> 6 Recibida Parcial, 7 Recibida Completa, 9 En Disputa
--   6 Recibida Parcial  -> 7 Recibida Completa, 9 En Disputa
--   7 Recibida Completa -> 10 Cerrada, 9 En Disputa
--   9 En Disputa        -> 3, 4, 5, 6, 7 (se reanuda) o 8 Cancelada
--   8 Cancelada y 10 Cerrada son finales: no admiten mas cambios.
INSERT INTO dbo.EstatusTransiciones (estatus_origen_id, estatus_destino_id)
SELECT v.o, v.d
FROM (VALUES
    (1,2),(1,8),
    (2,3),(2,8),(2,9),
    (3,4),(3,8),(3,9),
    (4,5),(4,8),(4,9),
    (5,6),(5,7),(5,9),
    (6,7),(6,9),
    (7,10),(7,9),
    (9,3),(9,4),(9,5),(9,6),(9,7),(9,8)
) AS v(o, d)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstatusTransiciones t
                  WHERE t.estatus_origen_id = v.o AND t.estatus_destino_id = v.d);
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

    -- 1) Permiso por rol (1 Administrador, 2 Comprador, 4 Supervisor)
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

        -- 3) El cambio debe estar permitido en EstatusTransiciones
        IF NOT EXISTS (SELECT 1 FROM dbo.EstatusTransiciones
                       WHERE estatus_origen_id  = @estatus_actual
                         AND estatus_destino_id = @nuevo_estatus_id)
        BEGIN
            DECLARE @msg NVARCHAR(300) =
                N'Cambio de estatus no permitido: de "'
                + ISNULL((SELECT nombre FROM dbo.EstatusOrden WHERE estatus_id = @estatus_actual), N'?')
                + N'" a "'
                + ISNULL((SELECT nombre FROM dbo.EstatusOrden WHERE estatus_id = @nuevo_estatus_id), N'?')
                + N'".';
            THROW 50005, @msg, 1;
        END

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
        THROW;
    END CATCH
END
GO

-- Para ver el recorrido cargado:
-- SELECT o.nombre AS desde, d.nombre AS hacia
-- FROM dbo.EstatusTransiciones t
-- JOIN dbo.EstatusOrden o ON o.estatus_id = t.estatus_origen_id
-- JOIN dbo.EstatusOrden d ON d.estatus_id = t.estatus_destino_id
-- ORDER BY t.estatus_origen_id, t.estatus_destino_id;
--
-- Para permitir un cambio nuevo (ejemplo: de Enviada a Borrador):
-- INSERT INTO dbo.EstatusTransiciones VALUES (2, 1);
-- Para quitar uno:
-- DELETE FROM dbo.EstatusTransiciones WHERE estatus_origen_id = 2 AND estatus_destino_id = 1;
