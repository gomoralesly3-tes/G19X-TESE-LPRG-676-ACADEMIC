# PRD — Plataforma de Monitoreo de Órdenes de Compra

**Producto de PluriOne S.A. de C.V. · Versión 1.0 · Octubre 2026 · Estado: borrador para validación**

Este documento describe qué debe hacer el producto, para quién y con qué prioridad. Indica además qué parte ya está construida y qué falta. Las metas numéricas son propuestas para validar con la empresa; no son mediciones.

## 1. Resumen

La Plataforma de Monitoreo de Órdenes de Compra es una aplicación de escritorio para Windows, conectada a SQL Server, que da seguimiento a cada orden de compra de punta a punta: creación con aprobación, estatus e historial, alertas automáticas y analítica de cumplimiento y riesgo del abastecimiento.

## 2. Contexto y problema

Supuestos de partida, por confirmar con el equipo de compras:

- Las órdenes se siguen con poca visibilidad: no es evidente cuáles están por vencer, estancadas o con incidencias, y los retrasos se descubren tarde.
- No hay una medición sistemática del cumplimiento y los tiempos de entrega de cada proveedor, así que las decisiones de abastecimiento se toman sin datos.
- Falta control sobre quién crea, aprueba y modifica una orden, y no queda un historial confiable.

## 3. Visión y objetivos

**Visión:** que el equipo de compras vea en una sola pantalla qué está pasando con cada orden, reciba aviso antes de que algo se retrase y use datos para mejorar el abastecimiento.

Objetivos:

1. Visibilidad: conocer el estado actual de cada orden y su historial.
2. Anticipación: detectar retrasos y riesgos con alertas y un puntaje de riesgo que explique sus motivos.
3. Mejora continua: medir cumplimiento, tiempos de entrega e incidencias por proveedor, y encontrar cuellos de botella del proceso.
4. Control y trazabilidad: roles, aprobación obligatoria y registro de cada cambio.
5. Operación confiable: respaldos, instalación repetible y datos protegidos.

### Métricas de éxito (metas propuestas)

- Órdenes vencidas sin atención: reducirlas respecto a la línea base que se mida al iniciar el piloto.
- Detección: toda orden vencida debe haber generado una alerta antes (con un máximo de una hora de retraso).
- Adopción: al menos 90 % de los cambios de estatus registrados en el sistema.
- Cobertura analítica: cumplimiento y tiempo de entrega visibles para todos los proveedores con al menos una entrega.
- Rendimiento: dashboard y lista de órdenes en menos de 3 segundos con hasta 5,000 órdenes.
- Recuperación: respaldo diario verificado y restauración probada en menos de 4 horas.

## 4. Usuarios y roles

- **Administrador.** Configura usuarios y el catálogo de productos por proveedor; ve todo; puede aprobar. Necesita control total y orden en el sistema.
- **Comprador.** Crea y da seguimiento a sus propias órdenes y las envía a aprobación. Necesita saber qué falta de cada pedido sin ver los de otras personas.
- **Supervisor.** Aprueba o devuelve órdenes pendientes y monitorea todas. Necesita priorizar dónde intervenir.
- **Almacén.** Registra la recepción de materiales (hoy mediante el cambio de estatus de entrega). Necesita una forma rápida de confirmar lo recibido.
- **Consulta.** Solo lectura de órdenes, alertas e indicadores.

## 5. Alcance

**Dentro del producto:** inicio de sesión con roles; administración de usuarios; catálogo de productos por proveedor; alta de órdenes con aprobación; seguimiento de estatus con historial; alertas automáticas; dashboard; indicadores y riesgo; respaldos y scripts de instalación.

**Fuera de alcance por ahora:** aplicación móvil o web; integración con ERP o contabilidad; facturación y conciliación detallada; notificaciones por correo o mensajería; varias empresas en la misma base; firma electrónica; modelo predictivo entrenado (faltan datos).

## 6. Flujo de negocio y reglas

Flujo normal de una orden:

**Borrador → Pendiente de Aprobación → Enviada → Confirmada → En Proceso → En Tránsito → Recibida Parcial o Completa → Cerrada.**

- **Cancelada:** desde Borrador, Pendiente de Aprobación, Enviada, Confirmada o En Proceso. Es un estatus final, igual que Cerrada.
- **En Disputa:** desde Enviada hasta Recibida Completa. Al resolverse, la orden regresa al punto operativo que corresponda o se cancela.
- Toda orden pasa por aprobación antes de enviarse al proveedor. Solo el Supervisor o el Administrador aprueban; el Comprador puede devolverla a Borrador.
- El Comprador solo ve y modifica las órdenes que él creó.
- Almacén puede hacer los cambios del flujo de entrega, pero no aprueba ni toca Borrador o Pendiente de Aprobación.
- Los cambios válidos y los roles que pueden hacerlos se definen en tablas, no en el código, y la base de datos los valida.
- Cada cambio queda en el historial con fecha, usuario y comentario.
- La numeración es automática (OC-AAAA-NNNN) y se reinicia cada año.
- Cada proveedor solo puede ofrecer los productos que tiene asignados, con su precio pactado.
- Una orden se guarda completa o no se guarda: cabecera, líneas e historial en una sola operación.

