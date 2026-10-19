using System.Data;
using System.Data.SqlClient;

namespace MonitoreoOrdenes.Data
{
    /// <summary>Acceso a datos del catálogo de proveedores y los productos que vende cada uno.</summary>
    public class CatalogoRepository
    {
        public DataTable ObtenerProveedoresActivos()
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT proveedor_id, codigo, nombre FROM Proveedores WHERE activo = 1 ORDER BY nombre");
        }

        /// <summary>Todos los productos activos, con una columna "vende" para este proveedor.</summary>
        public DataTable ObtenerProductosDeProveedor(int proveedorId)
        {
            return DatabaseHelper.ExecuteProcedure("sp_ObtenerProveedorProductos",
                new[] { new SqlParameter("@proveedor_id", proveedorId) });
        }

        /// <summary>
        /// Guarda los productos del proveedor. "items" debe traer, en este orden:
        /// producto_id (int), vende (bool), precio_pactado (decimal), tiempo_entrega_dias (int).
        /// </summary>
        public void GuardarProductosDeProveedor(int adminId, int proveedorId, DataTable items)
        {
            DatabaseHelper.ExecuteNonQuery("sp_GuardarProveedorProductos", new[]
            {
                new SqlParameter("@admin_id",     adminId),
                new SqlParameter("@proveedor_id", proveedorId),
                new SqlParameter("@items", SqlDbType.Structured)
                {
                    TypeName = "dbo.TipoProveedorProducto",
                    Value    = items
                }
            });
        }
    }
}
