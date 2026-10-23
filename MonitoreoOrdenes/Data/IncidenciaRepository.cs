using System;
using System.Data;
using System.Data.SqlClient;
using MonitoreoOrdenes.Forms;

namespace MonitoreoOrdenes.Data
{
    /// <summary>
    /// Incidencias de una orden de compra. La base de datos valida permisos y reglas
    /// (quién puede registrar o resolver, y que el Comprador solo toque sus órdenes).
    /// </summary>
    public class IncidenciaRepository
    {
        private static int UsuarioId
        {
            get
            {
                var usuario = FrmLogin.UsuarioActual;
                if (usuario == null) throw new InvalidOperationException("No hay una sesión iniciada.");
                return usuario.UsuarioId;
            }
        }

        public DataTable ObtenerIncidencias(int ordenId)
        {
            return DatabaseHelper.ExecuteProcedure("sp_ObtenerIncidencias", new[]
            {
                new SqlParameter("@orden_id",   ordenId),
                new SqlParameter("@usuario_id", UsuarioId)
            });
        }

        public DataTable ObtenerTipos()
        {
            return DatabaseHelper.ExecuteQuery(
                "SELECT tipo_id, nombre, gravedad FROM TiposIncidencia ORDER BY gravedad, nombre");
        }

        /// <summary>Registra una incidencia y devuelve su id.</summary>
        public int Registrar(int ordenId, int tipoId, string descripcion)
        {
            var dt = DatabaseHelper.ExecuteProcedure("sp_RegistrarIncidencia", new[]
            {
                new SqlParameter("@usuario_id",  UsuarioId),
                new SqlParameter("@orden_id",    ordenId),
                new SqlParameter("@tipo_id",     tipoId),
                new SqlParameter("@descripcion", descripcion)
            });
            return Convert.ToInt32(dt.Rows[0]["incidencia_id"]);
        }

        public void Resolver(int incidenciaId, string resolucion)
        {
            DatabaseHelper.ExecuteNonQuery("sp_ResolverIncidencia", new[]
            {
                new SqlParameter("@usuario_id",    UsuarioId),
                new SqlParameter("@incidencia_id", incidenciaId),
                new SqlParameter("@resolucion",    resolucion)
            });
        }
    }
}
