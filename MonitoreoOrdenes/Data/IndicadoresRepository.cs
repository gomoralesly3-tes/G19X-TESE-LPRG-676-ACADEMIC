using System;
using System.Data;
using System.Data.SqlClient;
using MonitoreoOrdenes.Forms;

namespace MonitoreoOrdenes.Data
{
    /// <summary>
    /// Consultas de indicadores. La base de datos recibe el usuario que consulta para que un
    /// Comprador (rol 2) vea solo las cifras de sus propias órdenes.
    /// </summary>
    public class IndicadoresRepository
    {
        private static object UsuarioActualId
        {
            get
            {
                var usuario = FrmLogin.UsuarioActual;
                return usuario == null ? (object)DBNull.Value : usuario.UsuarioId;
            }
        }

        /// <summary>Cumplimiento y tiempos por proveedor. dias = null: todo el historial.</summary>
        public DataTable ObtenerIndicadoresProveedores(int? dias)
        {
            return DatabaseHelper.ExecuteProcedure("sp_IndicadoresProveedores", new[]
            {
                new SqlParameter("@usuario_id", UsuarioActualId),
                new SqlParameter("@dias",       dias.HasValue ? (object)dias.Value : DBNull.Value)
            });
        }

        /// <summary>Órdenes abiertas con puntaje de riesgo y los motivos.</summary>
        public DataTable ObtenerRiesgoOrdenes()
        {
            return DatabaseHelper.ExecuteProcedure("sp_RiesgoOrdenesAbiertas",
                new[] { new SqlParameter("@usuario_id", UsuarioActualId) });
        }

        /// <summary>Días promedio que las órdenes pasan en cada estatus.</summary>
        public DataTable ObtenerTiempoPorEstatus()
        {
            return DatabaseHelper.ExecuteProcedure("sp_TiempoPorEstatus",
                new[] { new SqlParameter("@usuario_id", UsuarioActualId) });
        }
    }
}
