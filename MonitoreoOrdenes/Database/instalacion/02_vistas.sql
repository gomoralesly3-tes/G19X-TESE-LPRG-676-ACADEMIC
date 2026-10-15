-- ============================================================
-- 02_vistas.sql  -  Vistas usadas por la aplicacion y por reportes
--   vw_OrdenesCompraDetalle  : la usa sp_ObtenerOrdenesCompra (lista de ordenes)
--   vw_KPIs_Dashboard        : la usa el dashboard (tarjetas)
--   vw_RendimientoProveedores: analitica de proveedores (la app aun no la usa)
-- Ejecutar DESPUES de 01_esquema.sql. Se puede repetir (CREATE OR ALTER,
-- requiere SQL Server 2016 SP1 o superior).
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO
/****** Object:  View [dbo].[vw_KPIs_Dashboard] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- VISTA: vw_KPIs_Dashboard
-- KPIs para el panel principal
-- ============================================================
CREATE OR ALTER VIEW [dbo].[vw_KPIs_Dashboard] AS
SELECT
    COUNT(*)                                                                AS total_ordenes,
    SUM(CASE WHEN es.nombre = 'Recibida Completa' THEN 1 ELSE 0 END)       AS ordenes_completadas,
    SUM(CASE WHEN GETDATE() > oc.fecha_requerida AND oc.fecha_entrega_real IS NULL THEN 1 ELSE 0 END) AS ordenes_vencidas,
    SUM(CASE WHEN es.nombre IN ('Enviada','Confirmada','En Proceso','En Tránsito') THEN 1 ELSE 0 END) AS ordenes_activas,
    SUM(CASE WHEN es.nombre = 'En Disputa' THEN 1 ELSE 0 END)              AS ordenes_en_disputa,
    SUM(CASE WHEN oc.prioridad = 1 THEN 1 ELSE 0 END)                      AS ordenes_alta_prioridad,
    ISNULL(SUM(oc.monto_total), 0)                                          AS monto_total_comprometido,
    -- Cumplimiento en %
    CASE
        WHEN COUNT(*) = 0 THEN 0
        ELSE CAST(
            SUM(CASE
                WHEN oc.fecha_entrega_real IS NOT NULL AND oc.fecha_entrega_real <= oc.fecha_requerida THEN 1
                ELSE 0
            END) * 100.0 / NULLIF(SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL THEN 1 ELSE 0 END), 0)
        AS DECIMAL(5,2))
    END AS porcentaje_cumplimiento,
    -- Alertas no leídas
    (SELECT COUNT(*) FROM Alertas WHERE leida = 0)                          AS alertas_pendientes,
    -- Incidencias abiertas
    (SELECT COUNT(*) FROM Incidencias WHERE resuelta = 0)                   AS incidencias_abiertas
FROM OrdenesCompra oc
INNER JOIN EstatusOrden es ON oc.estatus_id = es.estatus_id;
GO
/****** Object:  View [dbo].[vw_OrdenesCompraDetalle] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER VIEW [dbo].[vw_OrdenesCompraDetalle] AS
SELECT
    oc.orden_id,
    oc.numero_orden,
    oc.fecha_emision,
    oc.fecha_requerida,
    oc.fecha_entrega_real,
    oc.monto_total,
    oc.moneda,
    oc.prioridad,
    CASE oc.prioridad WHEN 1 THEN 'Alta' WHEN 2 THEN 'Media' ELSE 'Baja' END AS prioridad_texto,
    oc.observaciones,
    -- Proveedor
    p.proveedor_id,
    p.codigo     AS proveedor_codigo,
    p.nombre     AS proveedor_nombre,
    p.calificacion AS proveedor_calificacion,
    -- Estatus
    es.estatus_id,
    es.nombre    AS estatus_nombre,
    es.color_hex AS estatus_color,
    -- Usuario creador
    u.usuario_id AS creador_id,
    u.nombre + ' ' + u.apellido AS creador_nombre,
    -- Indicadores calculados
    DATEDIFF(DAY, oc.fecha_emision, ISNULL(oc.fecha_entrega_real, GETDATE())) AS dias_transcurridos,
    DATEDIFF(DAY, GETDATE(), oc.fecha_requerida) AS dias_para_vencer,
    CASE
        WHEN oc.fecha_entrega_real IS NOT NULL AND oc.fecha_entrega_real <= oc.fecha_requerida THEN 'A Tiempo'
        WHEN oc.fecha_entrega_real IS NOT NULL AND oc.fecha_entrega_real >  oc.fecha_requerida THEN 'Retrasada'
        WHEN oc.fecha_entrega_real IS NULL AND GETDATE() > oc.fecha_requerida                  THEN 'Vencida'
        WHEN oc.fecha_entrega_real IS NULL AND DATEDIFF(DAY, GETDATE(), oc.fecha_requerida) <= 3 THEN 'Próxima a Vencer'
        ELSE 'En Tiempo'
    END AS cumplimiento,
    -- Contadores
    (SELECT COUNT(*) FROM Incidencias i WHERE i.orden_id = oc.orden_id AND i.resuelta = 0) AS incidencias_abiertas,
    (SELECT COUNT(*) FROM Alertas a     WHERE a.orden_id = oc.orden_id AND a.leida = 0)    AS alertas_no_leidas
FROM OrdenesCompra oc
INNER JOIN Proveedores p ON oc.proveedor_id = p.proveedor_id
INNER JOIN EstatusOrden es ON oc.estatus_id = es.estatus_id
INNER JOIN Usuarios u ON oc.usuario_creador_id = u.usuario_id;
GO
/****** Object:  View [dbo].[vw_RendimientoProveedores] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================================================
-- VISTA: vw_RendimientoProveedores
-- Para analítica de proveedores
-- ============================================================
CREATE OR ALTER VIEW [dbo].[vw_RendimientoProveedores] AS
SELECT
    p.proveedor_id,
    p.codigo,
    p.nombre,
    p.calificacion,
    COUNT(oc.orden_id)                                                          AS total_ordenes,
    SUM(oc.monto_total)                                                         AS monto_total,
    SUM(CASE WHEN oc.fecha_entrega_real <= oc.fecha_requerida THEN 1 ELSE 0 END) AS entregas_tiempo,
    SUM(CASE WHEN oc.fecha_entrega_real > oc.fecha_requerida  THEN 1 ELSE 0 END) AS entregas_tardias,
    SUM(CASE WHEN oc.fecha_entrega_real IS NULL AND GETDATE() > oc.fecha_requerida THEN 1 ELSE 0 END) AS ordenes_vencidas,
    CASE
        WHEN COUNT(oc.orden_id) = 0 THEN 0
        ELSE CAST(
            SUM(CASE WHEN oc.fecha_entrega_real <= oc.fecha_requerida THEN 1 ELSE 0 END) * 100.0
            / NULLIF(SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL THEN 1 ELSE 0 END), 0)
        AS DECIMAL(5,2))
    END AS tasa_cumplimiento_pct,
    (SELECT COUNT(*) FROM Incidencias i WHERE i.orden_id IN (SELECT orden_id FROM OrdenesCompra WHERE proveedor_id = p.proveedor_id)) AS total_incidencias
FROM Proveedores p
LEFT JOIN OrdenesCompra oc ON p.proveedor_id = oc.proveedor_id
GROUP BY p.proveedor_id, p.codigo, p.nombre, p.calificacion;
GO
