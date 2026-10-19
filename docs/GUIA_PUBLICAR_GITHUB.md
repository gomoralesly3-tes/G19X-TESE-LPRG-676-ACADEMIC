# Guía para subir el proyecto a GitHub

Para quien mantiene el repositorio. No es necesaria para instalar la aplicación (eso está en `README.md`).

## 1. Antes de empezar

- Tu repositorio ya apunta a `https://github.com/gomoralesly3-tes/MonitoreoOrdenes`. Confírmalo con
  `git remote -v` en la carpeta del proyecto. Si esa cuenta no es tuya o no tienes permiso de escritura,
  mira la sección 6.
- El repositorio debe ser **privado**: incluye nombres de proveedores, la empresa y el diseño de seguridad.

## 2. Carpeta raíz del repositorio

Es la carpeta que contiene `.git` y `MonitoreoOrdenes.sln`
(en tu equipo: `Escritorio\Proyectodevolop\MonitoreoOrdenes`). **No** la carpeta de adentro con el `.csproj`.

## 3. Qué SÍ se sube

```
MonitoreoOrdenes/                      (raíz)
├── README.md
├── .gitignore
├── MonitoreoOrdenes.sln
├── Database/                          (scripts SQL: instalacion, migraciones, jobs)
├── docs/
└── MonitoreoOrdenes/
    ├── App.config                     (configuración común, sin servidores ni claves)
    ├── conexion.config.example        (modelo de la conexión)
    ├── entorno.config.example         (modelo del entorno)
    ├── MonitoreoOrdenes.csproj
    ├── Program.cs
    ├── Data/   Forms/   Helpers/   Models/   Properties/
    └── Forms/FrmLogin.resx            (recursos de formularios)
```

Incluye los archivos nuevos de esta etapa: `Forms/FrmNuevaOrden.cs`, `Helpers/UITheme.cs`,
`Helpers/UIControls.cs` y la carpeta `Database/`.

## 4. Qué NO se sube

| No subir | Por qué |
|---|---|
| `bin/`, `obj/`, `.vs/`, `*.user`, `*.suo` | Los genera Visual Studio. El `.gitignore` ya los excluye |
| `*.rar`, `*.zip`, `*.7z` | No se sube el proyecto comprimido: el código va directo (y el `.rar` trae otra copia de `.git` adentro) |
| `*.bak`, `*.mdf`, `*.ldf` | Respaldos y archivos de base de datos con usuarios y datos reales |
| `generar_alertas.log` | Registro local del Programador de tareas |
| `conexion.config` y `entorno.config` | Son propios de cada equipo y pueden llevar servidor y claves. El `.gitignore` ya los excluye; lo que sí se sube son los `.example` |
| Contraseñas o claves escritas en cualquier archivo | Cualquiera con acceso al repositorio las vería |
| Exportaciones de SSMS con datos (`esquema.sql`, `script.sql`) | Ya están integradas, limpias, en `Database/` |
| Tabla `Usuarios` o sus hashes | Datos reales de personas |

## 5. Pasos para subir

**Preparación (una sola vez):**
1. Copia a la raíz del repositorio: `README.md`, `.gitignore`, `docs/` y `Database/`, reemplazando si existen.
2. Si antes copiaste `Database` dentro de la carpeta interna `MonitoreoOrdenes\MonitoreoOrdenes\`, bórrala de ahí:
   debe estar solo en la raíz, junto al `.sln`.
3. Copia también el `MonitoreoOrdenes.csproj` y los archivos de configuración nuevos (`App.config`,
   `conexion.config.example`, `entorno.config.example`) a la carpeta interna del proyecto. Ya no hay que editar
   el servidor en `App.config`: cada equipo usa su `conexion.config`, que se crea solo al compilar y que Git ignora.
   Comprueba que `App.config` ya no tenga `Server=` ni `Password=`.

**Con Visual Studio:**
1. Menú **Git → Confirmar o stash** (o **Git → Commit or Stash**).
2. Escribe un mensaje, por ejemplo: `Seguridad, roles, aprobación y scripts de base de datos`.
3. Pulsa **Confirmar todo** y después **Insertar** (Push).

**Con la línea de comandos** (en la raíz):

```
git status
git add .
git commit -m "Seguridad, roles, aprobación y scripts de base de datos"
git push
```

Antes de `git commit`, revisa `git status`: no debe aparecer ningún `bin/`, `obj/`, `.rar`, `.zip` ni `.bak`.

## 6. Si `git push` no te deja

- *Permission denied* o *403*: ese repositorio es de otra cuenta. Crea uno tuyo en github.com
  (**New repository → Private**, sin README) y apunta tu proyecto a él:
  ```
  git remote set-url origin https://github.com/TU_USUARIO/MonitoreoOrdenes.git
  git push -u origin master
  ```
- *Updates were rejected*: hay cambios en GitHub que no tienes aquí. Ejecuta `git pull` y luego `git push`.

## 7. Verifica en GitHub

Abre el repositorio en el navegador y comprueba:
- Se ve el `README.md` con las tablas.
- Existen las carpetas `Database/` y `docs/`.
- **No** hay `bin`, `obj`, `.vs`, archivos `.bak` ni `.rar`.
- Abre `MonitoreoOrdenes/App.config`: no debe haber servidor ni contraseñas, y no debe existir `conexion.config` (solo el `.example`).

## 8. Trabajar con dos laptops

- Al terminar en una: confirmar y **Insertar** (push).
- Al empezar en la otra: **Git → Extraer** (pull) o `git pull`.
- Los cambios en la base de datos van como un script nuevo en `Database/migraciones/` (por ejemplo
  `06_descripcion.sql`, con `USE [MonitoreoOrdenesCompra]` al inicio). Cada laptop lo ejecuta una vez y
  también se agrega al script correspondiente de `Database/instalacion/`.

## 9. Si subiste una contraseña o un respaldo por error

Borrarlo en un commit nuevo **no basta**: queda en el historial. Cambia de inmediato esa contraseña y avísame
para ver cómo limpiar el historial.
