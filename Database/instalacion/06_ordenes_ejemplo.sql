-- ============================================================
-- 06_ordenes_ejemplo.sql  -  OPCIONAL: Órdenes de prueba con historial e incidencias
--
-- Carga entre 10 y 15 órdenes ficticias con fechas relativas, su historial
-- de transiciones de estatus e incidencias para poblar las pantallas de
-- Indicadores, Riesgo de Órdenes y Tiempos por Estatus.
--
-- Requisito: Debe ejecutarse DESPUÉS de haber creado el primer usuario
-- Administrador (rol_id = 1).
--
-- Protegido contra duplicados mediante WHERE NOT EXISTS por numero_orden.
-- ============================================================
USE [MonitoreoOrdenesCompra1]
GO

-- 1. Obtener el usuario Administrador
DECLARE @admin INT = (SELECT TOP 1 usuario_id FROM dbo.Usuarios WHERE rol_id = 1 ORDER BY usuario_id);
IF @admin IS NULL 
BEGIN 
    RAISERROR('Primero crea el administrador (README, paso 2.4).', 16, 1); 
    RETURN; 
END

-- 2. Insertar Órdenes de Compra ficticias
INSERT INTO dbo.OrdenesCompra (numero_orden, proveedor_id, usuario_creador_id, estatus_id,
       fecha_emision, fecha_requerida, fecha_entrega_real, monto_total, moneda, prioridad)
SELECT v.num, v.prov, @admin, v.est,
       DATEADD(DAY, -v.emitida, GETDATE()), DATEADD(DAY, -v.requerida, GETDATE()),
       CASE WHEN v.entregada IS NULL THEN NULL ELSE DATEADD(DAY, -v.entregada, GETDATE()) END,
       v.monto, 'MXN', v.prio
FROM (VALUES
    -- Entregadas A TIEMPO (fecha_entrega_real <= fecha_requerida)
    ('OC-DEMO-0001', 1, 7, 60, 45, 47,   37000.00, 2),  -- Entregada 2 días antes
    ('OC-DEMO-0002', 2, 7, 50, 35, 36,    4200.00, 2),  -- Entregada 1 día antes
    ('OC-DEMO-0003', 4, 7, 40, 25, 25,   18500.00, 3),  -- Entregada justo a tiempo
    ('OC-DEMO-0004', 5, 7, 30, 15, 15,   24600.00, 2),  -- Entregada a tiempo

    -- Entregadas TARDE / RETRASADAS (fecha_entrega_real > fecha_requerida)
    ('OC-DEMO-0005', 3, 7, 50, 35, 25,    8900.00, 2),  -- Entregada con 10 días de retraso
    ('OC-DEMO-0006', 1, 7, 42, 28, 20,   11200.00, 1),  -- Entregada con 8 días de retraso (Prioridad Alta)

    -- ABIERTAS y VENCIDAS (fecha_requerida < GETDATE(), fecha_entrega_real IS NULL)
    ('OC-DEMO-0007', 4, 5, 20,  3, NULL, 18500.00, 1),  -- Vencida hace 3 días (En Tránsito, Prioridad Alta)
    ('OC-DEMO-0008', 2, 3, 18,  5, NULL,  3500.00, 2),  -- Vencida hace 5 días (Confirmada)
    ('OC-DEMO-0009', 3, 2, 15,  2, NULL,  5400.00, 2),  -- Vencida hace 2 días (Enviada)

    -- ABIERTAS EN TIEMPO (fecha_requerida >= GETDATE(), fecha_entrega_real IS NULL)
    ('OC-DEMO-0010', 1, 5, 10, -5, NULL,  8500.00, 2),  -- Vence en 5 días
    ('OC-DEMO-0011', 5, 2,  5,-10, NULL, 15000.00, 3),  -- Vence en 10 días

    -- EN DISPUTA / CANCELADAS
    ('OC-DEMO-0012', 3, 9, 25, 10, NULL,  5200.00, 1),  -- En Disputa por incidencia
    ('OC-DEMO-0013', 2, 8, 30, 15, NULL,  1200.00, 3)   -- Cancelada
) AS v(num, prov, est, emitida, requerida, entregada, monto, prio)
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompra o WHERE o.numero_orden = v.num);

