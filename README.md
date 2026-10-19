# Monitoreo de Órdenes de Compra

Aplicación de escritorio (Windows Forms, .NET Framework 4.8) con base de datos SQL Server para dar
seguimiento a las órdenes de compra de **PluriOne S.A. de C.V.**

## Qué hace

- Inicio de sesión con contraseñas guardadas de forma segura (PBKDF2 con sal).
- Dashboard con indicadores, lista de órdenes con búsqueda y detalle con historial de estatus.
- Alta de órdenes con validaciones y numeración automática (`OC-AAAA-NNNN`).
- Flujo de aprobación: `Borrador → Pendiente de Aprobación → Enviada → … → Cerrada`.
- Permisos por rol; el Comprador solo ve y modifica sus propias órdenes.
- Cada proveedor ofrece solo los productos que vende, con su precio pactado.
- Administración desde la aplicación: usuarios, productos por proveedor y cambio de contraseña propia.
- Alertas automáticas (por vencer, vencidas, incidencias abiertas) generadas por un trabajo programado.

## Estructura del repositorio

```
MonitoreoOrdenes/                  ← raíz del repositorio
├── README.md
├── .gitignore
├── MonitoreoOrdenes.sln           ← solución de Visual Studio
├── Database/                      ← TODO lo de la base de datos
│   ├── README.md                  (detalle de los scripts)
│   ├── instalacion/               (01 a 04: construyen la BD desde cero)
│   ├── migraciones/               (cambios históricos, solo referencia)
│   └── jobs/                      (trabajo programado de alertas)
├── docs/
│   └── GUIA_PUBLICAR_GITHUB.md    (para quien mantiene el repositorio)
└── MonitoreoOrdenes/              ← proyecto de C#
    ├── App.config                 (configuración común; sin servidores ni claves)
    ├── conexion.config.example    (modelo de la conexión; cada equipo tiene su conexion.config, que Git ignora)
    ├── entorno.config.example     (modelo del entorno: Desarrollo o Produccion)
    ├── MonitoreoOrdenes.csproj
    ├── Program.cs
    ├── Data/  Forms/  Helpers/  Models/  Properties/
```

## Requisitos

| Programa | Para qué | Notas |
|---|---|---|
| Windows 10 u 11 | Ejecutar la aplicación | |
| Visual Studio 2022 (Community es gratis) | Compilar | Al instalar, marca **Desarrollo de escritorio de .NET** |
| SQL Server 2016 SP1 o superior | Base de datos | Developer o Express. **Express no trae SQL Server Agent** (ver paso 6) |
| SQL Server Management Studio (SSMS) | Ejecutar los scripts | |
| Git para Windows | Descargar el código | O usa GitHub Desktop |

Al instalar SQL Server elige **Windows Authentication** y deja marcado **Add current user** como administrador.
Anota si es instancia por defecto (nombre `.`) o `SQLEXPRESS` (nombre `.\SQLEXPRESS`).

## Instalación en otra laptop

### 1. Descargar el código

```
git clone https://github.com/gomoralesly3-tes/MonitoreoOrdenes.git
```

(Cambia la dirección si tu repositorio es otro. Si es privado, inicia sesión en GitHub cuando te lo pida.)

### 2. Crear la base de datos

Elige **una** de las dos opciones.

#### Opción A: base nueva, vacía (con los scripts)

1. Abre **SSMS** y conéctate: *Server name* = `.` (este equipo), *Authentication* = Windows Authentication.
2. Menú **File → Open → File…** y abre, **en este orden**, estos archivos de la carpeta `Database\instalacion`.
   En cada uno pulsa **Execute (F5)** y espera a que termine **sin texto en rojo** en la pestaña *Messages*:
   1. `01_esquema.sql` (crea la base `MonitoreoOrdenesCompra` y sus tablas)
   2. `02_vistas.sql`
   3. `03_procedimientos.sql`
   4. `04_datos_base.sql` (roles, estatus, tipos de alerta e incidencia, categorías)
3. Comprueba el resultado con esta consulta. Debe dar **17 tablas, 3 vistas y 9 procedimientos**:
   ```sql
   USE MonitoreoOrdenesCompra;
   SELECT (SELECT COUNT(*) FROM sys.tables)     AS tablas,
          (SELECT COUNT(*) FROM sys.views)      AS vistas,
          (SELECT COUNT(*) FROM sys.procedures) AS procedimientos;
   ```
