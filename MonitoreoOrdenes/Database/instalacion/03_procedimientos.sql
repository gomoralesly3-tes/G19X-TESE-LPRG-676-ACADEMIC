-- ============================================================
-- 03_procedimientos.sql  -  Procedimientos almacenados de la aplicacion
-- Ejecutar DESPUES de 01_esquema.sql y 02_vistas.sql. Se puede repetir sin problema.
-- No incluye sp_Login (login antiguo, se elimina al terminar la migracion).
-- sp_GenerarAlertas NO la llama la aplicacion: debe programarse como
-- trabajo de SQL Server Agent (ver README.md).
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO
/****** Object:  StoredProcedure [dbo].[sp_ActualizarPasswordHash] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ActualizarPasswordHash]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[sp_ActualizarPasswordHash] AS' 
END
GO

-- 2) Guarda el nuevo hash (la app lo llama tras un login correcto
--    cuando el hash aun esta en formato antiguo).
ALTER   PROCEDURE [dbo].[sp_ActualizarPasswordHash]
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
/****** Object:  StoredProcedure [dbo].[sp_CambiarEstatusOrden] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_CambiarEstatusOrden]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[sp_CambiarEstatusOrden] AS' 
END
GO

ALTER   PROCEDURE [dbo].[sp_CambiarEstatusOrden]
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
/****** Object:  StoredProcedure [dbo].[sp_GenerarAlertas] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_GenerarAlertas]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[sp_GenerarAlertas] AS' 
END
GO

-- ============================================================
-- SP: sp_GenerarAlertas
-- Genera alertas automáticas para órdenes en riesgo
-- (Ejecutar periódicamente con un Job de SQL Agent)
-- ============================================================
ALTER   PROCEDURE [dbo].[sp_GenerarAlertas]
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Órdenes próximas a vencer (3 días o menos, no completadas ni canceladas)
    INSERT INTO Alertas (orden_id, tipo_alerta_id, mensaje)
    SELECT
        oc.orden_id,
        1,
        'Orden ' + oc.numero_orden + ' vence en ' +
        CAST(DATEDIFF(DAY, GETDATE(), oc.fecha_requerida) AS VARCHAR) + ' día(s). Proveedor: ' + p.nombre
    FROM OrdenesCompra oc
    INNER JOIN Proveedores p ON oc.proveedor_id = p.proveedor_id
    WHERE oc.estatus_id NOT IN (7, 8, 10)  -- Excluir completada, cancelada, cerrada
      AND DATEDIFF(DAY, GETDATE(), oc.fecha_requerida) BETWEEN 0 AND 3
      AND NOT EXISTS (
          SELECT 1 FROM Alertas a
          WHERE a.orden_id = oc.orden_id AND a.tipo_alerta_id = 1
            AND CAST(a.fecha_generada AS DATE) = CAST(GETDATE() AS DATE)
      );

    -- 2. Órdenes vencidas
    INSERT INTO Alertas (orden_id, tipo_alerta_id, mensaje)
    SELECT
        oc.orden_id,
        2,
        'Orden ' + oc.numero_orden + ' VENCIDA hace ' +
        CAST(ABS(DATEDIFF(DAY, GETDATE(), oc.fecha_requerida)) AS VARCHAR) + ' día(s). Proveedor: ' + p.nombre
    FROM OrdenesCompra oc
    INNER JOIN Proveedores p ON oc.proveedor_id = p.proveedor_id
    WHERE oc.estatus_id NOT IN (7, 8, 10)
      AND GETDATE() > oc.fecha_requerida
      AND NOT EXISTS (
          SELECT 1 FROM Alertas a
          WHERE a.orden_id = oc.orden_id AND a.tipo_alerta_id = 2
            AND CAST(a.fecha_generada AS DATE) = CAST(GETDATE() AS DATE)
      );

    -- 3. Incidencias abiertas por más de 3 días
    INSERT INTO Alertas (orden_id, tipo_alerta_id, mensaje)
    SELECT DISTINCT
        i.orden_id,
        6,
        'Orden ' + oc.numero_orden + ' tiene incidencia abierta hace ' +
        CAST(DATEDIFF(DAY, i.fecha_reporte, GETDATE()) AS VARCHAR) + ' día(s) sin resolver.'
    FROM Incidencias i
    INNER JOIN OrdenesCompra oc ON i.orden_id = oc.orden_id
    WHERE i.resuelta = 0
      AND DATEDIFF(DAY, i.fecha_reporte, GETDATE()) > 3
      AND NOT EXISTS (
          SELECT 1 FROM Alertas a
          WHERE a.orden_id = i.orden_id AND a.tipo_alerta_id = 6
            AND CAST(a.fecha_generada AS DATE) = CAST(GETDATE() AS DATE)
      );

    PRINT 'Alertas generadas correctamente: ' + CAST(@@ROWCOUNT AS VARCHAR);
END
GO
/****** Object:  StoredProcedure [dbo].[sp_ObtenerCredenciales] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ObtenerCredenciales]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[sp_ObtenerCredenciales] AS' 
END
GO

-- 1) Devuelve los datos del usuario ACTIVO y su hash almacenado.
--    La verificacion de la contraseña se hace en la aplicacion.
ALTER   PROCEDURE [dbo].[sp_ObtenerCredenciales]
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
/****** Object:  StoredProcedure [dbo].[sp_ObtenerOrdenesCompra] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_ObtenerOrdenesCompra]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[sp_ObtenerOrdenesCompra] AS' 
END
GO

-- ============================================================
-- SP: sp_ObtenerOrdenesCompra
-- Consulta órdenes con filtros opcionales
-- ============================================================
ALTER   PROCEDURE [dbo].[sp_ObtenerOrdenesCompra]
    @estatus_id   INT = NULL,
    @proveedor_id INT = NULL,
    @prioridad    TINYINT = NULL,
    @fecha_desde  DATE = NULL,
    @fecha_hasta  DATE = NULL,
    @numero_orden VARCHAR(30) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM vw_OrdenesCompraDetalle
    WHERE (@estatus_id   IS NULL OR estatus_id   = @estatus_id)
      AND (@proveedor_id IS NULL OR proveedor_id = @proveedor_id)
      AND (@prioridad    IS NULL OR prioridad    = @prioridad)
      AND (@fecha_desde  IS NULL OR CAST(fecha_emision AS DATE) >= @fecha_desde)
      AND (@fecha_hasta  IS NULL OR CAST(fecha_emision AS DATE) <= @fecha_hasta)
      AND (@numero_orden IS NULL OR numero_orden LIKE '%' + @numero_orden + '%')
    ORDER BY
        CASE prioridad WHEN 1 THEN 0 WHEN 2 THEN 1 ELSE 2 END,
        fecha_requerida ASC;
END
GO
/****** Object:  StoredProcedure [dbo].[sp_RegistrarAcceso] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[sp_RegistrarAcceso]') AND type in (N'P', N'PC'))
BEGIN
EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[sp_RegistrarAcceso] AS' 
END
GO

-- 3) Registra el ultimo acceso.
ALTER   PROCEDURE [dbo].[sp_RegistrarAcceso]
    @usuario_id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuarios
    SET    ultimo_acceso = GETDATE()
    WHERE  usuario_id    = @usuario_id;
END

GO

-- ============================================================
-- Crear orden de compra de forma segura
--   * Solo roles 1 (Administrador) y 2 (Comprador) pueden crear ordenes.
--   * Valida proveedor, productos, cantidades, precios y fecha.
--   * Asigna el numero de orden dentro de la transaccion (sin duplicados
--     cuando dos usuarios guardan al mismo tiempo; reinicia cada anio:
--     OC-2027-0001).
--   * Calcula el monto total en el servidor.
--   * Guarda cabecera, lineas e historial en UNA transaccion: o se guarda
--     todo o no se guarda nada (antes podian quedar ordenes a medias).
-- Requiere SQL Server 2016 SP1 o superior.
-- ============================================================
IF TYPE_ID(N'dbo.TipoLineaOrden') IS NULL
    CREATE TYPE dbo.TipoLineaOrden AS TABLE
    (
        producto_id     INT           NOT NULL,
        cantidad        DECIMAL(12,3) NOT NULL,
        precio_unitario DECIMAL(12,2) NOT NULL
    );
GO

CREATE OR ALTER PROCEDURE dbo.sp_CrearOrdenCompra
    @proveedor_id    INT,
    @usuario_id      INT,
    @fecha_requerida DATE,
    @prioridad       TINYINT,
    @observaciones   VARCHAR(500) = NULL,
    @lineas          dbo.TipoLineaOrden READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- 1) Permiso por rol (1 Administrador, 2 Comprador)
    DECLARE @rol_id INT;

    SELECT @rol_id = rol_id
    FROM   dbo.Usuarios
    WHERE  usuario_id = @usuario_id
      AND  activo     = 1;

    IF @rol_id IS NULL OR @rol_id NOT IN (1, 2)
        THROW 50011, 'No tiene permisos para crear ordenes de compra.', 1;

    -- 2) Validaciones
    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores WHERE proveedor_id = @proveedor_id AND activo = 1)
        THROW 50012, 'El proveedor no existe o esta inactivo.', 1;

    IF @prioridad NOT IN (1, 2, 3)
        THROW 50013, 'La prioridad no es valida.', 1;

    IF NOT EXISTS (SELECT 1 FROM @lineas)
        THROW 50014, 'La orden debe tener al menos una linea.', 1;

    IF EXISTS (SELECT 1 FROM @lineas WHERE cantidad <= 0 OR precio_unitario < 0)
        THROW 50015, 'Cada linea debe tener cantidad mayor a 0 y precio no negativo.', 1;

    IF EXISTS (SELECT 1 FROM @lineas l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos p
                                 WHERE p.producto_id = l.producto_id AND p.activo = 1))
        THROW 50016, 'Hay productos inexistentes o inactivos en las lineas.', 1;

    IF @fecha_requerida < CAST(GETDATE() AS DATE)
        THROW 50017, 'La fecha requerida no puede ser anterior a hoy.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 3) Reserva el numero: un solo usuario a la vez
        DECLARE @lock INT;
        EXEC @lock = sp_getapplock
             @Resource    = N'NumeroOrdenCompra',
             @LockMode    = 'Exclusive',
             @LockOwner   = 'Transaction',
             @LockTimeout = 10000;

        IF @lock < 0
            THROW 50018, 'No se pudo reservar el numero de orden. Intente de nuevo.', 1;

        DECLARE @anio      CHAR(4) = CAST(YEAR(GETDATE()) AS CHAR(4));
        DECLARE @siguiente INT;

        -- Formato OC-AAAA-NNNN: el consecutivo empieza en la posicion 9
        SELECT @siguiente = ISNULL(MAX(TRY_CAST(SUBSTRING(numero_orden, 9, 20) AS INT)), 0) + 1
        FROM   dbo.OrdenesCompra
        WHERE  numero_orden LIKE 'OC-' + @anio + '-%';

        DECLARE @numero VARCHAR(30) = 'OC-' + @anio + '-' + FORMAT(@siguiente, '0000');

        -- 4) Monto total calculado en el servidor
        DECLARE @monto DECIMAL(14,2) =
            (SELECT ISNULL(SUM(ROUND(cantidad * precio_unitario, 2)), 0) FROM @lineas);

        -- 5) Cabecera, lineas e historial
        DECLARE @orden_id INT;

        INSERT INTO dbo.OrdenesCompra
               (numero_orden, proveedor_id, usuario_creador_id, estatus_id,
                fecha_emision, fecha_requerida, monto_total, prioridad, observaciones)
        VALUES (@numero, @proveedor_id, @usuario_id, 1,
                GETDATE(), @fecha_requerida, @monto, @prioridad,
                NULLIF(LTRIM(RTRIM(@observaciones)), ''));

        SET @orden_id = SCOPE_IDENTITY();

        INSERT INTO dbo.DetalleOrdenCompra (orden_id, producto_id, cantidad_pedida, precio_unitario)
        SELECT @orden_id, producto_id, cantidad, precio_unitario
        FROM   @lineas;

        INSERT INTO dbo.HistorialEstatusOrden (orden_id, estatus_nuevo_id, usuario_id, comentario)
        VALUES (@orden_id, 1, @usuario_id, 'Orden creada');

        COMMIT TRANSACTION;

        SELECT @orden_id AS orden_id, @numero AS numero_orden;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Solo si la aplicacion se conecta con un usuario SQL limitado
-- (con Windows/sa no hace falta). Cambia TU_USUARIO_SQL y quita los "--":
-- GRANT EXECUTE ON TYPE::dbo.TipoLineaOrden   TO TU_USUARIO_SQL;
-- GRANT EXECUTE ON dbo.sp_CrearOrdenCompra    TO TU_USUARIO_SQL;
