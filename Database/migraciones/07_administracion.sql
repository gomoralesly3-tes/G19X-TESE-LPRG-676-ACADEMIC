-- ============================================================
-- 07_administracion.sql  (migracion para la base de datos existente)
--
-- Que hace: procedimientos de las pantallas de administracion
--   * Usuarios: listar, crear/editar, activar/desactivar, restablecer
--     contraseña (solo Administrador).
--   * Productos por proveedor: consultar y guardar (solo Administrador).
-- El cambio de la contraseña propia usa sp_ActualizarPasswordHash (ya existe).
--
-- Ejecutar ANTES de instalar los archivos nuevos de la aplicacion.
-- Requiere SQL Server 2016 SP1 o superior. Se puede repetir sin problema.
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

-- ── Tipo de tabla para guardar los productos de un proveedor ────────────
IF TYPE_ID(N'dbo.TipoProveedorProducto') IS NULL
    CREATE TYPE dbo.TipoProveedorProducto AS TABLE
    (
        producto_id         INT           NOT NULL,
        vende               BIT           NOT NULL,
        precio_pactado      DECIMAL(12,2) NULL,
        tiempo_entrega_dias INT           NULL
    );
GO

-- ── Usuarios: solo el Administrador (rol 1) los administra ──────────────
-- Nunca devuelve password_hash.
CREATE OR ALTER PROCEDURE dbo.sp_ListarUsuarios
    @admin_id INT
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @admin_id AND activo = 1 AND rol_id = 1)
        THROW 50021, N'Solo el Administrador puede realizar esta acción.', 1;

    SELECT u.usuario_id, u.nombre, u.apellido, u.email, u.username,
           u.rol_id, r.nombre AS rol_nombre, u.activo,
           u.fecha_creacion, u.ultimo_acceso
    FROM dbo.Usuarios u
    INNER JOIN dbo.Roles r ON r.rol_id = u.rol_id
    ORDER BY u.activo DESC, u.nombre, u.apellido;
END
GO

-- Alta (@usuario_id NULL) o edicion de un usuario. El hash de la contraseña lo
-- calcula la aplicacion; solo se usa en el alta. Evita que el sistema se quede
-- sin administradores activos.
CREATE OR ALTER PROCEDURE dbo.sp_GuardarUsuario
    @admin_id      INT,
    @usuario_id    INT          = NULL,
    @nombre        VARCHAR(100),
    @apellido      VARCHAR(100),
    @email         VARCHAR(150),
    @username      VARCHAR(50),
    @rol_id        INT,
    @activo        BIT,
    @password_hash VARCHAR(256) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @admin_id AND activo = 1 AND rol_id = 1)
        THROW 50021, N'Solo el Administrador puede realizar esta acción.', 1;

    SET @nombre   = LTRIM(RTRIM(@nombre));
    SET @apellido = LTRIM(RTRIM(@apellido));
    SET @email    = LTRIM(RTRIM(@email));
    SET @username = LTRIM(RTRIM(@username));

    IF @nombre = '' OR @apellido = '' OR @email = '' OR @username = ''
        THROW 50022, N'Nombre, apellido, correo y usuario son obligatorios.', 1;

    IF @email NOT LIKE '%_@_%._%'
        THROW 50023, N'El correo no tiene un formato válido.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE rol_id = @rol_id)
        THROW 50024, N'El rol seleccionado no existe.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuarios
               WHERE username = @username AND (@usuario_id IS NULL OR usuario_id <> @usuario_id))
        THROW 50025, N'Ya existe un usuario con ese nombre de usuario.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuarios
               WHERE email = @email AND (@usuario_id IS NULL OR usuario_id <> @usuario_id))
        THROW 50026, N'Ya existe un usuario con ese correo.', 1;

    IF @usuario_id IS NULL
    BEGIN
        IF @password_hash IS NULL OR LEN(@password_hash) = 0
            THROW 50027, N'La contraseña es obligatoria para un usuario nuevo.', 1;

        INSERT INTO dbo.Usuarios (nombre, apellido, email, username, password_hash, rol_id, activo)
        VALUES (@nombre, @apellido, @email, @username, @password_hash, @rol_id, @activo);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS usuario_id;
    END
    ELSE
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id)
            THROW 50028, N'El usuario no existe.', 1;

        -- Nadie se desactiva ni se quita el rol de administrador a si mismo
        IF @usuario_id = @admin_id AND (@activo = 0 OR @rol_id <> 1)
            THROW 50029, N'No puede desactivarse ni quitarse el rol de Administrador a sí mismo.', 1;

        -- Siempre debe quedar otro administrador activo
        IF EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id AND rol_id = 1 AND activo = 1)
           AND (@activo = 0 OR @rol_id <> 1)
           AND NOT EXISTS (SELECT 1 FROM dbo.Usuarios
                           WHERE usuario_id <> @usuario_id AND rol_id = 1 AND activo = 1)
            THROW 50030, N'Debe quedar al menos un Administrador activo.', 1;

        UPDATE dbo.Usuarios
        SET    nombre   = @nombre,
               apellido = @apellido,
               email    = @email,
               username = @username,
               rol_id   = @rol_id,
               activo   = @activo
        WHERE  usuario_id = @usuario_id;

        SELECT @usuario_id AS usuario_id;
    END
