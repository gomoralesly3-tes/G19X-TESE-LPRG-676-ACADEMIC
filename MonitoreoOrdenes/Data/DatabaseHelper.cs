using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace MonitoreoOrdenes.Data
{
    /// <summary>
    /// Clase de utilidad para manejo centralizado de la conexión a SQL Server.
    /// La cadena de conexión se lee desde App.config.
    /// </summary>
    public static class DatabaseHelper
    {
        private const string ConnectionName = "MonitoreoDB";
        private const int CommandTimeoutSeconds = 30;

        // Se lee de forma diferida: si falta en App.config el error ocurre al usarla
        // (dentro del try/catch de quien llama) y no al cargar la clase, que haría
        // caer la aplicación con TypeInitializationException sin mensaje útil.
        private static string ConnectionString
        {
            get
            {
                var settings = ConfigurationManager.ConnectionStrings[ConnectionName];
                if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
                    throw new ConfigurationErrorsException(
                        $"No se encontró la cadena de conexión '{ConnectionName}' en App.config.");
                return settings.ConnectionString;
            }
        }

        /// <summary>
        /// Devuelve una nueva conexión abierta. El llamador es responsable de cerrarla (using).
        /// </summary>
        public static SqlConnection GetConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private static SqlCommand CrearComando(SqlConnection conn, string texto,
                                               CommandType tipo, SqlParameter[] parameters)
        {
            var cmd = new SqlCommand(texto, conn)
            {
                CommandType    = tipo,
                CommandTimeout = CommandTimeoutSeconds
            };
            if (parameters != null)
                cmd.Parameters.AddRange(parameters);
            return cmd;
        }

        private static DataTable Llenar(string texto, CommandType tipo, SqlParameter[] parameters)
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            using (var cmd = CrearComando(conn, texto, tipo, parameters))
            using (var adapter = new SqlDataAdapter(cmd))
                adapter.Fill(dt);
            return dt;
        }

        /// <summary>
        /// Ejecuta un stored procedure y devuelve un DataTable con los resultados.
        /// </summary>
        public static DataTable ExecuteProcedure(string procedureName, SqlParameter[] parameters = null)
        {
            return Llenar(procedureName, CommandType.StoredProcedure, parameters);
        }

        /// <summary>
        /// Ejecuta un stored procedure que no devuelve resultados (INSERT/UPDATE/DELETE).
        /// Retorna el número de filas afectadas.
        /// </summary>
        public static int ExecuteNonQuery(string procedureName, SqlParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = CrearComando(conn, procedureName, CommandType.StoredProcedure, parameters))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Ejecuta una consulta SQL directa y devuelve un DataTable.
        /// Solo usar para consultas de lectura simples.
        /// </summary>
        public static DataTable ExecuteQuery(string sql, SqlParameter[] parameters = null)
        {
            return Llenar(sql, CommandType.Text, parameters);
        }

        /// <summary>
        /// Ejecuta una sentencia SQL directa que no devuelve resultados (UPDATE/INSERT/DELETE).
        /// Retorna el número de filas afectadas.
        /// </summary>
        public static int ExecuteNonQueryText(string sql, SqlParameter[] parameters = null)
        {
            using (var conn = GetConnection())
            using (var cmd = CrearComando(conn, sql, CommandType.Text, parameters))
                return cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Verifica que la conexión a la base de datos funciona correctamente.
        /// </summary>
        public static bool TestConnection()
        {
            string error;
            return TestConnection(out error);
        }

        /// <summary>
        /// Igual que TestConnection(), pero devuelve el motivo del fallo.
        /// </summary>
        public static bool TestConnection(out string error)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    error = null;
                    return conn.State == ConnectionState.Open;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("TestConnection falló: " + ex);
                error = ex.Message;
                return false;
            }
        }
    }
}