4. **Crea el primer usuario administrador** (la base nueva no trae usuarios). Cambia los datos y escribe
   una clave propia y segura (para este primer paso, solo letras sin acentos ni ñ, números y símbolos comunes):
   ```sql
   USE MonitoreoOrdenesCompra;
   INSERT INTO dbo.Usuarios (nombre, apellido, email, username, password_hash, rol_id)
   VALUES ('Nombre', 'Apellido', 'correo@empresa.com', 'admin',
           LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'ESCRIBE_AQUI_TU_CLAVE'), 2)),
           1);   -- 1 = Administrador
   ```
   Inicia sesión en la aplicación **de inmediato**: en ese momento la clave se guarda en el formato seguro.
   Para más usuarios, repite la consulta con otro `rol_id` (ver tabla de roles). La aplicación todavía no
   tiene pantalla para cambiar contraseñas. **No escribas la clave en ningún archivo del repositorio.**
5. **Importante:** esta base no trae proveedores, productos ni órdenes. Para poder crear órdenes necesitas
   proveedores, productos **y** la relación entre ambos (qué productos vende cada proveedor, con su precio
   pactado en la tabla `ProveedorProductos`). Cárgalos con tus propios `INSERT` (hay ejemplos al final de
   `Database\migraciones\06_proveedor_productos.sql`) o usa la Opción B.

#### Opción B: copiar la base de datos que ya tienes (con sus datos)

En la **computadora original**:
1. En SSMS, clic derecho sobre `MonitoreoOrdenesCompra` → **Tasks → Back Up…** → *Backup type*: Full →
   elige una ruta y un nombre (por ejemplo `MonitoreoOC.bak`) → **OK**.
2. Copia el `.bak` a la laptop nueva con USB, OneDrive o similar.
   **No lo subas a GitHub**: contiene usuarios, contraseñas cifradas y datos reales.

En la **laptop nueva**:
1. En SSMS, clic derecho sobre **Databases** → **Restore Database…** → **Device** → botón `…` → **Add** →
   elige el `.bak` → **OK**.
2. Si marca error de rutas de archivos, entra a la pestaña **Files** y activa
   **Relocate all files to folder**.
3. Los trabajos del Agent no viajan en el respaldo: haz el paso 6 de nuevo.
4. Si el respaldo es anterior a alguna migración de `Database\migraciones`, ejecuta las que falten, en orden
   numérico (cada una se puede repetir sin problema).

### 3. Configurar la conexión y el entorno

La conexión a la base de datos **no** está en `App.config`. Vive en `MonitoreoOrdenes\conexion.config`, un
archivo propio de cada equipo que Git ignora: así cada laptop o servidor usa su propio servidor y sus propias
claves sin tocar el código ni subirlas a GitHub.

**La primera vez que compilas, `conexion.config` se crea solo** a partir de `conexion.config.example`, con
`Server=.` (este equipo). Si tu instancia es otra, abre el archivo y cambia el servidor:

```xml
connectionString="Server=.;Database=MonitoreoOrdenesCompra;Integrated Security=True;TrustServerCertificate=True;"
```

- `Server=.` significa "este equipo". Con Express: `Server=.\SQLEXPRESS`.
- Si compilas y el archivo ya existe, no se sobrescribe.

**Entorno.** `entorno.config` (también se crea solo) dice si el equipo es de `Desarrollo` o de `Produccion`. En
Desarrollo, los títulos de las ventanas muestran `[DESARROLLO]`, para no confundir una laptop de pruebas con el
servidor real. En el servidor de la empresa:

1. Abre `entorno.config` y cambia el valor a `Produccion`.
2. En `conexion.config` apunta al servidor real y, cuando sea posible, usa un certificado válido en lugar de
   `TrustServerCertificate=True`.

### 4. Compilar y ejecutar

1. Abre `MonitoreoOrdenes.sln` con Visual Studio.
2. Si el proyecto aparece como **(Descargado)**, clic derecho → **Volver a cargar el proyecto**.
3. **Compilar → Compilar solución** (Ctrl+Shift+B) y luego **F5**.
4. Inicia sesión con el usuario que creaste.