-- 3. Historial de Estatus (HistorialEstatusOrden)
-- Transiciones para órdenes completadas (Estatus 7)
INSERT INTO dbo.HistorialEstatusOrden (orden_id, estatus_ant_id, estatus_nuevo_id, usuario_id, fecha_cambio, comentario)
SELECT o.orden_id, h.ant, h.nuevo, @admin, DATEADD(DAY, -h.dias_atras, GETDATE()), h.com
FROM dbo.OrdenesCompra o
CROSS APPLY (VALUES
    (NULL, 1, 60, 'Orden creada en borrador'),
    (1,    2, 58, 'Enviada al proveedor'),
    (2,    3, 52, 'Confirmada por el proveedor'),
    (3,    5, 48, 'Pedido en tránsito'),
    (5,    7, 45, 'Recepción de material completa')
) AS h(ant, nuevo, dias_atras, com)
WHERE o.numero_orden = 'OC-DEMO-0001'
  AND NOT EXISTS (SELECT 1 FROM dbo.HistorialEstatusOrden x WHERE x.orden_id = o.orden_id);

-- Transiciones generales para el resto de las órdenes
INSERT INTO dbo.HistorialEstatusOrden (orden_id, estatus_ant_id, estatus_nuevo_id, usuario_id, fecha_cambio, comentario)
SELECT o.orden_id, NULL, 1, @admin, o.fecha_emision, 'Creación de orden'
FROM dbo.OrdenesCompra o
WHERE o.numero_orden LIKE 'OC-DEMO-%'
  AND NOT EXISTS (SELECT 1 FROM dbo.HistorialEstatusOrden x WHERE x.orden_id = o.orden_id AND x.estatus_nuevo_id = 1);

INSERT INTO dbo.HistorialEstatusOrden (orden_id, estatus_ant_id, estatus_nuevo_id, usuario_id, fecha_cambio, comentario)
SELECT o.orden_id, 1, o.estatus_id, @admin, DATEADD(DAY, 1, o.fecha_emision), 'Cambio de estatus inicial'
FROM dbo.OrdenesCompra o
WHERE o.numero_orden LIKE 'OC-DEMO-%' AND o.estatus_id <> 1
  AND NOT EXISTS (SELECT 1 FROM dbo.HistorialEstatusOrden x WHERE x.orden_id = o.orden_id AND x.estatus_nuevo_id = o.estatus_id);

-- 4. Registrar Incidencias (Incidencias)
-- Incidencia 1: Resuelta
INSERT INTO dbo.Incidencias (orden_id, tipo_id, usuario_id, descripcion, fecha_reporte, fecha_resolucion, resuelta, resolucion_nota)
SELECT o.orden_id, 1, @admin, 'Retraso inicial reportado por el transporte.', DATEADD(DAY, -18, GETDATE()), DATEADD(DAY, -10, GETDATE()), 1, 'Se asignó flete exprés sin costo extra.'
FROM dbo.OrdenesCompra o
WHERE o.numero_orden = 'OC-DEMO-0005'
  AND NOT EXISTS (SELECT 1 FROM dbo.Incidencias i WHERE i.orden_id = o.orden_id);

-- Incidencia 2: SIN RESOLVER (Especial para cálculo de riesgo)
INSERT INTO dbo.Incidencias (orden_id, tipo_id, usuario_id, descripcion, fecha_reporte, fecha_resolucion, resuelta, resolucion_nota)
SELECT o.orden_id, 2, @admin, 'Empaque dañado al momento de la entrega parcial.', DATEADD(DAY, -8, GETDATE()), NULL, 0, NULL
FROM dbo.OrdenesCompra o
WHERE o.numero_orden = 'OC-DEMO-0012'
  AND NOT EXISTS (SELECT 1 FROM dbo.Incidencias i WHERE i.orden_id = o.orden_id);

-- Incidencia 3: SIN RESOLVER (Retraso o falta de confirmación)
INSERT INTO dbo.Incidencias (orden_id, tipo_id, usuario_id, descripcion, fecha_reporte, fecha_resolucion, resuelta, resolucion_nota)
SELECT o.orden_id, 1, @admin, 'El proveedor no ha enviado la guía de embarque.', DATEADD(DAY, -4, GETDATE()), NULL, 0, NULL
FROM dbo.OrdenesCompra o
WHERE o.numero_orden = 'OC-DEMO-0007'
  AND NOT EXISTS (SELECT 1 FROM dbo.Incidencias i WHERE i.orden_id = o.orden_id);

PRINT 'Script 06_ordenes_ejemplo.sql ejecutado correctamente con órdenes ficticias de prueba.';
GO