## 7. Requisitos funcionales

Prioridad: Must (indispensable), Should (importante), Could (deseable). Estado: Listo, Parcial o Pendiente, según lo construido hasta octubre de 2026; las pruebas formales de aceptación siguen pendientes.

### A. Acceso y seguridad

- RF-01 Inicio de sesión con usuario y contraseña. Must · Listo.
- RF-02 Contraseñas guardadas con PBKDF2 y sal, con migración automática de los hashes antiguos. Must · Listo.
- RF-03 Cambio de la propia contraseña con política mínima (8 caracteres, letra y número). Must · Listo.
- RF-04 Cerrar sesión sin cerrar la aplicación. Must · Listo.
- RF-05 Bloqueo temporal tras intentos fallidos. Should · Pendiente.
- RF-06 Recuperación de contraseña por autoservicio (requiere correo). Could · Pendiente.

### B. Usuarios y permisos

- RF-07 Administración de usuarios: alta, edición, activar o desactivar y restablecer contraseña; siempre queda un Administrador activo. Must · Listo.
- RF-08 Cinco roles con permisos por cambio de estatus validados en la base de datos. Must · Listo.
- RF-09 El Comprador ve solo sus órdenes, KPIs y alertas. Must · Listo.
- RF-10 Bitácora de acciones administrativas (quién cambió qué y cuándo). Should · Pendiente.

### C. Órdenes de compra

- RF-11 Crear una orden con proveedor, fecha requerida, prioridad, observaciones y líneas de producto, con número automático y guardado transaccional. Must · Listo.
- RF-12 Cada proveedor ofrece solo sus productos, con precio pactado y días de entrega; se administran desde una pantalla. Must · Listo.
- RF-13 Lista de órdenes con búsqueda por número. Must · Listo.
- RF-14 Detalle de la orden con información general e historial de estatus. Must · Listo.
- RF-15 Cambio de estatus con flujo válido, permisos por rol y comentario. Must · Listo.
- RF-16 Flujo de aprobación: enviar a aprobación, aprobar y devolver. Must · Listo.
- RF-17 Filtros por estatus, proveedor, prioridad y fechas en la lista. Should · Parcial (la base de datos ya los soporta; falta la pantalla).
- RF-18 Paginación para listas grandes. Should · Pendiente.
- RF-19 Editar una orden mientras esté en Borrador. Should · Pendiente.
- RF-20 Exportar órdenes e indicadores a Excel o PDF. Should · Pendiente.

### D. Recepción e incidencias

- RF-21 Registrar y resolver incidencias de una orden (tipo, descripción y resolución). Must · Pendiente (las tablas y las alertas existen; falta la pantalla).
- RF-22 Registrar recepciones parciales y completas con cantidades por línea. Should · Pendiente (hoy la recepción se refleja solo con el cambio de estatus).

### E. Alertas

- RF-23 Generación automática, cada hora, de alertas por vencer, vencida e incidencia abierta. Must · Listo.
- RF-24 Generación automática de las alertas ya definidas en el catálogo: sin confirmación, recepción incompleta y alta prioridad sin movimiento. Should · Pendiente.
- RF-25 Pantalla de alertas y marcar como leída. Must · Listo.
- RF-26 Aviso por correo electrónico. Could · Pendiente.

### F. Dashboard, indicadores y analítica

- RF-27 Dashboard con KPIs: total de órdenes, completadas, vencidas, activas, en disputa, alta prioridad, monto comprometido, cumplimiento, alertas e incidencias. Must · Listo.
- RF-28 Indicadores por proveedor: cumplimiento de entregas, tiempo promedio de entrega, retraso promedio, órdenes vencidas e incidencias, con selector de periodo. Must · Listo.
- RF-29 Riesgo de órdenes abiertas con puntaje de 0 a 100, nivel y motivos explicados. Must · Listo.
- RF-30 Tiempo promedio en cada estatus para encontrar cuellos de botella. Should · Listo.
- RF-31 Excluir las órdenes canceladas de los KPIs de vencidas y de monto comprometido. Should · Pendiente.
- RF-32 Recomendaciones en lenguaje natural generadas con un modelo de lenguaje a partir de los indicadores. Could · Pendiente.
- RF-33 Modelo predictivo de retrasos. Won't por ahora; solo cuando haya datos suficientes (orientativo: 200 o más órdenes entregadas).

### G. Catálogo

- RF-34 Alta y edición de proveedores, productos y categorías desde la aplicación (hoy se hace con SQL). Should · Pendiente.
- RF-35 Manejo de varias monedas en KPIs y montos (hoy se suman sin convertir). Should · Pendiente.

