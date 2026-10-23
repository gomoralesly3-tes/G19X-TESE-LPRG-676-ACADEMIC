-- ============================================================
-- 08_indicadores.sql  (migracion para la base de datos existente)
--
-- Consultas de la pantalla "Indicadores":
--   * sp_IndicadoresProveedores : cumplimiento, tiempos de entrega, retrasos e incidencias por proveedor
--   * sp_RiesgoOrdenesAbiertas  : puntuacion de riesgo (0-100) con los motivos de cada orden abierta
--   * sp_TiempoPorEstatus       : dias que las ordenes pasan en cada estatus
-- El Comprador (rol 2) solo ve las cifras de sus propias ordenes.
--
-- Ejecutar ANTES de instalar los archivos nuevos de la aplicacion.
-- Requiere SQL Server 2016 SP1 o superior. Se puede repetir sin problema.
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

-- ── Indicadores por proveedor ───────────────────────────────────────────
-- Considera solo ordenes ya enviadas (no cuenta Borrador, Pendiente de
-- Aprobacion ni Cancelada). "Entregada" = tiene fecha_entrega_real.
--   % cumplimiento = entregas a tiempo / entregas realizadas
--   @dias = solo ordenes emitidas en los ultimos N dias (NULL = todo el historial)
CREATE OR ALTER PROCEDURE dbo.sp_IndicadoresProveedores
    @usuario_id INT = NULL,
    @dias       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- El Comprador (rol 2) solo ve datos de sus propias ordenes
    DECLARE @solo_creador INT = NULL;
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
        SET @solo_creador = @usuario_id;

    WITH inc AS
    (
        SELECT orden_id,
               COUNT(*) AS total,
               SUM(CASE WHEN resuelta = 0 THEN 1 ELSE 0 END) AS abiertas
        FROM dbo.Incidencias
        GROUP BY orden_id
    )
    SELECT
        pr.proveedor_id,
        pr.codigo,
        pr.nombre AS proveedor,
        COUNT(*) AS ordenes,
        SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL THEN 1 ELSE 0 END) AS entregadas,
        SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL
                  AND oc.fecha_entrega_real <= oc.fecha_requerida THEN 1 ELSE 0 END) AS a_tiempo,
        CAST(SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL
                       AND oc.fecha_entrega_real <= oc.fecha_requerida THEN 1 ELSE 0 END) * 100.0
             / NULLIF(SUM(CASE WHEN oc.fecha_entrega_real IS NOT NULL THEN 1 ELSE 0 END), 0)
             AS DECIMAL(5,1)) AS pct_cumplimiento,
        CAST(AVG(CASE WHEN oc.fecha_entrega_real IS NOT NULL
                      THEN CAST(DATEDIFF(DAY, oc.fecha_emision, oc.fecha_entrega_real) AS DECIMAL(9,2)) END)
             AS DECIMAL(9,1)) AS dias_entrega_prom,
        CAST(AVG(CASE WHEN oc.fecha_entrega_real > oc.fecha_requerida
                      THEN CAST(DATEDIFF(DAY, oc.fecha_requerida, oc.fecha_entrega_real) AS DECIMAL(9,2)) END)
             AS DECIMAL(9,1)) AS retraso_prom_dias,
        SUM(CASE WHEN oc.fecha_entrega_real IS NULL
                  AND oc.estatus_id NOT IN (7, 10)
                  AND oc.fecha_requerida < GETDATE() THEN 1 ELSE 0 END) AS vencidas_abiertas,
        ISNULL(SUM(i.total), 0)    AS incidencias,
        ISNULL(SUM(i.abiertas), 0) AS incidencias_abiertas
    FROM dbo.OrdenesCompra oc
    INNER JOIN dbo.Proveedores pr ON pr.proveedor_id = oc.proveedor_id
    LEFT JOIN inc i ON i.orden_id = oc.orden_id
    WHERE oc.estatus_id NOT IN (1, 8, 11)
      AND (@dias IS NULL OR oc.fecha_emision >= DATEADD(DAY, -@dias, CAST(GETDATE() AS DATE)))
      AND (@solo_creador IS NULL OR oc.usuario_creador_id = @solo_creador)
    GROUP BY pr.proveedor_id, pr.codigo, pr.nombre
    ORDER BY pct_cumplimiento DESC, ordenes DESC;
END
GO