END
GO

-- El administrador define una contraseña nueva (la aplicacion envia solo el hash).
CREATE OR ALTER PROCEDURE dbo.sp_AdminRestablecerPassword
    @admin_id      INT,
    @usuario_id    INT,
    @password_hash VARCHAR(256)
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @admin_id AND activo = 1 AND rol_id = 1)
        THROW 50021, N'Solo el Administrador puede realizar esta acción.', 1;

    IF @password_hash IS NULL OR LEN(@password_hash) = 0
        THROW 50027, N'La contraseña es obligatoria.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @usuario_id)
        THROW 50028, N'El usuario no existe.', 1;

    UPDATE dbo.Usuarios SET password_hash = @password_hash WHERE usuario_id = @usuario_id;
END
GO

-- ── Productos que vende cada proveedor ──────────────────────────────────
-- Devuelve TODOS los productos activos y, para el proveedor, si los vende.
CREATE OR ALTER PROCEDURE dbo.sp_ObtenerProveedorProductos
    @proveedor_id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.producto_id, p.codigo, p.nombre, c.nombre AS categoria, p.unidad_medida,
           p.precio_ref,
           CAST(CASE WHEN pp.proveedor_id IS NOT NULL AND pp.activo = 1 THEN 1 ELSE 0 END AS BIT) AS vende,
           pp.precio_pactado, pp.tiempo_entrega_dias
    FROM dbo.Productos p
    LEFT JOIN dbo.Categorias c ON c.categoria_id = p.categoria_id
    LEFT JOIN dbo.ProveedorProductos pp
           ON pp.producto_id = p.producto_id AND pp.proveedor_id = @proveedor_id
    WHERE p.activo = 1
    ORDER BY c.nombre, p.nombre;
END
GO

-- Guarda de una vez los productos de un proveedor (solo Administrador).
-- Lo que deja de venderse se DESACTIVA, no se borra: el historial se conserva.
CREATE OR ALTER PROCEDURE dbo.sp_GuardarProveedorProductos
    @admin_id     INT,
    @proveedor_id INT,
    @items        dbo.TipoProveedorProducto READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE usuario_id = @admin_id AND activo = 1 AND rol_id = 1)
        THROW 50021, N'Solo el Administrador puede realizar esta acción.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.Proveedores WHERE proveedor_id = @proveedor_id AND activo = 1)
        THROW 50031, N'El proveedor no existe o está inactivo.', 1;

    IF EXISTS (SELECT 1 FROM @items i
               WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos p WHERE p.producto_id = i.producto_id))
        THROW 50032, N'Hay productos que no existen.', 1;

    IF EXISTS (SELECT 1 FROM @items WHERE vende = 1 AND (precio_pactado IS NULL OR precio_pactado < 0))
        THROW 50033, N'Cada producto que vende el proveedor necesita un precio pactado (0 o mayor).', 1;

    IF EXISTS (SELECT 1 FROM @items WHERE tiempo_entrega_dias IS NOT NULL AND tiempo_entrega_dias <= 0)
        THROW 50034, N'Los días de entrega deben ser mayores a 0.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE pp
        SET    pp.precio_pactado      = i.precio_pactado,
               pp.tiempo_entrega_dias = i.tiempo_entrega_dias,
               pp.activo              = 1
        FROM   dbo.ProveedorProductos pp
        INNER JOIN @items i ON i.producto_id = pp.producto_id
        WHERE  pp.proveedor_id = @proveedor_id AND i.vende = 1;

        INSERT INTO dbo.ProveedorProductos (proveedor_id, producto_id, precio_pactado, tiempo_entrega_dias)
        SELECT @proveedor_id, i.producto_id, i.precio_pactado, i.tiempo_entrega_dias
        FROM   @items i
        WHERE  i.vende = 1
          AND  NOT EXISTS (SELECT 1 FROM dbo.ProveedorProductos pp
                           WHERE pp.proveedor_id = @proveedor_id AND pp.producto_id = i.producto_id);

        UPDATE pp
        SET    pp.activo = 0
        FROM   dbo.ProveedorProductos pp
        INNER JOIN @items i ON i.producto_id = pp.producto_id
        WHERE  pp.proveedor_id = @proveedor_id AND i.vende = 0;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- Solo si la aplicacion se conecta con un usuario SQL limitado:
-- GRANT EXECUTE ON TYPE::dbo.TipoProveedorProducto TO TU_USUARIO_SQL;
-- (y EXECUTE sobre sp_ListarUsuarios, sp_GuardarUsuario, sp_AdminRestablecerPassword,
--  sp_ObtenerProveedorProductos y sp_GuardarProveedorProductos)