### H. Operación

- RF-36 Respaldo completo diario automático con verificación y limpieza. Must · Listo.
- RF-37 Configuración separada por equipo y por entorno (Desarrollo o Producción). Must · Listo.
- RF-38 Scripts de base de datos y guía para instalar en otra laptop. Must · Listo.

## 8. Requisitos no funcionales

- **Seguridad.** Contraseñas con hash y sal; permisos validados en la base de datos; consultas parametrizadas; sin claves en el repositorio; certificado válido en producción. Limitación conocida: la aplicación se conecta directo a la base, así que quien tenga acceso a ella puede saltarse la aplicación.
- **Rendimiento.** Pantallas principales en menos de 3 segundos con hasta 5,000 órdenes. Pendiente: que la ventana no se congele durante las consultas (async/await) y la paginación.
- **Disponibilidad y recuperación.** Respaldo diario (pérdida máxima de datos objetivo: 24 horas) y restauración en menos de 4 horas. La copia debe guardarse fuera del equipo del servidor.
- **Usabilidad.** Interfaz en español, mensajes claros y diseño que se adapte a distintas resoluciones y escalas de Windows (pendiente).
- **Mantenibilidad.** Scripts de base de datos versionados, README y repositorio en GitHub. Pendiente: pruebas automatizadas.
- **Compatibilidad.** Windows 10 y 11, .NET Framework 4.8 y SQL Server 2016 SP1 o superior.
- **Auditoría.** Historial de cambios de estatus (listo); bitácora administrativa (pendiente).
- **Privacidad.** Se guardan solo los datos necesarios de las personas usuarias; los respaldos contienen datos reales y deben protegerse.

## 9. Arquitectura y datos (resumen)

- Cliente de escritorio en Windows Forms, organizado en capas: pantallas, acceso a datos, modelos y utilidades.
- SQL Server con tablas, vistas y procedimientos almacenados. La aplicación opera a través de procedimientos que validan permisos y reglas.
- Trabajos de SQL Server Agent para alertas (cada hora) y respaldos (diario). En SQL Server Express se programan con el Programador de tareas de Windows.
- Entidades principales: usuarios y roles; proveedores, productos y categorías; relación proveedor-producto; órdenes y sus líneas; estatus, cambios válidos y permisos por rol; historial; alertas y sus tipos; incidencias y sus tipos; recepciones.
- Decisión: cliente conectado directo a la base (dos capas) por simplicidad. Si crecen los usuarios o se usa fuera de la red local, conviene una capa intermedia (API).

## 10. Roadmap

- **Fase 0 (hecha):** base de datos, seguridad, roles, órdenes con aprobación, alertas, dashboard, indicadores y riesgo, administración, respaldos e instalación repetible.
- **Fase 1 (cierre del MVP):** incidencias, que la ventana no se congele, paginación y filtros, corrección de canceladas en KPIs, pruebas por rol, prueba de restauración y piloto con datos reales.
- **Fase 2 (operación completa):** recepciones por línea, alertas faltantes, exportación, edición de borradores, bitácora administrativa, alta de proveedores y productos, bloqueo por intentos fallidos y manejo de monedas.
- **Fase 3 (analítica avanzada):** recomendaciones con IA sobre los indicadores, avisos por correo, API intermedia si se necesita y modelo predictivo cuando haya datos suficientes.

## 11. Riesgos y supuestos

Supuestos: pocos usuarios internos en una red local; los datos actuales son de ejemplo; SQL Server Developer es solo para desarrollo y pruebas, y producción requiere Standard con licencia o Express.

Riesgos y cómo atenderlos:

- Pocos datos para analítica: tratar los indicadores como orientativos hasta tener más órdenes entregadas y avisarlo en pantalla (ya se hace).
- Baja adopción si se siguen moviendo órdenes fuera del sistema: capacitación y piloto con seguimiento semanal.
- Un solo equipo concentra servidor y respaldos: copiar los respaldos a otro disco o a la nube.
- Acceso directo a la base de datos: limitar permisos por usuario de SQL y evaluar una API intermedia.
- Clave de un servicio de IA dentro de una aplicación de escritorio: usar un servicio intermedio en lugar de guardarla en cada equipo.
- SQL Server Express no incluye el Agent: usar el Programador de tareas para alertas y respaldos.

## 12. Preguntas abiertas

1. ¿Cuántos usuarios y cuántas órdenes por mes se esperan en producción?
2. ¿Quién es la persona responsable del producto y de aprobar cambios?
3. ¿La aprobación debe aplicar a todas las órdenes o solo a las de cierto monto?
4. ¿Se necesitan avisos por correo? ¿A quién?
5. ¿Se trabaja con una sola moneda o con varias?
6. ¿Dónde se instalará el servidor de producción y quién lo administra?
7. ¿Hace falta registrar recepciones por línea o basta el cambio de estatus?
8. ¿Cuáles son los proveedores, productos y precios reales que se cargarán?
