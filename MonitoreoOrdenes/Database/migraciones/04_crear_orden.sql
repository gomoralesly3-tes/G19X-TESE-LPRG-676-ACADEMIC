-- ============================================================
-- 04_crear_orden.sql  (migracion para la base de datos existente)
-- Ejecutar ANTES de instalar la nueva FrmNuevaOrden.cs.
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
