# MVP — Plataforma de Monitoreo de Órdenes de Compra

**PluriOne S.A. de C.V. · Versión 1.0 · Octubre 2026 · Complementa al PRD**

Este documento define la versión mínima que se pondrá a prueba con personas usuarias reales: qué incluye, qué no, cómo sabremos si funciona y qué falta cerrar antes del piloto. Las metas numéricas son propuestas para validar; no son mediciones.

## 1. Hipótesis del MVP

Si el equipo de compras tiene una sola herramienta que muestra el estado de cada orden, avisa de vencimientos y riesgos y mide el cumplimiento de los proveedores, detectará los retrasos antes y reducirá las órdenes vencidas sin atención.

Lo que queremos aprender con el piloto:

- ¿Las alertas y el puntaje de riesgo señalan lo que de verdad importa, o generan ruido?
- ¿El equipo mueve sus órdenes dentro del sistema (adopción) o sigue usándolas por otros medios?
- ¿Los indicadores por proveedor sirven para decidir a quién comprar?
- ¿El flujo de aprobación agrega control sin frenar demasiado?

## 2. Quiénes participan en el piloto

Un Supervisor, dos Compradores, una persona de Almacén y el Administrador. Consulta es opcional. Duración sugerida: 3 a 4 semanas con órdenes reales.

## 3. Alcance del MVP

### Incluido y ya construido

- Inicio de sesión con roles, cambio de contraseña y administración de usuarios.
- Catálogo de productos por proveedor con precio pactado.
- Alta de órdenes con numeración automática y guardado completo o nada.
- Flujo de aprobación: el Comprador envía y el Supervisor o el Administrador aprueban o devuelven.
- Seguimiento de estatus con reglas por rol e historial.
- El Comprador ve solo sus órdenes.
- Alertas automáticas cada hora y pantalla de alertas.
- Dashboard con KPIs.
- Indicadores por proveedor, riesgo explicado de órdenes abiertas y tiempo por estatus.
- Respaldos diarios automáticos e instalación repetible en otra laptop.

### Debe cerrarse antes del piloto (brechas)

1. Registrar y resolver incidencias desde la aplicación. Hoy las alertas y los indicadores las usan, pero no hay pantalla para crearlas.
2. Que la ventana no se congele cuando la base tarda en responder, más paginación y filtros básicos en la lista de órdenes.
3. Corregir que las órdenes canceladas cuenten como vencidas y como monto comprometido.
4. Probar la restauración de un respaldo y copiar los respaldos fuera del equipo.
5. Cargar proveedores, productos y precios reales, y probar cada rol con una matriz de pruebas.
6. Recomendable, no bloqueante: generar las alertas que ya están definidas pero no se crean solas (sin confirmación, recepción incompleta y alta prioridad).

### Fuera del MVP

Recepciones por línea; exportación a Excel o PDF; edición de órdenes en Borrador; avisos por correo; bitácora administrativa; alta de proveedores y productos desde la aplicación; manejo de monedas; recomendaciones con IA; modelo predictivo; aplicación móvil o web; integración con otros sistemas.

## 4. Recorrido mínimo de extremo a extremo

1. El Comprador crea una orden con productos de su proveedor (queda en Borrador).
2. La envía a aprobación.
3. El Supervisor la revisa y la aprueba (pasa a Enviada) o la devuelve con un comentario.
4. El Comprador registra la confirmación del proveedor y el avance (Confirmada, En Proceso, En Tránsito).
5. Almacén registra la recepción, parcial o completa.
6. Si hay un problema, se registra una incidencia y la orden puede pasar a En Disputa.
7. El sistema genera alertas y calcula el riesgo de cada orden abierta; el Supervisor revisa los Indicadores.
8. La orden se cierra y alimenta el cumplimiento del proveedor.

## 5. Criterios de aceptación

- Aprobación: una orden en Borrador no puede pasar a Enviada sin pasar por Pendiente de Aprobación, y el Comprador no puede aprobarla.
- Aislamiento: un Comprador no ve, en la lista, los KPIs ni las alertas, las órdenes de otro.
- Integridad: si falla el guardado de una orden, no queda ninguna orden a medias.
- Historial: cada cambio de estatus aparece con fecha, usuario y comentario.
- Alertas: toda orden vencida tiene su alerta en un máximo de una hora.
- Riesgo: cada orden abierta muestra puntaje, nivel y motivos legibles.
- Catálogo: al crear una orden solo se ofrecen los productos del proveedor elegido, y el servidor rechaza los demás.
- Usuarios: no se puede dejar al sistema sin un Administrador activo.
- Respaldo: existe un archivo verificado del día anterior y se restauró con éxito al menos una vez en otro equipo.
- Instalación: una persona sigue el README y deja el sistema funcionando en una laptop nueva sin ayuda.

## 6. Métricas del piloto y decisión Go / No-Go (metas propuestas)

- Adopción: al menos 90 % de las órdenes del periodo se crean y avanzan en el sistema.
- Calidad de datos: al menos 95 % de las órdenes sin correcciones manuales en la base de datos.
- Alertas útiles: en una revisión semanal con el Supervisor, al menos 7 de cada 10 alertas se consideran útiles.
- Detección: ninguna orden vencida sin una alerta previa.
- Estabilidad: cero pérdidas de datos, respaldo exitoso todos los días y ninguna pantalla bloqueada más de 10 segundos.
- Satisfacción: promedio de 4 sobre 5 en una encuesta breve al final.

**Go:** se cumplen adopción y estabilidad, y el equipo identifica al menos dos mejoras concretas de abastecimiento gracias a los indicadores.

**Ajustar y repetir:** se cumple la estabilidad pero no la adopción; se revisan la capacitación y el flujo.

**No-Go:** pérdida de datos o rechazo claro por parte de las personas usuarias clave.

## 7. Plan sugerido (estimación)

- Semana 1: cerrar las brechas 1 a 5.
- Semana 2: instalar en el equipo del piloto, cargar datos reales y capacitar una hora a cada rol.
- Semanas 3 y 4: piloto con revisión semanal de 30 minutos.
- Semana 5: retrospectiva, decisión Go / No-Go y priorización de la Fase 2.

## 8. Riesgos del MVP

- Pocos datos al inicio: los indicadores serán orientativos, y así se comunica en pantalla.
- Las personas siguen moviendo órdenes fuera del sistema: acordar que durante el piloto el sistema es la fuente oficial.
- Dependencia de un solo equipo: guardar una copia de los respaldos fuera de él.
- Alertas ruidosas: ajustar los umbrales del riesgo y de las alertas con lo aprendido.
- Aprobación demasiado lenta: medir el tiempo en Pendiente de Aprobación y revisar si hace falta un umbral por monto.

## 9. Después del MVP (prioridad MoSCoW)

- Should: recepciones por línea, exportación, edición de borradores, bitácora administrativa, alta de proveedores y productos, manejo de monedas, bloqueo por intentos fallidos.
- Could: avisos por correo, recomendaciones con IA sobre los indicadores, recuperación de contraseña por autoservicio.
- Won't por ahora: modelo predictivo de retrasos (faltan datos), aplicación móvil o web, integración con ERP.

## 10. Lista de verificación antes del piloto

- Brechas 1 a 5 cerradas.
- Matriz de pruebas por rol completa.
- Respaldo restaurado en otra computadora.
- README probado en una laptop nueva.
- Usuarios reales creados y capacitados.
- Proveedores, productos y precios reales cargados.
- Responsable del piloto y fecha de revisión definidos.
