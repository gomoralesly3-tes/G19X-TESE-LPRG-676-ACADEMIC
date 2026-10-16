-- ============================================================
-- 05_roles_aprobacion.sql  (migracion para la base de datos existente)
--
-- Que hace:
--   * Compradores: solo ven y modifican SUS ordenes (lista, KPIs y alertas).
--   * Almacen: puede hacer los cambios de estatus del flujo de entrega.
--   * Aprobacion: toda orden pasa por el nuevo estatus
--     "Pendiente de Aprobacion"; solo Supervisor y Administrador aprueban.
--   * Los permisos de cambio de estatus ahora son POR CAMBIO y por rol
--     (tabla EstatusTransicionRoles), no una lista unica de roles.
--
-- Ejecutar ANTES de instalar los archivos nuevos de la aplicacion.
-- Requiere SQL Server 2016 SP1 o superior. Se puede repetir sin problema.
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

-- Nueva columna: texto que ve el usuario en la lista de cambios
IF COL_LENGTH('dbo.EstatusTransiciones', 'accion') IS NULL
    ALTER TABLE dbo.EstatusTransiciones ADD accion VARCHAR(60) NULL;
GO

IF OBJECT_ID(N'dbo.EstatusTransicionRoles') IS NULL
BEGIN
    CREATE TABLE dbo.EstatusTransicionRoles
    (
        estatus_origen_id  INT NOT NULL,
        estatus_destino_id INT NOT NULL,
        rol_id             INT NOT NULL,
        CONSTRAINT PK_EstatusTransicionRoles PRIMARY KEY (estatus_origen_id, estatus_destino_id, rol_id)
    );
END
GO

-- ── Nuevo estatus: Pendiente de Aprobación ──────────────────────────────
SET IDENTITY_INSERT dbo.EstatusOrden ON;
INSERT INTO dbo.EstatusOrden (estatus_id, nombre, descripcion, color_hex)
SELECT 11, 'Pendiente de Aprobación', 'Orden enviada a aprobación del supervisor', '#FFC107'
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstatusOrden WHERE estatus_id = 11);
SET IDENTITY_INSERT dbo.EstatusOrden OFF;
GO

-- ── Flujo: toda orden pasa por aprobación antes de enviarse ─────────────
--   1 Borrador -> 11 Pendiente de Aprobación -> 2 Enviada
--   (ya no existe 1 Borrador -> 2 Enviada directo)
DELETE FROM dbo.EstatusTransiciones WHERE estatus_origen_id = 1 AND estatus_destino_id = 2;

INSERT INTO dbo.EstatusTransiciones (estatus_origen_id, estatus_destino_id)
SELECT v.o, v.d
FROM (VALUES (1,11),(11,2),(11,1),(11,8)) AS v(o, d)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstatusTransiciones t
                  WHERE t.estatus_origen_id = v.o AND t.estatus_destino_id = v.d);
GO

-- Texto que ve el usuario en la lista de cambios (si es NULL se muestra el nombre del estatus)
UPDATE dbo.EstatusTransiciones SET accion = 'Enviar a aprobación'            WHERE estatus_origen_id = 1  AND estatus_destino_id = 11;
UPDATE dbo.EstatusTransiciones SET accion = 'Aprobar y enviar al proveedor'  WHERE estatus_origen_id = 11 AND estatus_destino_id = 2;
UPDATE dbo.EstatusTransiciones SET accion = 'Devolver a Borrador'            WHERE estatus_origen_id = 11 AND estatus_destino_id = 1;
GO

-- ── Quien puede hacer cada cambio ───────────────────────────────────────
-- Desde "Enviada" en adelante (2,3,4,5,6,7,9): Administrador, Comprador,
-- Almacén y Supervisor. Consulta (5) nunca cambia estatus.
INSERT INTO dbo.EstatusTransicionRoles (estatus_origen_id, estatus_destino_id, rol_id)
SELECT t.estatus_origen_id, t.estatus_destino_id, r.rol_id
FROM dbo.EstatusTransiciones t
CROSS JOIN (VALUES (1),(2),(3),(4)) AS r(rol_id)
WHERE t.estatus_origen_id IN (2,3,4,5,6,7,9)
  AND NOT EXISTS (SELECT 1 FROM dbo.EstatusTransicionRoles x
                  WHERE x.estatus_origen_id  = t.estatus_origen_id
                    AND x.estatus_destino_id = t.estatus_destino_id
                    AND x.rol_id             = r.rol_id);

