# Bitácora de Instalación Limpia

* **Fecha de ejecución:** 28 y 29 de octubre de 2026
* **Commit de referencia:** 20aa0af
* **Entorno de prueba:** Windows 11 / .NET Framework 4.8 / Visual Studio 2022


| Paso | Qué hice | Resultado | Tiempo |
|---|---|---|---|
| 1 | git clone en C:\prueba-limpia | Bien | 1 min |
| 2 | Cambie el nombre de la base de datos;01 a 05 en SSMS; consulta de conteo | 18 / 3 / 20 | 10 min |
| 3 | Recompilar solución | Bien; se creó conexion.config pero se modifico el nombre de la base de datos ya que no conectaba | 5 min |
| 4 | Crear admin y entrar | Bien | 5 min |
| 5 | Orden → aprobación → En Tránsito → incidencia | Bien | 8 min |
| 6 | Indicadores | Sin datos: faltaban ordenes para cada proveedor  | 2 min |
| 7 | Se creo 06_ordenes_ejemplo.sql en base nueva | Proveedores: 5 filas · Riesgo: 6 filas · Tiempo por estatus: 4 filas; Hallazgos: «Se cargaron 13 órdenes de prueba con fechas relativas, permitiendo evaluar el rendimiento de los 5 proveedores, detectar 6 órdenes abiertas en riesgo y calcular los tiempos de tránsito y estatus sin dejar las pantallas del dashboard vacías | 10 min |
