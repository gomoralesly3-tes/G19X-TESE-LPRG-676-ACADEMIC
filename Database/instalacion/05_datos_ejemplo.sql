-- ============================================================
-- 05_datos_ejemplo.sql  -  OPCIONAL: proveedores y productos de EJEMPLO
--
-- Sirve para probar la aplicacion en una base nueva sin tener que restaurar un
-- respaldo (Opcion B del README). NO es informacion real: son datos ficticios
-- de demostracion. En una base de produccion no se ejecuta.
--
-- Carga: 5 proveedores, 10 productos y que productos vende cada proveedor
-- (con su precio pactado igual al precio de referencia).
-- No crea usuarios ni ordenes: los usuarios se crean con el primer
-- administrador (README, paso 2.4) y desde la pantalla Usuarios.
--
-- Ejecutar DESPUES de 01 a 04. Se puede repetir sin duplicar nada.
-- ============================================================
USE [MonitoreoOrdenesCompra]
GO

-- ── Proveedores ficticios ───────────────────────────────────
SET IDENTITY_INSERT dbo.Proveedores ON;
INSERT INTO dbo.Proveedores (proveedor_id, codigo, nombre)
SELECT v.id, v.codigo, v.nombre
FROM (VALUES
    (1, 'PROV-001', 'TechSupply S.A. de C.V.'),
    (2, 'PROV-002', 'Distribuidora Norte S.A.'),
    (3, 'PROV-003', 'Papelería Unión S.R.L.'),
    (4, 'PROV-004', 'Soluciones IT México S.A.'),
    (5, 'PROV-005', 'Muebles y Más S.A. de C.V.')
) AS v(id, codigo, nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Proveedores p WHERE p.proveedor_id = v.id OR p.codigo = v.codigo);
SET IDENTITY_INSERT dbo.Proveedores OFF;
GO

-- ── Productos ficticios (categorias de 04_datos_base.sql) ───
--   2 Equipos de Computo · 3 Papeleria y Oficina · 5 Mobiliario · 6 Software y Licencias · 7 Consumibles
SET IDENTITY_INSERT dbo.Productos ON;
INSERT INTO dbo.Productos (producto_id, codigo, nombre, unidad_medida, categoria_id, precio_ref)
SELECT v.id, v.codigo, v.nombre, v.unidad, v.categoria, v.precio
FROM (VALUES
    ( 1, 'PRD-001', 'Laptop Dell Inspiron 15',          'Pieza',    2, 18500.00),
    ( 2, 'PRD-002', 'Monitor LED 24" Full HD',          'Pieza',    2,  4200.00),
    ( 3, 'PRD-003', 'Teclado y Mouse Inalámbrico',      'Paquete',  2,   850.00),
    ( 4, 'PRD-004', 'Resma Papel Bond T/Carta',         'Resma',    3,   105.00),
    ( 5, 'PRD-005', 'Folder Manila Letter',             'Paquete',  3,    95.00),
    ( 6, 'PRD-006', 'Licencia Microsoft 365 Business',  'Licencia', 6,  3200.00),
    ( 7, 'PRD-007', 'Silla Ejecutiva Ergonómica',       'Pieza',    5,  4800.00),
    ( 8, 'PRD-008', 'Escritorio en L de Madera',        'Pieza',    5,  7500.00),
    ( 9, 'PRD-009', 'Tóner HP LaserJet',                'Pieza',    7,   890.00),
    (10, 'PRD-010', 'Switch de Red 24 puertos',         'Pieza',    2,  5600.00)
) AS v(id, codigo, nombre, unidad, categoria, precio)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Productos p WHERE p.producto_id = v.id OR p.codigo = v.codigo);
SET IDENTITY_INSERT dbo.Productos OFF;
GO

-- ── Que productos vende cada proveedor (precio pactado = precio de referencia) ──
--   1 TechSupply           : Laptop, Monitor, Teclado y Mouse, Switch
--   2 Distribuidora Norte  : Teclado y Mouse, Resma, Folder, Toner
--   3 Papeleria Union      : Resma, Folder, Toner
--   4 Soluciones IT Mexico : Laptop, Licencia Microsoft 365, Switch
--   5 Muebles y Mas        : Silla, Escritorio
INSERT INTO dbo.ProveedorProductos (proveedor_id, producto_id, precio_pactado)
SELECT v.prov, v.prod, ISNULL(p.precio_ref, 0)
FROM (VALUES
    (1,1),(1,2),(1,3),(1,10),
    (2,3),(2,4),(2,5),(2,9),
    (3,4),(3,5),(3,9),
    (4,1),(4,6),(4,10),
    (5,7),(5,8)
) AS v(prov, prod)
INNER JOIN dbo.Proveedores pr ON pr.proveedor_id = v.prov
INNER JOIN dbo.Productos   p  ON p.producto_id   = v.prod
WHERE NOT EXISTS (SELECT 1 FROM dbo.ProveedorProductos x
                  WHERE x.proveedor_id = v.prov AND x.producto_id = v.prod);
GO
