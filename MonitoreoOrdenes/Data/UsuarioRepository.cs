using System.Data.SqlClient;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Data
{
    public class UsuarioRepository
    {
        /// <summary>
        /// Valida credenciales contra el SP sp_Login.
        /// Retorna el usuario autenticado o null si falla.
        /// </summary>
        public Usuario Login(string username, string passwordHash)
        {
            var parameters = new[]
            {
                new SqlParameter("@username",      username),
                new SqlParameter("@password_hash", passwordHash)
            };

            var dt = DatabaseHelper.ExecuteProcedure("sp_Login", parameters);

            if (dt.Rows.Count == 0)
                return null;

            var row = dt.Rows[0];
            return new Usuario
            {
                UsuarioId  = (int)row["usuario_id"],
                Nombre     = row["nombre"].ToString(),
                Apellido   = row["apellido"].ToString(),
                Email      = row["email"].ToString(),
                Username   = row["username"].ToString(),
                RolId      = (int)row["rol_id"],
                RolNombre  = row["rol_nombre"].ToString(),
                Activo     = (bool)row["activo"]
            };
        }
    }
}