-- Antes de enviarse: el Comprador prepara; el Supervisor aprueba.
--   1 -> 11  Enviar a aprobación : Administrador, Comprador
--   1 -> 8   Cancelar borrador   : Administrador, Comprador
--   11 -> 2  APROBAR             : Administrador, Supervisor   (Comprador NO)
--   11 -> 1  Devolver/rechazar   : Administrador, Comprador, Supervisor
--   11 -> 8  Cancelar            : Administrador, Supervisor
INSERT INTO dbo.EstatusTransicionRoles (estatus_origen_id, estatus_destino_id, rol_id)
SELECT v.o, v.d, v.r
FROM (VALUES
    (1,11,1),(1,11,2),
    (1, 8,1),(1, 8,2),
    (11,2,1),(11,2,4),
    (11,1,1),(11,1,2),(11,1,4),
    (11,8,1),(11,8,4)
) AS v(o, d, r)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstatusTransicionRoles x
                  WHERE x.estatus_origen_id  = v.o
                    AND x.estatus_destino_id = v.d
                    AND x.rol_id             = v.r);
GO

-- ── sp_CambiarEstatusOrden: permisos POR TRANSICION y por duenio ─────────
-- Reglas:
--   * El cambio debe existir en EstatusTransiciones (flujo valido).
--   * El rol del usuario debe estar autorizado para ESE cambio
--     (tabla EstatusTransicionRoles). Ej.: solo Supervisor/Administrador aprueban.
--   * El Comprador (rol 2) solo puede cambiar ordenes que el creo.
--   * Orden + historial en una sola transaccion, con la fila bloqueada.
CREATE OR ALTER PROCEDURE dbo.sp_CambiarEstatusOrden
    @orden_id         INT,
    @nuevo_estatus_id INT,
    @usuario_id       INT,
    @comentario       VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- 1) Usuario activo y con un rol que alguna vez cambia estatus
    DECLARE @rol_id INT;

    SELECT @rol_id = rol_id
    FROM   dbo.Usuarios
    WHERE  usuario_id = @usuario_id
      AND  activo     = 1;

    IF @rol_id IS NULL OR @rol_id NOT IN (1, 2, 3, 4)
        THROW 50001, 'No tiene permisos para cambiar el estatus de una orden.', 1;

    -- 2) El estatus destino debe existir
    IF NOT EXISTS (SELECT 1 FROM dbo.EstatusOrden WHERE estatus_id = @nuevo_estatus_id)
        THROW 50002, 'El estatus seleccionado no existe.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @estatus_actual INT, @creador_id INT;

        -- Bloquea la orden mientras se cambia
        SELECT @estatus_actual = estatus_id,
               @creador_id     = usuario_creador_id
        FROM   dbo.OrdenesCompra WITH (UPDLOCK, ROWLOCK)
        WHERE  orden_id = @orden_id;

        IF @estatus_actual IS NULL
            THROW 50003, 'Orden de compra no encontrada.', 1;

        -- 3) El Comprador solo toca sus propias ordenes
        IF @rol_id = 2 AND ISNULL(@creador_id, 0) <> @usuario_id
            THROW 50006, 'Solo puede modificar las ordenes que usted creo.', 1;

        IF @estatus_actual = @nuevo_estatus_id
            THROW 50004, 'La orden ya tiene ese estatus. Actualice la pantalla.', 1;

        -- 4) El cambio debe ser valido en el flujo
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

        -- 5) Y el rol debe estar autorizado para ese cambio
        IF NOT EXISTS (SELECT 1 FROM dbo.EstatusTransicionRoles
                       WHERE estatus_origen_id  = @estatus_actual
                         AND estatus_destino_id = @nuevo_estatus_id
                         AND rol_id             = @rol_id)
            THROW 50007, 'Su rol no tiene permiso para realizar este cambio de estatus.', 1;

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

-- ── sp_ObtenerOrdenesCompra: el Comprador (rol 2) solo ve sus ordenes ───
-- @usuario_id es el usuario que consulta. Si es NULL no se filtra
-- (compatibilidad con versiones anteriores de la aplicacion).
CREATE OR ALTER PROCEDURE dbo.sp_ObtenerOrdenesCompra
    @estatus_id   INT = NULL,
    @proveedor_id INT = NULL,
    @prioridad    TINYINT = NULL,
    @fecha_desde  DATE = NULL,
    @fecha_hasta  DATE = NULL,
    @numero_orden VARCHAR(30) = NULL,
    @usuario_id   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @solo_creador INT = NULL;
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
        SET @solo_creador = @usuario_id;

    SELECT *
    FROM vw_OrdenesCompraDetalle
    WHERE (@estatus_id   IS NULL OR estatus_id   = @estatus_id)
      AND (@proveedor_id IS NULL OR proveedor_id = @proveedor_id)
      AND (@prioridad    IS NULL OR prioridad    = @prioridad)
      AND (@fecha_desde  IS NULL OR CAST(fecha_emision AS DATE) >= @fecha_desde)
      AND (@fecha_hasta  IS NULL OR CAST(fecha_emision AS DATE) <= @fecha_hasta)
      AND (@numero_orden IS NULL OR numero_orden LIKE '%' + @numero_orden + '%')
      AND (@solo_creador IS NULL OR creador_id = @solo_creador)
    ORDER BY
        CASE prioridad WHEN 1 THEN 0 WHEN 2 THEN 1 ELSE 2 END,
        fecha_requerida ASC;