### 5. Probar

- Inicia sesión con tu usuario administrador.
- Crea una orden (necesitas al menos un proveedor y un producto).
- Envíala a aprobación y apruébala con un usuario Supervisor o Administrador.

### 6. Alertas automáticas

La aplicación **no** genera las alertas por sí sola: las crea `sp_GenerarAlertas`, que debe ejecutarse cada hora.

**Con SQL Server Developer o Standard (tienen Agent):**
1. En SSMS abre `Database\jobs\crear_job_alertas.sql` y pulsa **Execute**.
2. Confirma que el servicio **SQL Server Agent** está en ejecución y en inicio **Automático**
   (tecla Windows → **Servicios** → *SQL Server Agent* → doble clic).
3. Prueba: `EXEC msdb.dbo.sp_start_job @job_name = N'MonitoreoOC - Generar alertas';` y revisa el historial
   (SQL Server Agent → Jobs → clic derecho → **View History**).

**Con SQL Server Express (no tiene Agent):** usa `Database\jobs\generar_alertas.bat` con el Programador de
tareas de Windows, repitiendo la tarea cada hora (pasos en `Database\README.md`).

### 7. Respaldos automáticos

**Con SQL Server Developer o Standard (tienen Agent):**
1. En SSMS abre `Database\jobs\crear_job_respaldo.sql` y pulsa **Execute**.
   Crea un trabajo que cada día a las 02:00 respalda la base, verifica el archivo y borra los respaldos de más
   de 14 días. Por omisión guarda en la carpeta de respaldos del servidor SQL, subcarpeta `MonitoreoOC`
   (puedes cambiarla en el script, en `@CarpetaBase`).
2. Prueba: `EXEC msdb.dbo.sp_start_job @job_name = N'MonitoreoOC - Respaldo diario';` y revisa el historial.
3. **Copia esa carpeta a otro disco o a la nube de vez en cuando:** si el disco falla, los respaldos que están en
   la misma computadora se pierden junto con la base.

**Con SQL Server Express (sin Agent) o antes de un cambio importante:** ejecuta `Database\jobs\respaldo_manual.sql`
para hacer un respaldo en ese momento.

Para restaurar un respaldo: en SSMS, clic derecho en **Databases → Restore Database…** (ver Opción B del paso 2).

## Roles y permisos

| Rol (`rol_id`) | Órdenes que ve | Qué puede hacer |
|---|---|---|
| 1 Administrador | Todas | Todo, incluida la aprobación |
| 2 Comprador | Solo las que él creó | Crear órdenes, enviarlas a aprobación, devolverlas a Borrador y cambiar estatus de las suyas una vez enviadas |
| 3 Almacén | Todas | Cambiar estatus desde "Enviada" en adelante (no aprueba ni toca Borrador/Pendiente) |
| 4 Supervisor | Todas | Aprobar o devolver órdenes pendientes y cambiar estatus después del envío |
| 5 Consulta | Todas | Solo consulta |

Los cambios de estatus permitidos por rol están en la tabla `EstatusTransicionRoles` y los valida la base de datos.

**Pantallas de administración** (en el menú lateral):

| Pantalla | Quién la ve | Para qué |
|---|---|---|
| Usuarios | Solo Administrador | Crear y editar usuarios, activarlos o desactivarlos y restablecer su contraseña |
| Prov. y Productos | Solo Administrador | Definir qué productos vende cada proveedor, con precio pactado y días de entrega |
| Cambiar contraseña | Todos | Cambiar la propia contraseña (mínimo 8 caracteres, con letra y número) |

Nadie puede desactivarse a sí mismo y siempre debe quedar al menos un Administrador activo.

## Variante: la aplicación en una laptop y la base de datos en otra

1. En el equipo con la base de datos: en **SQL Server Configuration Manager** activa **TCP/IP**
   y reinicia el servicio; permite el puerto **1433** en el Firewall de Windows.
2. Entre equipos que no están en un dominio, `Integrated Security` no funciona. Crea un usuario de SQL Server
   (activa *Mixed Mode* en las propiedades del servidor) con permisos sobre la base de datos.
