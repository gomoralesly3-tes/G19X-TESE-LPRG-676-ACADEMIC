# Monitoreo de Órdenes de Compra

Sistema de escritorio para el seguimiento del ciclo de vida de órdenes de compra. Desarrollado en **Windows Forms / C# (.NET Framework 4.8)** con **SQL Server** como motor de base de datos.

---

## Tabla de contenidos

1. [Requisitos previos](#1-requisitos-previos)
2. [Estructura del proyecto](#2-estructura-del-proyecto)
3. [Instalación de la base de datos](#3-instalación-de-la-base-de-datos)
4. [Configurar la cadena de conexión](#4-configurar-la-cadena-de-conexión)
5. [Compilar y ejecutar la aplicación](#5-compilar-y-ejecutar-la-aplicación)
6. [Primer usuario administrador](#6-primer-usuario-administrador)
7. [Alertas automáticas (opcional)](#7-alertas-automáticas-opcional)
8. [Roles y permisos](#8-roles-y-permisos)
9. [Flujo de estatus de una orden](#9-flujo-de-estatus-de-una-orden)
10. [Solución de problemas comunes](#10-solución-de-problemas-comunes)

---

## 1. Requisitos previos

| Componente | Versión mínima | Notas |
|---|---|---|
| Windows | 10 / 11 | La app es un ejecutable WinForms |
| .NET Framework | **4.8** | Incluido en Windows 10 May 2019 Update y posteriores. [Descargar aquí](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48) si no está presente |
| SQL Server | **2016 SP1** o superior | Las vistas usan `CREATE OR ALTER VIEW`, disponible desde esa versión. Sirve cualquier edición: Express, Developer, Standard o Enterprise |
| SQL Server Management Studio (SSMS) | Cualquier versión reciente | O bien `sqlcmd` desde la línea de comandos |
| Visual Studio | 2019 o 2022 | Solo para compilar. La solución usa .NET Framework 4.8, **sin paquetes NuGet adicionales** |

---

## 2. Estructura del proyecto

```
MonitoreoOrdenes/
├── MonitoreoOrdenes.sln
└── MonitoreoOrdenes/
    ├── App.config                   ← Cadena de conexión (editar aquí)
    ├── Program.cs
    ├── Data/
    │   ├── DatabaseHelper.cs        ← Capa de acceso a datos (ADO.NET)
    │   ├── OrdenCompraRepository.cs
    │   └── UsuarioRepository.cs
    ├── Forms/
    │   ├── FrmLogin.cs
    │   ├── FrmDashboard.cs
    │   ├── FrmNuevaOrden.cs
    │   └── FrmDetalleOrden.cs
    ├── Helpers/
    │   ├── SecurityHelper.cs        ← Hashing PBKDF2-HMAC-SHA256
    │   ├── UITheme.cs
    │   └── UIControls.cs
    ├── Models/
    │   ├── OrdenCompra.cs
    │   ├── Usuario.cs
    │   ├── Alerta.cs
    │   └── KpiDashboard.cs
    └── Database/
        ├── README.md
        ├── instalacion/             ← Scripts para BD nueva (ejecutar en orden)
        │   ├── 01_esquema.sql
        │   ├── 02_vistas.sql
        │   ├── 03_procedimientos.sql
        │   └── 04_datos_base.sql
        ├── migraciones/             ← Historial de cambios (solo referencia)
        └── jobs/                    ← Configuración de alertas automáticas
            ├── crear_job_alertas.sql
            └── generar_alertas.bat
```

---

## 3. Instalación de la base de datos

Los scripts crean la base de datos `MonitoreoOrdenesCompra` y todo su esquema. Todos son **idempotentes**: se pueden ejecutar más de una vez sin errores.

### Opción A — SSMS (interfaz gráfica)

1. Abrir **SQL Server Management Studio** y conectarse al servidor de destino.
2. Abrir cada archivo en el orden indicado y pulsar **F5** (o el botón "Ejecutar"):

| Orden | Archivo | Qué hace |
|---|---|---|
| 1 | `Database/instalacion/01_esquema.sql` | Crea la BD y todas las tablas |
| 2 | `Database/instalacion/02_vistas.sql` | Crea las 3 vistas del dashboard |
| 3 | `Database/instalacion/03_procedimientos.sql` | Crea todos los stored procedures |
| 4 | `Database/instalacion/04_datos_base.sql` | Inserta roles, estatus, catálogos |

### Opción B — `sqlcmd` (línea de comandos)

Reemplazar `NOMBRE_SERVIDOR` por el nombre real de tu instancia SQL Server (ej. `.\SQLEXPRESS` para Express local).

```cmd
sqlcmd -S NOMBRE_SERVIDOR -E -i "Database\instalacion\01_esquema.sql"
sqlcmd -S NOMBRE_SERVIDOR -E -i "Database\instalacion\02_vistas.sql"
sqlcmd -S NOMBRE_SERVIDOR -E -i "Database\instalacion\03_procedimientos.sql"
sqlcmd -S NOMBRE_SERVIDOR -E -i "Database\instalacion\04_datos_base.sql"
```

> **Nota:** El flag `-E` usa autenticación de Windows (Integrated Security). Si tu servidor requiere usuario y contraseña SQL usa `-U usuario -P contraseña` en su lugar.

---

## 4. Configurar la cadena de conexión

Editar el archivo `MonitoreoOrdenes/App.config` y cambiar el valor de `Server` al nombre de tu servidor SQL:

```xml
<connectionStrings>
  <add name="MonitoreoDB"
       connectionString="Server=NOMBRE_SERVIDOR;Database=MonitoreoOrdenesCompra;Integrated Security=True;TrustServerCertificate=True;"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

### Ejemplos de valores para `Server`

| Escenario | Valor |
|---|---|
| SQL Server Express en la misma PC | `.\SQLEXPRESS` o `localhost\SQLEXPRESS` |
| SQL Server local (instancia por defecto) | `.` o `localhost` |
| Servidor en red por nombre | `NOMBRE-PC` o `192.168.1.100` |
| Instancia nombrada en red | `SERVIDOR\INSTANCIA` |

### Autenticación SQL en lugar de Windows

Si el servidor no permite autenticación de Windows, cambiar a:

```xml
connectionString="Server=NOMBRE_SERVIDOR;Database=MonitoreoOrdenesCompra;User Id=mi_usuario;Password=mi_contraseña;TrustServerCertificate=True;"
```

---

## 5. Compilar y ejecutar la aplicación

### Con Visual Studio

1. Abrir `MonitoreoOrdenes.sln` en Visual Studio 2019 o 2022.
2. Verificar que la configuración esté en **Debug** o **Release**.
3. Pulsar **F5** (con depurador) o **Ctrl+F5** (sin depurador).

> No se requiere restaurar paquetes NuGet: el proyecto no usa dependencias externas.

### Ejecutar directamente el binario compilado

Una vez compilado, el ejecutable se encuentra en:

```
MonitoreoOrdenes/bin/Debug/MonitoreoOrdenes.exe
MonitoreoOrdenes/bin/Release/MonitoreoOrdenes.exe
```

Para distribuir a otra máquina, copiar **todo el contenido** de la carpeta `bin/Release/`, no solo el `.exe`. Incluye los archivos de configuración y dependencias del framework necesarios.

> La máquina de destino debe tener **.NET Framework 4.8** instalado.

---

## 6. Primer usuario administrador

Al ejecutar `04_datos_base.sql` se insertan los catálogos, pero **no se crea ningún usuario**. Al final de ese archivo hay un bloque comentado de ejemplo. Ejecutar en SSMS la siguiente consulta, reemplazando los valores:

```sql
USE [MonitoreoOrdenesCompra];
GO

-- Generar el hash con la aplicación, o insertar con hash SHA-256 temporal.
-- La app lo migrará a PBKDF2 automáticamente en el primer login.
INSERT INTO dbo.Usuarios (nombre, apellido, email, username, password_hash, rol_id, activo)
VALUES (
    'Admin',
    'Principal',
    'admin@empresa.com',
    'admin',
    -- Hash SHA-256 de la contraseña 'Admin123' (la app lo actualizará a PBKDF2 en el primer login)
    'a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3',
    1,   -- rol_id 1 = Administrador
    1
);
```

> Tras el primer inicio de sesión correcto, el hash se migra automáticamente a **PBKDF2-HMAC-SHA256** (100.000 iteraciones, sal única por usuario).

---

## 7. Alertas automáticas (opcional)

El procedimiento `sp_GenerarAlertas` revisa órdenes próximas a vencer, vencidas e incidencias abiertas. **La aplicación no lo llama directamente**; debe programarse en el servidor.

### SQL Server Standard / Enterprise / Developer (tiene SQL Server Agent)

Ejecutar en SSMS:

```
Database/jobs/crear_job_alertas.sql
```

Esto crea un trabajo de SQL Agent que ejecuta `sp_GenerarAlertas` cada hora.

### SQL Server Express (sin SQL Server Agent)

Usar el Programador de tareas de Windows con el script batch:

1. Editar `Database/jobs/generar_alertas.bat` y ajustar el nombre del servidor.
2. Abrir el **Programador de tareas de Windows** (taskschd.msc).
3. Crear una tarea básica que ejecute `generar_alertas.bat` con repetición de **1 hora**.

Para saber qué edición tiene tu servidor:

```sql
SELECT SERVERPROPERTY('Edition');
```

---

## 8. Roles y permisos

| rol_id | Rol | Crear órdenes | Cambiar estatus | Ver órdenes |
|---|---|---|---|---|
| 1 | Administrador | ✅ Todas | ✅ Todos los flujos | Todas |
| 2 | Comprador | ✅ Solo las propias | ✅ Sus propias órdenes (desde Enviada) | Solo las propias |
| 3 | Almacén | ❌ | ✅ Desde Enviada en adelante | Todas |
| 4 | Supervisor | ❌ | ✅ Aprueba Pendiente→Enviada; desde Enviada | Todas |
| 5 | Consulta | ❌ | ❌ | Todas (solo lectura) |

---

## 9. Flujo de estatus de una orden

```
Borrador (1)
    └─► Pendiente de Aprobación (11) ──► Enviada (2) ──► Confirmada (3) ──► En Proceso (4)
                                                                                   └─► En Tránsito (5)
                                                                                            ├─► Recibida Parcial (6) ─┐
                                                                                            └─► Recibida Completa (7) ◄┘
                                                                                                     └─► Cerrada (10)
```

- **Cancelada (8)** y **Cerrada (10)** son estados finales (sin salida).
- **En Disputa (9)** puede originarse desde cualquier estado activo y volver a varios estados del flujo.
- Los cambios de estatus son validados por la base de datos (tabla `EstatusTransiciones` + `EstatusTransicionRoles`), no solo por la interfaz.

---

## 10. Solución de problemas comunes

### La aplicación no conecta a la BD
- Verificar que el nombre del servidor en `App.config` es correcto.
- Confirmar que el servicio de SQL Server está corriendo en la máquina de destino.
- Si usas autenticación Windows, el usuario de Windows que ejecuta la app debe tener acceso a la BD.
- Probar la conexión desde SSMS con las mismas credenciales antes de ejecutar la app.

### Error "No se encontró la cadena de conexión 'MonitoreoDB'"
- Asegurarse de estar ejecutando el `.exe` de la carpeta `bin/`, no una copia suelta del ejecutable sin su `.config` al lado.
- Verificar que `MonitoreoOrdenes.exe.config` existe junto al `.exe`.

### Error al ejecutar los scripts SQL ("CREATE OR ALTER VIEW not supported")
- Tu versión de SQL Server es anterior a **2016 SP1**. Actualizar SQL Server o reemplazar `CREATE OR ALTER VIEW` por `CREATE VIEW` (previa eliminación manual si ya existe).

### Los scripts de instalación fallan por permisos
- El usuario de Windows o SQL que ejecuta los scripts necesita al menos el rol `dbcreator` en el servidor (para crear la BD) y `db_owner` en la base ya creada.

### No aparece el botón "Nueva Orden" en el dashboard
- Es por diseño: solo los usuarios con rol **Administrador (1)** o **Comprador (2)** lo ven.

---

## Información del proyecto

| Campo | Valor |
|---|---|
| Empresa | PluriOne S.A. de C.V. |
| Versión | 1.0.0 |
| Framework | .NET Framework 4.8 |
| Base de datos | SQL Server 2016 SP1+ |
| Autenticación BD | Windows Integrated Security (por defecto) |
| Hashing de contraseñas | PBKDF2-HMAC-SHA256, 100 000 iteraciones, sal por usuario |
