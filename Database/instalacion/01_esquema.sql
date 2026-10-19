-- ============================================================
-- 01_esquema.sql  -  Tablas, valores por defecto, llaves foraneas y CHECK
-- Base de datos: MonitoreoOrdenesCompra
-- Generado desde la base real (SSMS > Generar scripts, solo esquema).
-- Se puede ejecutar varias veces: crea solo lo que no existe.
-- ============================================================
IF DB_ID(N'MonitoreoOrdenesCompra') IS NULL
    CREATE DATABASE [MonitoreoOrdenesCompra];
GO
USE [MonitoreoOrdenesCompra]
GO
/****** Object:  Table [dbo].[Alertas] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Alertas]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Alertas](
	[alerta_id] [int] IDENTITY(1,1) NOT NULL,
	[orden_id] [int] NOT NULL,
	[tipo_alerta_id] [int] NOT NULL,
	[mensaje] [varchar](500) NOT NULL,
	[fecha_generada] [datetime] NOT NULL,
	[leida] [bit] NOT NULL,
	[fecha_leida] [datetime] NULL,
	[usuario_id] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[alerta_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Categorias] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categorias]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Categorias](
	[categoria_id] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](100) NOT NULL,
	[descripcion] [varchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[categoria_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[DetalleOrdenCompra] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[DetalleOrdenCompra](
	[detalle_id] [int] IDENTITY(1,1) NOT NULL,
	[orden_id] [int] NOT NULL,
	[producto_id] [int] NOT NULL,
	[cantidad_pedida] [decimal](12, 3) NOT NULL,
	[cantidad_recibida] [decimal](12, 3) NOT NULL,
	[precio_unitario] [decimal](12, 2) NOT NULL,
	[subtotal]  AS ([cantidad_pedida]*[precio_unitario]) PERSISTED,
	[observaciones] [varchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[detalle_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[DetalleRecepcion] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[DetalleRecepcion](
	[det_recepcion_id] [int] IDENTITY(1,1) NOT NULL,
	[recepcion_id] [int] NOT NULL,
	[detalle_orden_id] [int] NOT NULL,
	[cantidad_recibida] [decimal](12, 3) NOT NULL,
	[condicion] [varchar](100) NULL,
	[observaciones] [varchar](300) NULL,
PRIMARY KEY CLUSTERED 
(
	[det_recepcion_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[EstatusOrden] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EstatusOrden]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[EstatusOrden](
	[estatus_id] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](50) NOT NULL,
	[descripcion] [varchar](200) NULL,
	[color_hex] [varchar](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[estatus_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[EstatusTransiciones] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EstatusTransiciones]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[EstatusTransiciones](
	[estatus_origen_id] [int] NOT NULL,
	[estatus_destino_id] [int] NOT NULL,
	[accion] [varchar](60) NULL,
 CONSTRAINT [PK_EstatusTransiciones] PRIMARY KEY CLUSTERED 
(
	[estatus_origen_id] ASC,
	[estatus_destino_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[EstatusTransicionRoles] ******/
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
/****** Object:  Table [dbo].[HistorialEstatusOrden] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[HistorialEstatusOrden](
	[historial_id] [int] IDENTITY(1,1) NOT NULL,
	[orden_id] [int] NOT NULL,
	[estatus_ant_id] [int] NULL,
	[estatus_nuevo_id] [int] NOT NULL,
	[usuario_id] [int] NOT NULL,
	[fecha_cambio] [datetime] NOT NULL,
	[comentario] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[historial_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Incidencias] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Incidencias]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Incidencias](
	[incidencia_id] [int] IDENTITY(1,1) NOT NULL,
	[orden_id] [int] NOT NULL,
	[tipo_id] [int] NOT NULL,
	[usuario_id] [int] NOT NULL,
	[descripcion] [varchar](500) NOT NULL,
	[fecha_reporte] [datetime] NOT NULL,
	[fecha_resolucion] [datetime] NULL,
	[resuelta] [bit] NOT NULL,
	[resolucion_nota] [varchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[incidencia_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[OrdenesCompra] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[OrdenesCompra](
	[orden_id] [int] IDENTITY(1,1) NOT NULL,
	[numero_orden] [varchar](30) NOT NULL,
	[proveedor_id] [int] NOT NULL,
	[usuario_creador_id] [int] NOT NULL,
	[estatus_id] [int] NOT NULL,
	[fecha_emision] [datetime] NOT NULL,
	[fecha_requerida] [datetime] NOT NULL,
	[fecha_entrega_real] [datetime] NULL,
	[monto_total] [decimal](14, 2) NOT NULL,
	[moneda] [varchar](5) NOT NULL,
	[observaciones] [varchar](500) NULL,
	[prioridad] [tinyint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[orden_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[numero_orden] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Productos] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Productos]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Productos](
	[producto_id] [int] IDENTITY(1,1) NOT NULL,
	[codigo] [varchar](50) NOT NULL,
	[nombre] [varchar](200) NOT NULL,
	[descripcion] [varchar](500) NULL,
	[unidad_medida] [varchar](30) NOT NULL,
	[categoria_id] [int] NULL,
	[precio_ref] [decimal](12, 2) NULL,
	[activo] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[producto_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Proveedores] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Proveedores]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Proveedores](
	[proveedor_id] [int] IDENTITY(1,1) NOT NULL,
	[codigo] [varchar](20) NOT NULL,
	[nombre] [varchar](200) NOT NULL,
	[rfc] [varchar](15) NULL,
	[contacto] [varchar](100) NULL,
	[telefono] [varchar](20) NULL,
	[email] [varchar](150) NULL,
	[direccion] [varchar](300) NULL,
	[ciudad] [varchar](100) NULL,
	[pais] [varchar](80) NOT NULL,
	[activo] [bit] NOT NULL,
	[fecha_registro] [datetime] NOT NULL,
	[calificacion] [decimal](3, 2) NULL,
PRIMARY KEY CLUSTERED 
(
	[proveedor_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[ProveedorProductos] ******/
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
/****** Object:  Table [dbo].[Recepciones] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Recepciones]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Recepciones](
	[recepcion_id] [int] IDENTITY(1,1) NOT NULL,
	[orden_id] [int] NOT NULL,
	[usuario_id] [int] NOT NULL,
	[fecha_recepcion] [datetime] NOT NULL,
	[observaciones] [varchar](500) NULL,
	[completa] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[recepcion_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Roles] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Roles](
	[rol_id] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](50) NOT NULL,
	[descripcion] [varchar](200) NULL,
PRIMARY KEY CLUSTERED 
(
	[rol_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[TiposAlerta] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TiposAlerta]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[TiposAlerta](
	[tipo_alerta_id] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](100) NOT NULL,
	[descripcion] [varchar](300) NULL,
	[dias_anticipacion] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[tipo_alerta_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[TiposIncidencia] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TiposIncidencia]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[TiposIncidencia](
	[tipo_id] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](100) NOT NULL,
	[descripcion] [varchar](300) NULL,
	[gravedad] [tinyint] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[tipo_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
/****** Object:  Table [dbo].[Usuarios] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Usuarios]') AND type in (N'U'))
BEGIN
CREATE TABLE [dbo].[Usuarios](
	[usuario_id] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](100) NOT NULL,
	[apellido] [varchar](100) NOT NULL,
	[email] [varchar](150) NOT NULL,
	[username] [varchar](50) NOT NULL,
	[password_hash] [varchar](256) NOT NULL,
	[rol_id] [int] NOT NULL,
	[activo] [bit] NOT NULL,
	[fecha_creacion] [datetime] NOT NULL,
	[ultimo_acceso] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[usuario_id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[username] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Alertas__fecha_g__6477ECF3]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Alertas] ADD  DEFAULT (getdate()) FOR [fecha_generada]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Alertas__leida__656C112C]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Alertas] ADD  DEFAULT ((0)) FOR [leida]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__DetalleOr__canti__48CFD27E]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[DetalleOrdenCompra] ADD  DEFAULT ((0)) FOR [cantidad_recibida]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Historial__fecha__4F7CD00D]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[HistorialEstatusOrden] ADD  DEFAULT (getdate()) FOR [fecha_cambio]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Incidenci__fecha__59FA5E80]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Incidencias] ADD  DEFAULT (getdate()) FOR [fecha_reporte]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Incidenci__resue__5AEE82B9]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Incidencias] ADD  DEFAULT ((0)) FOR [resuelta]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__OrdenesCo__fecha__3F466844]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenesCompra] ADD  DEFAULT (getdate()) FOR [fecha_emision]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__OrdenesCo__monto__403A8C7D]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenesCompra] ADD  DEFAULT ((0)) FOR [monto_total]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__OrdenesCo__moned__412EB0B6]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenesCompra] ADD  DEFAULT ('MXN') FOR [moneda]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__OrdenesCo__prior__4222D4EF]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[OrdenesCompra] ADD  DEFAULT ((2)) FOR [prioridad]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Productos__activ__37A5467C]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Productos] ADD  DEFAULT ((1)) FOR [activo]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Proveedore__pais__2F10007B]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedores] ADD  DEFAULT ('México') FOR [pais]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Proveedor__activ__300424B4]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedores] ADD  DEFAULT ((1)) FOR [activo]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Proveedor__fecha__30F848ED]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Proveedores] ADD  DEFAULT (getdate()) FOR [fecha_registro]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Recepcion__fecha__6B24EA82]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Recepciones] ADD  DEFAULT (getdate()) FOR [fecha_recepcion]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Recepcion__compl__6C190EBB]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Recepciones] ADD  DEFAULT ((0)) FOR [completa]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__TiposAler__dias___619B8048]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[TiposAlerta] ADD  DEFAULT ((3)) FOR [dias_anticipacion]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__TiposInci__grave__571DF1D5]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[TiposIncidencia] ADD  DEFAULT ((2)) FOR [gravedad]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Usuarios__activo__29572725]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT ((1)) FOR [activo]
END
GO
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DF__Usuarios__fecha___2A4B4B5E]') AND type = 'D')
BEGIN
ALTER TABLE [dbo].[Usuarios] ADD  DEFAULT (getdate()) FOR [fecha_creacion]
END
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ALT_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[Alertas]'))
ALTER TABLE [dbo].[Alertas]  WITH CHECK ADD  CONSTRAINT [FK_ALT_Orden] FOREIGN KEY([orden_id])
REFERENCES [dbo].[OrdenesCompra] ([orden_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ALT_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[Alertas]'))
ALTER TABLE [dbo].[Alertas] CHECK CONSTRAINT [FK_ALT_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ALT_TipoAlerta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Alertas]'))
ALTER TABLE [dbo].[Alertas]  WITH CHECK ADD  CONSTRAINT [FK_ALT_TipoAlerta] FOREIGN KEY([tipo_alerta_id])
REFERENCES [dbo].[TiposAlerta] ([tipo_alerta_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ALT_TipoAlerta]') AND parent_object_id = OBJECT_ID(N'[dbo].[Alertas]'))
ALTER TABLE [dbo].[Alertas] CHECK CONSTRAINT [FK_ALT_TipoAlerta]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ALT_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Alertas]'))
ALTER TABLE [dbo].[Alertas]  WITH CHECK ADD  CONSTRAINT [FK_ALT_Usuario] FOREIGN KEY([usuario_id])
REFERENCES [dbo].[Usuarios] ([usuario_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ALT_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Alertas]'))
ALTER TABLE [dbo].[Alertas] CHECK CONSTRAINT [FK_ALT_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DOC_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra]  WITH CHECK ADD  CONSTRAINT [FK_DOC_Orden] FOREIGN KEY([orden_id])
REFERENCES [dbo].[OrdenesCompra] ([orden_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DOC_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra] CHECK CONSTRAINT [FK_DOC_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DOC_Producto]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra]  WITH CHECK ADD  CONSTRAINT [FK_DOC_Producto] FOREIGN KEY([producto_id])
REFERENCES [dbo].[Productos] ([producto_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DOC_Producto]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra] CHECK CONSTRAINT [FK_DOC_Producto]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DR_DetalleOrden]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]'))
ALTER TABLE [dbo].[DetalleRecepcion]  WITH CHECK ADD  CONSTRAINT [FK_DR_DetalleOrden] FOREIGN KEY([detalle_orden_id])
REFERENCES [dbo].[DetalleOrdenCompra] ([detalle_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DR_DetalleOrden]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]'))
ALTER TABLE [dbo].[DetalleRecepcion] CHECK CONSTRAINT [FK_DR_DetalleOrden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DR_Recepcion]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]'))
ALTER TABLE [dbo].[DetalleRecepcion]  WITH CHECK ADD  CONSTRAINT [FK_DR_Recepcion] FOREIGN KEY([recepcion_id])
REFERENCES [dbo].[Recepciones] ([recepcion_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_DR_Recepcion]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]'))
ALTER TABLE [dbo].[DetalleRecepcion] CHECK CONSTRAINT [FK_DR_Recepcion]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_EstatusAnt]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden]  WITH CHECK ADD  CONSTRAINT [FK_HST_EstatusAnt] FOREIGN KEY([estatus_ant_id])
REFERENCES [dbo].[EstatusOrden] ([estatus_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_EstatusAnt]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden] CHECK CONSTRAINT [FK_HST_EstatusAnt]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_EstatusNuevo]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden]  WITH CHECK ADD  CONSTRAINT [FK_HST_EstatusNuevo] FOREIGN KEY([estatus_nuevo_id])
REFERENCES [dbo].[EstatusOrden] ([estatus_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_EstatusNuevo]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden] CHECK CONSTRAINT [FK_HST_EstatusNuevo]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden]  WITH CHECK ADD  CONSTRAINT [FK_HST_Orden] FOREIGN KEY([orden_id])
REFERENCES [dbo].[OrdenesCompra] ([orden_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden] CHECK CONSTRAINT [FK_HST_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden]  WITH CHECK ADD  CONSTRAINT [FK_HST_Usuario] FOREIGN KEY([usuario_id])
REFERENCES [dbo].[Usuarios] ([usuario_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_HST_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[HistorialEstatusOrden]'))
ALTER TABLE [dbo].[HistorialEstatusOrden] CHECK CONSTRAINT [FK_HST_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_INC_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[Incidencias]'))
ALTER TABLE [dbo].[Incidencias]  WITH CHECK ADD  CONSTRAINT [FK_INC_Orden] FOREIGN KEY([orden_id])
REFERENCES [dbo].[OrdenesCompra] ([orden_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_INC_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[Incidencias]'))
ALTER TABLE [dbo].[Incidencias] CHECK CONSTRAINT [FK_INC_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_INC_Tipo]') AND parent_object_id = OBJECT_ID(N'[dbo].[Incidencias]'))
ALTER TABLE [dbo].[Incidencias]  WITH CHECK ADD  CONSTRAINT [FK_INC_Tipo] FOREIGN KEY([tipo_id])
REFERENCES [dbo].[TiposIncidencia] ([tipo_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_INC_Tipo]') AND parent_object_id = OBJECT_ID(N'[dbo].[Incidencias]'))
ALTER TABLE [dbo].[Incidencias] CHECK CONSTRAINT [FK_INC_Tipo]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_INC_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Incidencias]'))
ALTER TABLE [dbo].[Incidencias]  WITH CHECK ADD  CONSTRAINT [FK_INC_Usuario] FOREIGN KEY([usuario_id])
REFERENCES [dbo].[Usuarios] ([usuario_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_INC_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Incidencias]'))
ALTER TABLE [dbo].[Incidencias] CHECK CONSTRAINT [FK_INC_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OC_Estatus]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra]  WITH CHECK ADD  CONSTRAINT [FK_OC_Estatus] FOREIGN KEY([estatus_id])
REFERENCES [dbo].[EstatusOrden] ([estatus_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OC_Estatus]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra] CHECK CONSTRAINT [FK_OC_Estatus]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OC_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra]  WITH CHECK ADD  CONSTRAINT [FK_OC_Proveedor] FOREIGN KEY([proveedor_id])
REFERENCES [dbo].[Proveedores] ([proveedor_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OC_Proveedor]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra] CHECK CONSTRAINT [FK_OC_Proveedor]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OC_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra]  WITH CHECK ADD  CONSTRAINT [FK_OC_Usuario] FOREIGN KEY([usuario_creador_id])
REFERENCES [dbo].[Usuarios] ([usuario_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OC_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra] CHECK CONSTRAINT [FK_OC_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Productos_Categorias]') AND parent_object_id = OBJECT_ID(N'[dbo].[Productos]'))
ALTER TABLE [dbo].[Productos]  WITH CHECK ADD  CONSTRAINT [FK_Productos_Categorias] FOREIGN KEY([categoria_id])
REFERENCES [dbo].[Categorias] ([categoria_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Productos_Categorias]') AND parent_object_id = OBJECT_ID(N'[dbo].[Productos]'))
ALTER TABLE [dbo].[Productos] CHECK CONSTRAINT [FK_Productos_Categorias]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_REC_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[Recepciones]'))
ALTER TABLE [dbo].[Recepciones]  WITH CHECK ADD  CONSTRAINT [FK_REC_Orden] FOREIGN KEY([orden_id])
REFERENCES [dbo].[OrdenesCompra] ([orden_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_REC_Orden]') AND parent_object_id = OBJECT_ID(N'[dbo].[Recepciones]'))
ALTER TABLE [dbo].[Recepciones] CHECK CONSTRAINT [FK_REC_Orden]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_REC_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Recepciones]'))
ALTER TABLE [dbo].[Recepciones]  WITH CHECK ADD  CONSTRAINT [FK_REC_Usuario] FOREIGN KEY([usuario_id])
REFERENCES [dbo].[Usuarios] ([usuario_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_REC_Usuario]') AND parent_object_id = OBJECT_ID(N'[dbo].[Recepciones]'))
ALTER TABLE [dbo].[Recepciones] CHECK CONSTRAINT [FK_REC_Usuario]
GO
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Usuarios_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
ALTER TABLE [dbo].[Usuarios]  WITH CHECK ADD  CONSTRAINT [FK_Usuarios_Roles] FOREIGN KEY([rol_id])
REFERENCES [dbo].[Roles] ([rol_id])
GO
IF  EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Usuarios_Roles]') AND parent_object_id = OBJECT_ID(N'[dbo].[Usuarios]'))
ALTER TABLE [dbo].[Usuarios] CHECK CONSTRAINT [FK_Usuarios_Roles]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_DOC_Cantidad]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra]  WITH CHECK ADD  CONSTRAINT [CK_DOC_Cantidad] CHECK  (([cantidad_pedida]>(0)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_DOC_Cantidad]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra] CHECK CONSTRAINT [CK_DOC_Cantidad]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_DOC_Precio]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra]  WITH CHECK ADD  CONSTRAINT [CK_DOC_Precio] CHECK  (([precio_unitario]>=(0)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_DOC_Precio]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleOrdenCompra]'))
ALTER TABLE [dbo].[DetalleOrdenCompra] CHECK CONSTRAINT [CK_DOC_Precio]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_DR_Cantidad]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]'))
ALTER TABLE [dbo].[DetalleRecepcion]  WITH CHECK ADD  CONSTRAINT [CK_DR_Cantidad] CHECK  (([cantidad_recibida]>(0)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_DR_Cantidad]') AND parent_object_id = OBJECT_ID(N'[dbo].[DetalleRecepcion]'))
ALTER TABLE [dbo].[DetalleRecepcion] CHECK CONSTRAINT [CK_DR_Cantidad]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_EstatusTransiciones_distintos]') AND parent_object_id = OBJECT_ID(N'[dbo].[EstatusTransiciones]'))
ALTER TABLE [dbo].[EstatusTransiciones]  WITH CHECK ADD  CONSTRAINT [CK_EstatusTransiciones_distintos] CHECK  (([estatus_origen_id]<>[estatus_destino_id]))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_EstatusTransiciones_distintos]') AND parent_object_id = OBJECT_ID(N'[dbo].[EstatusTransiciones]'))
ALTER TABLE [dbo].[EstatusTransiciones] CHECK CONSTRAINT [CK_EstatusTransiciones_distintos]
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_OC_Prioridad]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra]  WITH CHECK ADD  CONSTRAINT [CK_OC_Prioridad] CHECK  (([prioridad]=(3) OR [prioridad]=(2) OR [prioridad]=(1)))
GO
IF  EXISTS (SELECT * FROM sys.check_constraints WHERE object_id = OBJECT_ID(N'[dbo].[CK_OC_Prioridad]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrdenesCompra]'))
ALTER TABLE [dbo].[OrdenesCompra] CHECK CONSTRAINT [CK_OC_Prioridad]
GO
