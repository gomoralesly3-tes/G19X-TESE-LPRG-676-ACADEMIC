-- ============================================================
-- 06_proveedor_productos.sql  (migracion para la base de datos existente)
--
-- Que hace:
--   * Crea ProveedorProductos: que productos vende cada proveedor, con su
--     precio pactado y (opcional) dias de entrega.
--   * Carga las asignaciones: primero lo que ya aparece en tus ordenes
--     (para no contradecir el historial) y despues una asignacion propuesta
--     por categoria. REVISALA: es una propuesta, no un dato real.
--   * sp_CrearOrdenCompra rechaza productos que el proveedor no ofrece.
--
-- Ejecutar ANTES de instalar la nueva FrmNuevaOrden.cs.
-- Se puede repetir sin problema (no duplica nada).
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

IF OBJECT_ID(N'dbo.ProveedorProductos') IS NULL
BEGIN
    CREATE TABLE dbo.ProveedorProductos
    (
        proveedor_id        INT           NOT NULL,
        producto_id         INT           NOT NULL,
        precio_pactado      DECIMAL(12,2) NOT NULL,
        tiempo_entrega_dias INT           NULL,      -- dias de entrega pactados (NULL = sin definir)
        activo              BIT           NOT NULL CONSTRAINT DF_ProveedorProductos_activo DEFAULT (1),
        CONSTRAINT PK_ProveedorProductos PRIMARY KEY (proveedor_id, producto_id),
        CONSTRAINT FK_ProveedorProductos_Proveedor FOREIGN KEY (proveedor_id) REFERENCES dbo.Proveedores (proveedor_id),
        CONSTRAINT FK_ProveedorProductos_Producto  FOREIGN KEY (producto_id)  REFERENCES dbo.Productos (producto_id),
        CONSTRAINT CK_ProveedorProductos_precio  CHECK (precio_pactado >= 0),
        CONSTRAINT CK_ProveedorProductos_entrega CHECK (tiempo_entrega_dias IS NULL OR tiempo_entrega_dias > 0)
    );
END
GO

-- 1) Respeta el historial: cada proveedor con los productos que ya se le compraron
INSERT INTO dbo.ProveedorProductos (proveedor_id, producto_id, precio_pactado)
SELECT DISTINCT oc.proveedor_id, d.producto_id, ISNULL(p.precio_ref, 0)
FROM dbo.OrdenesCompra oc
INNER JOIN dbo.DetalleOrdenCompra d ON d.orden_id    = oc.orden_id
INNER JOIN dbo.Productos          p ON p.producto_id = d.producto_id
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProveedorProductos x
                  WHERE x.proveedor_id = oc.proveedor_id AND x.producto_id = d.producto_id);
GO

-- 2) Asignacion PROPUESTA por categoria (el precio pactado arranca igual al precio de referencia)
--    1 TechSupply            : Laptop, Monitor, Teclado y Mouse, Switch de Red
--    2 Distribuidora Norte   : Teclado y Mouse, Resma, Folder, Toner
--    3 Papeleria Union       : Resma, Folder, Toner
--    4 Soluciones IT Mexico  : Laptop, Licencia Microsoft 365, Switch de Red
--    5 Muebles y Mas         : Silla Ejecutiva, Escritorio en L
INSERT INTO dbo.ProveedorProductos (proveedor_id, producto_id, precio_pactado)
SELECT v.prov, v.prod, ISNULL(p.precio_ref, 0)
FROM (VALUES
    (1,1),(1,2),(1,3),(1,10),
    (2,3),(2,4),(2,5),(2,9),
    (3,4),(3,5),(3,9),
    (4,1),(4,6),(4,10),
    (5,7),(5,8)
) AS v(prov, prod)
INNER JOIN dbo.Proveedores pr ON pr.proveedor_id = v.prov
INNER JOIN dbo.Productos   p  ON p.producto_id   = v.prod
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProveedorProductos x
                  WHERE x.proveedor_id = v.prov AND x.producto_id = v.prod);
GO

-- 3) Validacion en la base de datos
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

    -- El proveedor solo puede vender los productos que tiene asignados
    IF EXISTS (SELECT 1 FROM @lineas l
               WHERE NOT EXISTS (SELECT 1 FROM dbo.ProveedorProductos pp
                                 WHERE pp.proveedor_id = @proveedor_id
                                   AND pp.producto_id  = l.producto_id
                                   AND pp.activo       = 1))
        THROW 50019, 'Hay productos que el proveedor seleccionado no ofrece.', 1;

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

-- ============================================================
-- Para revisar y mantener (no forman parte de la instalacion)
-- ============================================================
-- Ver la asignacion completa:
-- SELECT pr.nombre AS proveedor, p.nombre AS producto, pp.precio_pactado, pp.tiempo_entrega_dias, pp.activo
-- FROM dbo.ProveedorProductos pp
-- JOIN dbo.Proveedores pr ON pr.proveedor_id = pp.proveedor_id
-- JOIN dbo.Productos   p  ON p.producto_id   = pp.producto_id
-- ORDER BY pr.nombre, p.nombre;
--
-- Agregar un producto a un proveedor (ej.: proveedor 3 vende producto 8 a 7200):
-- INSERT INTO dbo.ProveedorProductos (proveedor_id, producto_id, precio_pactado) VALUES (3, 8, 7200.00);
--
-- Quitarle uno (sin borrar el historial):
-- UPDATE dbo.ProveedorProductos SET activo = 0 WHERE proveedor_id = 3 AND producto_id = 8;
--
-- Cambiar un precio pactado o los dias de entrega:
-- UPDATE dbo.ProveedorProductos SET precio_pactado = 4500, tiempo_entrega_dias = 5
-- WHERE proveedor_id = 5 AND producto_id = 7;
