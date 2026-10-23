# Declaración de uso de inteligencia artificial

**Herramienta:** Claude (modelo de lenguaje de Anthropic), usado mediante conversación.
**Proyecto:** Plataforma de Monitoreo de Órdenes de Compra.

## Qué se hizo con ayuda de la IA

- Diseño y escritura de los scripts de la base de datos: esquema, vistas, procedimientos almacenados,
  migraciones y trabajos programados de alertas y respaldos.
- Código en C# de la capa de datos y de las pantallas: inicio de sesión seguro, cambio de contraseña,
  administración de usuarios, productos por proveedor, nueva orden, detalle con incidencias, indicadores
  y riesgo de órdenes.
- Detección y corrección de errores, y revisiones de seguridad del código existente.
- Redacción de la documentación: README, guías, PRD y MVP.

## Qué hizo la persona autora

- Definió el objetivo del sistema y los requisitos, por ejemplo los indicadores de cumplimiento y tiempos
  de entrega y la analítica de riesgos del abastecimiento.
- Tomó las decisiones de negocio cuando se le consultaron: qué órdenes ve cada rol, los permisos de cada rol
  y que todas las órdenes pasen por aprobación del Supervisor.
- Proporcionó los datos del catálogo (proveedores, productos, categorías, tipos de alerta y de incidencia).
- Instaló, ejecutó los scripts, compiló y probó la aplicación en su equipo, y reportó los errores para
  que se corrigieran.

## Alcance y límites

- El código y los documentos generados con IA se probaron manualmente en un equipo de desarrollo. No hay
  pruebas automatizadas.
- La IA no tuvo acceso al equipo ni a la base de datos de la persona autora: trabajó con los archivos,
  mensajes de error y resultados que ella compartió.
- La persona autora decide qué se incluye en el repositorio y es responsable de su contenido.

## Cómo se indica en el repositorio

Los commits que incluyen código o documentos generados con ayuda de IA lo señalan en el cuerpo del mensaje:
«Desarrollado con asistencia de Claude (Anthropic)».
