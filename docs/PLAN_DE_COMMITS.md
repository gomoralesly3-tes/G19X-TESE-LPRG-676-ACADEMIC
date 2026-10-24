# Plan de commits

Objetivo: subir los cambios en commits pequeños, uno por tema y con un mensaje que diga qué se hizo.
Un commit grande llamado «cambios» no permite saber qué se modificó ni por qué.

Todos los comandos se ejecutan en la raíz del repositorio (la carpeta con `.git` y el `.sln`).
Revisa antes de cada commit con `git status` que solo estén los archivos de ese tema.

## Antes: quitar la copia vieja de `Database`

Dentro del proyecto quedó una carpeta `MonitoreoOrdenes/Database/` con scripts desactualizados (sin las
migraciones 06 a 09). La carpeta correcta es `Database/`, en la raíz, junto al `.sln`.

```
git rm -r MonitoreoOrdenes/Database
git commit -m "Elimina la copia obsoleta de Database dentro del proyecto" -m "La versión vigente está en Database/ en la raíz."
```

Si `git` responde que esa ruta no está en el repositorio, bórrala desde el Explorador de archivos.

## Commits de los cambios nuevos

**1. Indicadores y riesgo**

```
git add Database/migraciones/08_indicadores.sql MonitoreoOrdenes/Data/IndicadoresRepository.cs MonitoreoOrdenes/Forms/FrmIndicadores.cs MonitoreoOrdenes/Forms/FrmDashboard.cs
git commit -m "Agrega pantalla de indicadores y riesgo de órdenes abiertas" -m "Consultas SQL, repositorio, pantalla con tres pestañas y botón en el menú. Desarrollado con asistencia de Claude (Anthropic)."
```

**2. Incidencias**

```
git add Database/migraciones/09_incidencias.sql MonitoreoOrdenes/Data/IncidenciaRepository.cs MonitoreoOrdenes/Forms/FrmDetalleOrden.cs
git commit -m "Agrega registro y resolución de incidencias en el detalle de la orden" -m "Pestaña Incidencias con permisos por rol validados en la base de datos. Desarrollado con asistencia de Claude (Anthropic)."
```

**3. Proyecto y scripts de instalación**

```
git add MonitoreoOrdenes/MonitoreoOrdenes.csproj Database/instalacion/02_vistas.sql Database/instalacion/03_procedimientos.sql
git commit -m "Incluye las pantallas nuevas en el proyecto y actualiza los scripts de instalación" -m "Registra IndicadoresRepository, FrmIndicadores e IncidenciaRepository en el csproj."
```

(Estos archivos son compartidos por los dos temas anteriores; por eso van juntos. Los commits 1 y 2 no
compilan por separado, pero el 3 deja el proyecto completo.)

**4. Datos de ejemplo opcionales**

```
git add Database/instalacion/05_datos_ejemplo.sql
git commit -m "Agrega script opcional con proveedores y productos de ejemplo" -m "Sirve para probar en una base nueva sin restaurar un respaldo. Desarrollado con asistencia de Claude (Anthropic)."
```

**5. Correcciones del README**

```
git add README.md Database/README.md Database/instalacion/04_datos_base.sql
git commit -m "Corrige el README: dirección de clonado, conteo de objetos y pantalla de contraseña"
```

**6. Declaración de uso de IA**

```
git add docs/USO_DE_IA.md
git commit -m "Declara el uso de inteligencia artificial en el desarrollo"
```

**7. PRD y MVP alineados con el código**

Exporta los documentos actualizados a Markdown y reemplaza los de tu repositorio (ajusta las rutas a donde
estén en tu proyecto):

```
git add docs/PRD.md docs/MVP.md
git commit -m "Actualiza el PRD y el MVP con el estado real del código" -m "Estados Listo, Parcial y Pendiente según lo que hay en el repositorio, con la trazabilidad de requisitos a archivos."
```

**8. Esta guía**

```
git add docs/PLAN_DE_COMMITS.md docs/GUIA_PUBLICAR_GITHUB.md
git commit -m "Agrega la guía para subir cambios en commits pequeños"
```

## Subir

```
git push
```

## Hábito para lo que sigue

- Un commit por cambio con sentido propio: «Corrige el cierre de sesión», «Agrega filtros a la lista».
- Mensaje en presente, de menos de 70 caracteres, que diga qué cambió. El detalle va en el segundo `-m`.
- Si una parte del código o de un documento se generó con ayuda de IA, dilo en el cuerpo del mensaje.
