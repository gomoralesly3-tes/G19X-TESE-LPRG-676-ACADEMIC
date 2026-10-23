-- ============================================================
-- 04_datos_base.sql  -  Datos que la aplicacion necesita para funcionar
-- Ejecutar DESPUES de 01_esquema.sql, 02_vistas.sql y 03_procedimientos.sql.
-- Se puede repetir: solo inserta lo que no existe.
--
-- Incluye: Roles, EstatusOrden, EstatusTransiciones, EstatusTransicionRoles, TiposAlerta,
-- TiposIncidencia y Categorias.
-- sp_GenerarAlertas usa tipo_alerta_id 1 (Proxima a Vencer), 2 (Vencida)
-- y 6 (Incidencia Abierta): no cambiar esos numeros.
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

-- ── Roles ───────────────────────────────────────────────────
-- Permisos en la aplicacion: 1, 2 y 4 cambian estatus; 1 y 2 crean ordenes.
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (rol_id, nombre, descripcion)
SELECT v.id, v.nombre, v.descr
FROM (VALUES
    (1, 'Administrador', 'Acceso total al sistema'),
    (2, 'Comprador',     'Crea y gestiona órdenes de compra'),
    (3, 'Almacén',       'Registra recepciones de materiales'),
    (4, 'Supervisor',    'Monitorea y aprueba órdenes'),
    (5, 'Consulta',      'Solo lectura de reportes y dashboards')
) AS v(id, nombre, descr)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.rol_id = v.id);
SET IDENTITY_INSERT dbo.Roles OFF;
GO

-- ── Estatus de orden ────────────────────────────────────────
-- Los numeros 7 (Recibida Completa), 8 (Cancelada) y 10 (Cerrada) estan
-- usados dentro de sp_CambiarEstatusOrden y sp_GenerarAlertas.
SET IDENTITY_INSERT dbo.EstatusOrden ON;
INSERT INTO dbo.EstatusOrden (estatus_id, nombre, descripcion, color_hex)
SELECT v.id, v.nombre, v.descr, v.color
FROM (VALUES
    ( 1, 'Borrador',          'Orden creada pero no enviada',            '#9E9E9E'),
    ( 2, 'Enviada',           'Orden enviada al proveedor',              '#2196F3'),
    ( 3, 'Confirmada',        'Proveedor confirmó la orden',             '#03A9F4'),
    ( 4, 'En Proceso',        'Proveedor está preparando el pedido',     '#FF9800'),
    ( 5, 'En Tránsito',       'Pedido en camino',                        '#8BC34A'),
    ( 6, 'Recibida Parcial',  'Recepción parcial de materiales',         '#CDDC39'),
    ( 7, 'Recibida Completa', 'Todos los materiales recibidos',          '#4CAF50'),
    ( 8, 'Cancelada',         'Orden cancelada',                         '#F44336'),
    ( 9, 'En Disputa',        'Existe una incidencia sin resolver',      '#FF5722'),
    (10, 'Cerrada',           'Orden cerrada y conciliada',              '#607D8B'),
    (11, 'Pendiente de Aprobación', 'Orden enviada a aprobación del supervisor', '#FFC107')
) AS v(id, nombre, descr, color)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstatusOrden e WHERE e.estatus_id = v.id);
SET IDENTITY_INSERT dbo.EstatusOrden OFF;
GO

-- ── Flujo de estatus ────────────────────────────────────────
--   1 Borrador -> 11 Pendiente de Aprobación -> 2 Enviada -> ...
--   8 Cancelada y 10 Cerrada son finales (no tienen salida).
INSERT INTO dbo.EstatusTransiciones (estatus_origen_id, estatus_destino_id, accion)
SELECT v.o, v.d, v.accion
FROM (VALUES
    (1,11,'Enviar a aprobación'),(1,8,NULL),
    (11,2,'Aprobar y enviar al proveedor'),(11,1,'Devolver a Borrador'),(11,8,NULL),
    (2,3,NULL),(2,8,NULL),(2,9,NULL),
    (3,4,NULL),(3,8,NULL),(3,9,NULL),
    (4,5,NULL),(4,8,NULL),(4,9,NULL),
    (5,6,NULL),(5,7,NULL),(5,9,NULL),
    (6,7,NULL),(6,9,NULL),
    (7,10,NULL),(7,9,NULL),
    (9,3,NULL),(9,4,NULL),(9,5,NULL),(9,6,NULL),(9,7,NULL),(9,8,NULL)
) AS v(o, d, accion)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstatusTransiciones t
                  WHERE t.estatus_origen_id = v.o AND t.estatus_destino_id = v.d);
