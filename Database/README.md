# Base de datos: MonitoreoOrdenesCompra

## Estructura

| Carpeta | Para qué sirve |
|---|---|
| `instalacion/` | Construir la base de datos **desde cero** (otra PC, servidor nuevo o después de un daño) |
| `migraciones/` | Cambios que ya se aplicaron a la base de datos existente, en orden histórico (solo referencia) |

## Instalar en una base de datos nueva

Ejecutar en este orden, cada archivo completo (F5 en SQL Server Management Studio):

1. `instalacion/01_esquema.sql` — crea la base de datos, tablas, llaves y restricciones
2. `instalacion/02_vistas.sql` — vistas (lista de órdenes, KPIs del dashboard, rendimiento de proveedores)
3. `instalacion/03_procedimientos.sql` — procedimientos almacenados
4. `instalacion/04_datos_base.sql` — roles, estatus, cambios de estatus permitidos y catálogos

Desde la línea de comandos:

    sqlcmd -S SERVIDOR -E -i instalacion\01_esquema.sql
    sqlcmd -S SERVIDOR -E -i instalacion\02_vistas.sql
    sqlcmd -S SERVIDOR -E -i instalacion\03_procedimientos.sql
    sqlcmd -S SERVIDOR -E -i instalacion\04_datos_base.sql

Después, crear el primer usuario administrador con el bloque comentado al final de `04_datos_base.sql`.

## Generación automática de alertas

La aplicación **no** llama a `sp_GenerarAlertas`; hay que programarla en el servidor. Usar solo una de estas opciones:

| Si el servidor tiene… | Usar |
|---|---|
| SQL Server Agent (Standard, Enterprise, Developer) | `jobs/crear_job_alertas.sql` (trabajo cada hora) |
| SQL Server Express (no trae Agent) | `jobs/generar_alertas.bat` + Programador de tareas de Windows (repetir cada hora) |

Ver la edición del servidor: `SELECT SERVERPROPERTY('Edition');`

### Programar `generar_alertas.bat` (SQL Server Express, sin Agent)

1. Guarda `generar_alertas.bat` en una carpeta fija (por ejemplo `C:\MonitoreoOC\`). Ábrelo con el Bloc de notas y
   revisa `set SERVIDOR=...` (para Express suele ser `.\SQLEXPRESS`).
2. Abre el **Programador de tareas** (tecla Windows y escribe el nombre) → **Crear tarea básica…**
   → nombre `MonitoreoOC alertas`.
3. Desencadenador **Diariamente**; Acción **Iniciar un programa** y elige el `.bat`.
4. Al terminar, doble clic en la tarea → pestaña **Desencadenadores** → editar → marca
   **Repetir la tarea cada: 1 hora** durante **Indefinidamente**.
5. Prueba con clic derecho → **Ejecutar**: se crea `generar_alertas.log` junto al `.bat` con el resultado.

Solo funciona mientras la computadora del servidor esté encendida.

## Limpieza pendiente en la base existente

- Tabla `Usuarios_respaldo_20261005`: respaldo temporal creado antes de migrar contraseñas. Se puede borrar cuando todo esté estable.
- `sp_Login`: login antiguo, ya no lo usa la aplicación. Borrarlo cuando todos los usuarios activos hayan iniciado sesión al menos una vez (`DROP PROCEDURE dbo.sp_Login;`). Mientras exista, el hash antiguo funciona como llave de acceso.
- `migraciones/02_permisos_estatus.sql` quedó reemplazado por `03_transiciones_estatus.sql` (el 03 incluye todo lo del 02).
- `migraciones/04_crear_orden.sql` crea `sp_CrearOrdenCompra` (alta de órdenes con permisos y transacción). Ya está incluido en `instalacion/03_procedimientos.sql`.
- `migraciones/06_proveedor_productos.sql` crea `ProveedorProductos` (qué vende cada proveedor, con precio pactado), carga una asignación propuesta y hace que `sp_CrearOrdenCompra` rechace productos que el proveedor no ofrece. Ya está incluido en `instalacion/`.
- `migraciones/07_administracion.sql` agrega los procedimientos de las pantallas de usuarios y de productos por proveedor (solo Administrador). Ya está incluido en `instalacion/03_procedimientos.sql`.
- `migraciones/05_roles_aprobacion.sql` agrega el estatus "Pendiente de Aprobación", los permisos por cambio y por rol (`EstatusTransicionRoles`) y el filtro de órdenes propias para Compradores. Ya está incluido en `instalacion/`.

## Cómo regenerar el esquema

SSMS → clic derecho en la base → Tareas → Generar scripts → tablas, vistas y procedimientos → Avanzadas: *Types of data to script = Schema only*. No incluir datos de `Usuarios`.

## Respaldos

| Archivo | Para qué |
|---|---|
| `jobs/crear_job_respaldo.sql` | Trabajo de SQL Server Agent: respaldo completo diario a las 02:00, con verificación y limpieza de los de más de 14 días |
| `jobs/respaldo_manual.sql` | Respaldo inmediato (sirve en Express, sin Agent) |

Por omisión los archivos van a la carpeta de respaldos del servidor SQL, subcarpeta `MonitoreoOC`. Esos
respaldos están en la misma computadora: copia la carpeta a otro disco o a la nube.