-- ── Riesgo de las ordenes abiertas (reglas explicables, 0 a 100) ────────
-- Ordenes abiertas = todas menos Recibida Completa (7), Cancelada (8) y Cerrada (10).
-- Puntos (los umbrales se pueden ajustar aqui):
--   Vencimiento : vencida 40 (+1 por dia de atraso, max +10) | vence en <=3 dias 25 | en <=7 dias 10
--   Sin cambios de estatus: mas de 7 dias 15 | mas de 3 dias 8
--   Incidencias abiertas  : 2 o mas 20 | 1 diez
--   Proveedor (cumplimiento historico, con 3 o mas entregas): menos de 70% 15 | menos de 85% 8
--   Prioridad alta        : 5
-- Nivel: Alto >= 60, Medio >= 30, Bajo < 30. "motivos" explica cada punto sumado.
CREATE OR ALTER PROCEDURE dbo.sp_RiesgoOrdenesAbiertas
    @usuario_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- El Comprador (rol 2) solo ve datos de sus propias ordenes
    DECLARE @solo_creador INT = NULL;
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
        SET @solo_creador = @usuario_id;

    WITH ult AS
    (
        SELECT orden_id, MAX(fecha_cambio) AS ultima
        FROM dbo.HistorialEstatusOrden
        GROUP BY orden_id
    ),
    inc AS
    (
        SELECT orden_id, COUNT(*) AS abiertas
        FROM dbo.Incidencias
        WHERE resuelta = 0
        GROUP BY orden_id
    ),
    prov AS
    (
        SELECT proveedor_id,
               COUNT(*) AS entregadas,
               SUM(CASE WHEN fecha_entrega_real <= fecha_requerida THEN 1 ELSE 0 END) AS a_tiempo
        FROM dbo.OrdenesCompra
        WHERE fecha_entrega_real IS NOT NULL AND estatus_id NOT IN (1, 8, 11)
        GROUP BY proveedor_id
    ),
    base AS
    (
        SELECT oc.orden_id, oc.numero_orden, pr.nombre AS proveedor, es.nombre AS estatus,
               oc.prioridad,
               DATEDIFF(DAY, CAST(GETDATE() AS DATE), CAST(oc.fecha_requerida AS DATE)) AS dias_para_vencer,
               DATEDIFF(DAY, ISNULL(u.ultima, oc.fecha_emision), GETDATE())             AS dias_sin_movimiento,
               ISNULL(i.abiertas, 0) AS incidencias_abiertas,
               CASE WHEN p.entregadas >= 3 THEN p.a_tiempo * 100.0 / p.entregadas END   AS cumpl_proveedor
        FROM dbo.OrdenesCompra oc
        INNER JOIN dbo.Proveedores pr ON pr.proveedor_id = oc.proveedor_id
        INNER JOIN dbo.EstatusOrden es ON es.estatus_id = oc.estatus_id
        LEFT JOIN ult  u ON u.orden_id     = oc.orden_id
        LEFT JOIN inc  i ON i.orden_id     = oc.orden_id
        LEFT JOIN prov p ON p.proveedor_id = oc.proveedor_id
        WHERE oc.estatus_id NOT IN (7, 8, 10)
          AND (@solo_creador IS NULL OR oc.usuario_creador_id = @solo_creador)
    ),
    puntos AS
    (
        SELECT b.*,
               CASE WHEN b.dias_para_vencer < 0
                         THEN 40 + CASE WHEN -b.dias_para_vencer > 10 THEN 10 ELSE -b.dias_para_vencer END
                    WHEN b.dias_para_vencer <= 3 THEN 25
                    WHEN b.dias_para_vencer <= 7 THEN 10
                    ELSE 0 END AS p_venc,
               CASE WHEN b.dias_sin_movimiento > 7 THEN 15
                    WHEN b.dias_sin_movimiento > 3 THEN 8
                    ELSE 0 END AS p_mov,
               CASE WHEN b.incidencias_abiertas >= 2 THEN 20
                    WHEN b.incidencias_abiertas = 1  THEN 10
                    ELSE 0 END AS p_inc,
               CASE WHEN b.cumpl_proveedor IS NULL THEN 0
                    WHEN b.cumpl_proveedor < 70    THEN 15
                    WHEN b.cumpl_proveedor < 85    THEN 8
                    ELSE 0 END AS p_prov,
               CASE WHEN b.prioridad = 1 THEN 5 ELSE 0 END AS p_prio
        FROM base b
    ),
    total AS
    (
        SELECT p.*, p.p_venc + p.p_mov + p.p_inc + p.p_prov + p.p_prio AS bruto
        FROM puntos p
    )
    SELECT
        t.orden_id, t.numero_orden, t.proveedor, t.estatus, t.prioridad,
        t.dias_para_vencer, t.dias_sin_movimiento, t.incidencias_abiertas,
        CAST(t.cumpl_proveedor AS DECIMAL(5,1)) AS cumplimiento_proveedor,
        CASE WHEN t.bruto > 100 THEN 100 ELSE t.bruto END AS puntaje,
        CASE WHEN t.bruto >= 60 THEN N'Alto'
             WHEN t.bruto >= 30 THEN N'Medio'
             ELSE N'Bajo' END AS nivel,
        ISNULL(STUFF(
              CASE WHEN t.dias_para_vencer < 0
                        THEN N'; Vencida hace ' + CAST(-t.dias_para_vencer AS NVARCHAR(10)) + N' día(s)'
                   WHEN t.dias_para_vencer = 0 THEN N'; Vence hoy'
                   WHEN t.dias_para_vencer <= 7
                        THEN N'; Vence en ' + CAST(t.dias_para_vencer AS NVARCHAR(10)) + N' día(s)'
                   ELSE N'' END
            + CASE WHEN t.dias_sin_movimiento > 3
                        THEN N'; Sin cambio de estatus hace ' + CAST(t.dias_sin_movimiento AS NVARCHAR(10)) + N' día(s)'
                   ELSE N'' END
            + CASE WHEN t.incidencias_abiertas > 0
                        THEN N'; ' + CAST(t.incidencias_abiertas AS NVARCHAR(10)) + N' incidencia(s) abierta(s)'
                   ELSE N'' END
            + CASE WHEN t.p_prov > 0
                        THEN N'; Proveedor con ' + CAST(CAST(t.cumpl_proveedor AS DECIMAL(5,0)) AS NVARCHAR(10)) + N'% de entregas a tiempo'
                   ELSE N'' END
            + CASE WHEN t.prioridad = 1 THEN N'; Prioridad alta' ELSE N'' END
            , 1, 2, N''), N'Sin factores de riesgo') AS motivos
    FROM total t
    ORDER BY CASE WHEN t.bruto > 100 THEN 100 ELSE t.bruto END DESC, t.dias_para_vencer;
