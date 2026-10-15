using System;
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
    }
}
