using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;

namespace MonitoreoOrdenes.Data
{
    /// <summary>
    /// Clase de utilidad para manejo centralizado de la conexión a SQL Server.
    /// La cadena de conexión se lee desde App.config.
    /// </summary>
    public static class DatabaseHelper
    {
        // Lee la conexión desde App.config → connectionStrings → "MonitoreoDB"
        private static readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["MonitoreoDB"].ConnectionString;

        /// <summary>
        /// Devuelve una nueva conexión abierta. El llamador es responsable de cerrarla (using).
        /// </summary>
        public static SqlConnection GetConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Ejecuta un stored procedure y devuelve un DataTable con los resultados.
        /// </summary>
        public static DataTable ExecuteProcedure(string procedureName, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(procedureName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                using (var adapter = new SqlDataAdapter(cmd))
                    adapter.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// Ejecuta un stored procedure que no devuelve resultados (INSERT/UPDATE/DELETE).
        /// Retorna el número de filas afectadas.
        /// </summary>
        public static int ExecuteNonQuery(string procedureName, SqlParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(procedureName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 30;

                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Ejecuta una consulta SQL directa y devuelve un DataTable.
        /// Solo usar para consultas de lectura simples.
        /// </summary>
        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.CommandTimeout = 30;

                if (parameters != null)
                    cmd.Parameters.AddRange(parameters);

                using (var adapter = new SqlDataAdapter(cmd))
                    adapter.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// Verifica que la conexión a la base de datos funciona correctamente.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                    return conn.State == ConnectionState.Open;
            }
            catch
            {
                return false;
            }
        }
    }
}