END
GO

-- ── Tiempo que las ordenes pasan en cada estatus (cuellos de botella) ───
-- Promedio de dias entre un cambio de estatus y el siguiente. Solo cuenta
-- estancias ya terminadas; una orden que sigue en un estatus no entra todavia.
CREATE OR ALTER PROCEDURE dbo.sp_TiempoPorEstatus
    @usuario_id INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- El Comprador (rol 2) solo ve datos de sus propias ordenes
    DECLARE @solo_creador INT = NULL;
    IF @usuario_id IS NOT NULL
       AND EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 2)
        SET @solo_creador = @usuario_id;

    WITH h AS
    (
        SELECT hh.orden_id,
               hh.estatus_nuevo_id AS estatus_id,
               hh.fecha_cambio,
               LEAD(hh.fecha_cambio) OVER (PARTITION BY hh.orden_id
                                           ORDER BY hh.fecha_cambio, hh.historial_id) AS salida
        FROM dbo.HistorialEstatusOrden hh
        INNER JOIN dbo.OrdenesCompra oc ON oc.orden_id = hh.orden_id
        WHERE (@solo_creador IS NULL OR oc.usuario_creador_id = @solo_creador)
    ),
    est AS
    (
        SELECT estatus_id,
               CAST(DATEDIFF(HOUR, fecha_cambio, salida) / 24.0 AS DECIMAL(9,2)) AS dias
        FROM h
        WHERE salida IS NOT NULL
    )
    SELECT e.estatus_id,
           e.nombre AS estatus,
           COUNT(*) AS estancias,
           CAST(AVG(est.dias) AS DECIMAL(9,1)) AS dias_promedio,
           CAST(MAX(est.dias) AS DECIMAL(9,1)) AS dias_maximo
    FROM est
    INNER JOIN dbo.EstatusOrden e ON e.estatus_id = est.estatus_id
    GROUP BY e.estatus_id, e.nombre
    ORDER BY dias_promedio DESC;
END
GO

-- Prueba rapida (sin usuario = todas las ordenes):
-- EXEC dbo.sp_IndicadoresProveedores;
-- EXEC dbo.sp_RiesgoOrdenesAbiertas;
-- EXEC dbo.sp_TiempoPorEstatus;