3. En el `conexion.config` de la laptop cliente, la cadena queda así:
   `Server=IP_DEL_SERVIDOR;Database=MonitoreoOrdenesCompra;User Id=USUARIO;Password=CLAVE;TrustServerCertificate=True;`
4. `conexion.config` lleva una contraseña, pero **Git ya lo ignora**: no se sube a GitHub. Aun así, no lo envíes
   por correo ni lo copies a otras carpetas del repositorio.

## Solución de problemas

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| `Invalid object name 'dbo.Usuarios'` (u otra tabla) | Un script se ejecutó en la base equivocada (por ejemplo `master`) | Elige `MonitoreoOrdenesCompra` en el selector de SSMS o inicia el script con `USE MonitoreoOrdenesCompra;`. Si algo quedó en `master`, bórralo allí |
| `A network-related or instance-specific error` | Nombre de servidor mal escrito o servicio detenido | Revisa `Server=` en `conexion.config`; en **Servicios** confirma *SQL Server* en ejecución; el nombre real sale de `SELECT @@SERVERNAME` |
| `Login failed for user '...'` | Tu usuario de Windows no tiene acceso a SQL Server | Ver bloque de abajo |
| `The certificate chain was issued by an authority that is not trusted` | Falta `TrustServerCertificate=True` | Ya viene en la cadena de conexión; verifica que no lo hayas borrado |
| El proyecto aparece como **(Descargado)** y no deja correrlo | Se descargó para editarlo | Clic derecho → **Volver a cargar el proyecto** |
| Error de compilación por .NET Framework 4.8 | Falta el componente en Visual Studio | Visual Studio Installer → Modificar → **Desarrollo de escritorio de .NET** |
| "Usuario o contraseña incorrectos" | Usuario inexistente, inactivo (`activo = 0`) o clave mal escrita | Consulta `SELECT username, activo, rol_id FROM Usuarios;` y restablece la clave (abajo) |
| Las listas de proveedor o producto en "Nueva Orden" están vacías | Base nueva sin datos, o el proveedor no tiene productos asignados | Carga proveedores, productos y la relación en `ProveedorProductos`, o usa la Opción B |
| No aparecen alertas nuevas | Falta el trabajo programado o el Agent está detenido | Paso 6; prueba con `EXEC dbo.sp_GenerarAlertas;` |
| El trabajo de alertas falla con el error 15404 | El propietario del trabajo es una cuenta de Windows | `EXEC msdb.dbo.sp_update_job @job_name = N'MonitoreoOC - Generar alertas', @owner_login_name = N'sa';` |

**Dar acceso a tu usuario de Windows** (solo si te sale `Login failed`; cambia `EQUIPO\Usuario`):

```sql
CREATE LOGIN [EQUIPO\Usuario] FROM WINDOWS;
GO
USE MonitoreoOrdenesCompra;
CREATE USER [EQUIPO\Usuario] FOR LOGIN [EQUIPO\Usuario];
ALTER ROLE db_datareader ADD MEMBER [EQUIPO\Usuario];
ALTER ROLE db_datawriter ADD MEMBER [EQUIPO\Usuario];
GRANT EXECUTE TO [EQUIPO\Usuario];
GRANT EXECUTE ON TYPE::dbo.TipoLineaOrden TO [EQUIPO\Usuario];
```

**Restablecer la clave de un usuario** (clave temporal con caracteres sin acentos ni ñ; se convierte al formato seguro al primer ingreso):

```sql
UPDATE dbo.Usuarios
SET password_hash = LOWER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', 'ClaveTemporal1'), 2))
WHERE username = 'el_usuario';
```

## Seguridad: lista de verificación

- Repositorio de GitHub **privado**.
- No subir contraseñas, archivos `.bak` ni `conexion.config` (Git ya los ignora; no los fuerces con `git add -f`).
- `TrustServerCertificate=True` es aceptable para desarrollo; en producción usa un certificado válido.
- SQL Server **Developer Edition es solo para desarrollo y pruebas**. Para uso real en la empresa se requiere una
  edición con licencia (Standard) o Express.
- Programa los respaldos automáticos (paso 7) y copia la carpeta de respaldos a otro disco o a la nube.

## Pendientes conocidos

- Indicadores de cumplimiento y tiempos de entrega, y analítica de riesgos.
- Que la ventana no se congele cuando la base tarda en responder (`async/await`).