GO

-- ── Quien puede hacer cada cambio ───────────────────────────
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

-- ── Tipos de alerta ─────────────────────────────────────────
SET IDENTITY_INSERT dbo.TiposAlerta ON;
INSERT INTO dbo.TiposAlerta (tipo_alerta_id, nombre, descripcion, dias_anticipacion)
SELECT v.id, v.nombre, v.descr, v.dias
FROM (VALUES
    (1, 'Próxima a Vencer',     'La fecha de entrega requerida se acerca',              3),
    (2, 'Vencida',              'La fecha de entrega requerida ya pasó',                0),
    (3, 'Sin Confirmación',     'El proveedor no ha confirmado la orden',               5),
    (4, 'Recepción Incompleta', 'Llevan más de 7 días con recepción parcial',           7),
    (5, 'Alta Prioridad',       'Orden marcada como alta prioridad sin movimiento',     1),
    (6, 'Incidencia Abierta',   'Existe una incidencia sin resolver por más de 3 días', 3)
) AS v(id, nombre, descr, dias)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposAlerta t WHERE t.tipo_alerta_id = v.id);
SET IDENTITY_INSERT dbo.TiposAlerta OFF;
GO

-- ── Tipos de incidencia ─────────────────────────────────────
SET IDENTITY_INSERT dbo.TiposIncidencia ON;
INSERT INTO dbo.TiposIncidencia (tipo_id, nombre, descripcion, gravedad)
SELECT v.id, v.nombre, v.descr, v.gravedad
FROM (VALUES
    (1, 'Retraso en Entrega',           'El proveedor no entregó en la fecha pactada',      1),
    (2, 'Material Dañado',              'Materiales recibidos con daño físico',             1),
    (3, 'Cantidad Incorrecta',          'Cantidad recibida difiere de la ordenada',         2),
    (4, 'Especificaciones Incorrectas', 'El material no cumple las especificaciones',       1),
    (5, 'Factura Incorrecta',           'Datos de facturación no coinciden',                2),
    (6, 'Falta de Documentación',       'Documentos de entrega incompletos',                3),
    (7, 'Comunicación Deficiente',      'Problemas de comunicación con el proveedor',       3),
    (8, 'Precio Incorrecto',            'Precio difiere del acordado en la orden',          1)
) AS v(id, nombre, descr, gravedad)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposIncidencia t WHERE t.tipo_id = v.id);
SET IDENTITY_INSERT dbo.TiposIncidencia OFF;
GO

-- ── Categorias de producto ──────────────────────────────────
SET IDENTITY_INSERT dbo.Categorias ON;
INSERT INTO dbo.Categorias (categoria_id, nombre, descripcion)
SELECT v.id, v.nombre, v.descr
FROM (VALUES
    (1, 'Materiales de Construcción', 'Cemento, varilla, block, etc.'),
    (2, 'Equipos de Cómputo',         'Laptops, servidores, periféricos'),
    (3, 'Papelería y Oficina',        'Papel, bolígrafos, folders'),
    (4, 'Servicios Profesionales',    'Consultoría, capacitación, soporte'),
    (5, 'Mobiliario',                 'Escritorios, sillas, archiveros'),
    (6, 'Software y Licencias',       'Licencias de software y suscripciones'),
    (7, 'Consumibles',                'Tóner, papel, tintas'),
    (8, 'Herramientas',               'Herramienta menor y mayor')
) AS v(id, nombre, descr)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categorias c WHERE c.categoria_id = v.id);
SET IDENTITY_INSERT dbo.Categorias OFF;
GO

-- ── Primer usuario administrador (solo para una BD NUEVA) ───
-- Quita los "--", cambia los datos y escribe una clave PROPIA y segura
-- (para este primer paso solo letras sin acentos ni ñ, numeros y simbolos
-- comunes). Inicia sesion de inmediato: la aplicacion la guarda entonces en
-- el formato seguro PBKDF2. Despues, cada persona puede cambiar su clave en
-- "Cambiar contraseña" y el Administrador puede restablecer la de otros en
-- "Usuarios". NO dejes la clave escrita en este archivo ni la subas a GitHub.
-- INSERT INTO dbo.Usuarios (nombre, apellido, email, username, password_hash, rol_id)
-- VALUES ('Nombre', 'Apellido', 'correo@empresa.com', 'admin',
--         LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'ESCRIBE_UNA_CLAVE_TEMPORAL'), 2)),
--         1);
