using System;
using System.Data;
using System.Diagnostics;
using System.Data.SqlClient;
using MonitoreoOrdenes.Helpers;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Data
{
    public class UsuarioRepository
    {
        /// <summary>
        /// Valida usuario y contraseña. Retorna el usuario autenticado o null si falla.
        /// La contraseña se verifica aqui (PBKDF2); si el hash guardado esta en formato
        /// antiguo (SHA-256), se actualiza automaticamente al formato nuevo.
        /// </summary>
        public Usuario Login(string username, string password)
        {
            var dt = DatabaseHelper.ExecuteProcedure("sp_ObtenerCredenciales",
                new[] { new SqlParameter("@username", username) });

            if (dt.Rows.Count == 0)
            {
                SecurityHelper.SimularVerificacion(password);
                return null;
            }

            var row = dt.Rows[0];
            string almacenado = row["password_hash"] == DBNull.Value
                ? null : row["password_hash"].ToString();

            var resultado = SecurityHelper.Verificar(password, almacenado);
            if (resultado == SecurityHelper.ResultadoVerificacion.Incorrecta)
                return null;

            int usuarioId = (int)row["usuario_id"];

            // Estas dos operaciones no deben impedir el acceso si fallan.
            if (resultado == SecurityHelper.ResultadoVerificacion.CorrectaRequiereActualizar)
            {
                try
                {
                    DatabaseHelper.ExecuteNonQuery("sp_ActualizarPasswordHash", new[]
                    {
                        new SqlParameter("@usuario_id",    usuarioId),
                        new SqlParameter("@password_hash", SecurityHelper.HashPassword(password))
                    });
                }
                catch (Exception ex)
                {
                    Trace.TraceError("No se pudo actualizar el hash del usuario " + usuarioId + ": " + ex);
                }
            }

            try
            {
                DatabaseHelper.ExecuteNonQuery("sp_RegistrarAcceso",
                    new[] { new SqlParameter("@usuario_id", usuarioId) });
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo registrar el acceso: " + ex);
            }

            return new Usuario
            {
                UsuarioId = usuarioId,
                Nombre    = row["nombre"].ToString(),
                Apellido  = row["apellido"].ToString(),
                Email     = row["email"].ToString(),
                Username  = row["username"].ToString(),
                RolId     = (int)row["rol_id"],
                RolNombre = row["rol_nombre"].ToString(),
                Activo    = (bool)row["activo"]
            };
        }

        // ── Cambio de contraseña propia ─────────────────────────

        /// <summary>
        /// Cambia la contraseña del propio usuario. Verifica primero la actual.
        /// Devuelve false si la contraseña actual no es correcta.
        /// </summary>
        public bool CambiarPassword(string username, int usuarioId, string passwordActual, string passwordNueva)
        {
            var dt = DatabaseHelper.ExecuteProcedure("sp_ObtenerCredenciales",
                new[] { new SqlParameter("@username", username) });

            if (dt.Rows.Count == 0) return false;

            var fila = dt.Rows[0];
            string almacenado = fila["password_hash"] == DBNull.Value ? null : fila["password_hash"].ToString();

            if (Convert.ToInt32(fila["usuario_id"]) != usuarioId ||
                SecurityHelper.Verificar(passwordActual, almacenado) == SecurityHelper.ResultadoVerificacion.Incorrecta)
                return false;

            DatabaseHelper.ExecuteNonQuery("sp_ActualizarPasswordHash", new[]
            {
                new SqlParameter("@usuario_id",    usuarioId),
                new SqlParameter("@password_hash", SecurityHelper.HashPassword(passwordNueva))
            });
            return true;
        }

        // ── Administracion de usuarios (solo rol Administrador) ──

        public DataTable ObtenerRoles()
        {
            return DatabaseHelper.ExecuteQuery("SELECT rol_id, nombre FROM Roles ORDER BY rol_id");
        }

        public DataTable ListarUsuarios(int adminId)
        {
            return DatabaseHelper.ExecuteProcedure("sp_ListarUsuarios",
                new[] { new SqlParameter("@admin_id", adminId) });
        }

        /// <summary>
        /// Crea (usuarioId = null) o edita un usuario. "passwordNueva" solo se usa al crear.
        /// Devuelve el id del usuario guardado.
        /// </summary>
        public int GuardarUsuario(int adminId, int? usuarioId, string nombre, string apellido,
                                  string email, string username, int rolId, bool activo,
                                  string passwordNueva)
        {
            string hash = usuarioId == null && passwordNueva != null
                ? SecurityHelper.HashPassword(passwordNueva)
                : null;

            var dt = DatabaseHelper.ExecuteProcedure("sp_GuardarUsuario", new[]
            {
                new SqlParameter("@admin_id",      adminId),
                new SqlParameter("@usuario_id",    usuarioId.HasValue ? (object)usuarioId.Value : DBNull.Value),
                new SqlParameter("@nombre",        nombre),
                new SqlParameter("@apellido",      apellido),
                new SqlParameter("@email",         email),
                new SqlParameter("@username",      username),
                new SqlParameter("@rol_id",        rolId),
                new SqlParameter("@activo",        activo),
                new SqlParameter("@password_hash", hash != null ? (object)hash : DBNull.Value)
            });
            return Convert.ToInt32(dt.Rows[0]["usuario_id"]);
        }

        /// <summary>El administrador define una contraseña nueva para otro usuario.</summary>
        public void RestablecerPassword(int adminId, int usuarioId, string passwordNueva)
        {
            DatabaseHelper.ExecuteNonQuery("sp_AdminRestablecerPassword", new[]
            {
                new SqlParameter("@admin_id",      adminId),
                new SqlParameter("@usuario_id",    usuarioId),
                new SqlParameter("@password_hash", SecurityHelper.HashPassword(passwordNueva))
            });
        }
    }
}