END
GO

-- ── sp_ObtenerKpisDashboard: mismos KPIs que vw_KPIs_Dashboard, ─────────
-- pero limitados a las ordenes del Comprador cuando el usuario es rol 2.
-- Los SUM llevan ISNULL: un Comprador sin ordenes recibe ceros, no NULL.
CREATE OR ALTER PROCEDURE dbo.sp_ObtenerKpisDashboard
    @usuario_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @solo_creador INT = NULL;
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
        SET @solo_creador = @usuario_id;

    SELECT
        COUNT(*)                                                                              AS total_ordenes,
        ISNULL(SUM(CASE WHEN es.nombre = 'Recibida Completa' THEN 1 ELSE 0 END), 0)           AS ordenes_completadas,
        ISNULL(SUM(CASE WHEN GETDATE() > oc.fecha_requerida AND oc.fecha_entrega_real IS NULL THEN 1 ELSE 0 END), 0) AS ordenes_vencidas,
        ISNULL(SUM(CASE WHEN es.nombre IN ('Enviada','Confirmada','En Proceso','En Tránsito') THEN 1 ELSE 0 END), 0) AS ordenes_activas,
        ISNULL(SUM(CASE WHEN es.nombre = 'En Disputa' THEN 1 ELSE 0 END), 0)                  AS ordenes_en_disputa,
        ISNULL(SUM(CASE WHEN oc.prioridad = 1 THEN 1 ELSE 0 END), 0)                          AS ordenes_alta_prioridad,
        ISNULL(SUM(oc.monto_total), 0)                                                        AS monto_total_comprometido,
        CASE
            WHEN COUNT(*) = 0 THEN 0
            ELSE CAST(
                SUM(CASE
                    WHEN oc.fecha_entrega_real IS NOT NULL AND oc.fecha_entrega_real <= oc.fecha_requerida THEN 1
                    ELSE 0
                END) * 100.0 / NULLIF(SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL THEN 1 ELSE 0 END), 0)
            AS DECIMAL(5,2))
        END                                                                                   AS porcentaje_cumplimiento,
        (SELECT COUNT(*)
         FROM dbo.Alertas a
         INNER JOIN dbo.OrdenesCompra o ON o.orden_id = a.orden_id
         WHERE a.leida = 0
           AND (@solo_creador IS NULL OR o.usuario_creador_id = @solo_creador))               AS alertas_pendientes,
        (SELECT COUNT(*)
         FROM dbo.Incidencias i
         INNER JOIN dbo.OrdenesCompra o ON o.orden_id = i.orden_id
         WHERE i.resuelta = 0
           AND (@solo_creador IS NULL OR o.usuario_creador_id = @solo_creador))               AS incidencias_abiertas
    FROM dbo.OrdenesCompra oc
    INNER JOIN dbo.EstatusOrden es ON oc.estatus_id = es.estatus_id
    WHERE (@solo_creador IS NULL OR oc.usuario_creador_id = @solo_creador);
END
GO

-- ── sp_ObtenerAlertas: el Comprador solo ve alertas de sus ordenes ──────
CREATE OR ALTER PROCEDURE dbo.sp_ObtenerAlertas
    @solo_no_leidas BIT = 1,
    @usuario_id     INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @solo_creador INT = NULL;
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
        SET @solo_creador = @usuario_id;

    SELECT a.alerta_id, a.orden_id, oc.numero_orden,
           ta.nombre AS tipo_alerta, a.mensaje,
           a.fecha_generada, a.leida
    FROM dbo.Alertas a
    INNER JOIN dbo.OrdenesCompra oc ON a.orden_id = oc.orden_id
    INNER JOIN dbo.TiposAlerta   ta ON a.tipo_alerta_id = ta.tipo_alerta_id
    WHERE (@solo_no_leidas = 0 OR a.leida = 0)
      AND (@solo_creador IS NULL OR oc.usuario_creador_id = @solo_creador)
    ORDER BY a.fecha_generada DESC;
END
GO

-- Solo si la aplicacion se conecta con un usuario SQL limitado:
-- GRANT EXECUTE ON dbo.sp_ObtenerKpisDashboard TO TU_USUARIO_SQL;
-- GRANT EXECUTE ON dbo.sp_ObtenerAlertas       TO TU_USUARIO_SQL;

-- Comprobacion rapida: debe mostrar el flujo y los roles de cada cambio
-- SELECT o.nombre AS desde, d.nombre AS hacia, r.rol_id
-- FROM dbo.EstatusTransicionRoles r
-- JOIN dbo.EstatusOrden o ON o.estatus_id = r.estatus_origen_id
-- JOIN dbo.EstatusOrden d ON d.estatus_id = r.estatus_destino_id
-- WHERE r.estatus_origen_id IN (1, 11)
-- ORDER BY r.estatus_origen_id, r.estatus_destino_id, r.rol_id;
