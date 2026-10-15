using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Data
{
    public class OrdenCompraRepository
    {
        /// <summary>
        /// Obtiene órdenes de compra con filtros opcionales vía sp_ObtenerOrdenesCompra.
        /// </summary>
        public List<OrdenCompra> ObtenerOrdenes(
            int? estatusId    = null,
            int? proveedorId  = null,
            int? prioridad    = null,
            DateTime? desde   = null,
            DateTime? hasta   = null,
            string numeroOrden = null)
        {
            var parameters = new[]
            {
                new SqlParameter("@estatus_id",   (object)estatusId   ?? DBNull.Value),
                new SqlParameter("@proveedor_id", (object)proveedorId ?? DBNull.Value),
                new SqlParameter("@prioridad",    (object)prioridad   ?? DBNull.Value),
                new SqlParameter("@fecha_desde",  (object)desde       ?? DBNull.Value),
                new SqlParameter("@fecha_hasta",  (object)hasta       ?? DBNull.Value),
                new SqlParameter("@numero_orden", (object)numeroOrden ?? DBNull.Value)
            };

            var dt = DatabaseHelper.ExecuteProcedure("sp_ObtenerOrdenesCompra", parameters);
            return MapearOrdenes(dt);
        }

        /// <summary>
        /// Obtiene los KPIs para el dashboard principal.
        /// </summary>
        public KpiDashboard ObtenerKPIs()
        {
            var dt = DatabaseHelper.ExecuteQuery("SELECT * FROM vw_KPIs_Dashboard");

            if (dt.Rows.Count == 0)
                return new KpiDashboard();

            var row = dt.Rows[0];
            return new KpiDashboard
            {
                TotalOrdenes              = Convert.ToInt32(row["total_ordenes"]),
                OrdenesCompletadas        = Convert.ToInt32(row["ordenes_completadas"]),
                OrdenesVencidas           = Convert.ToInt32(row["ordenes_vencidas"]),
                OrdenesActivas            = Convert.ToInt32(row["ordenes_activas"]),
                OrdenesEnDisputa          = Convert.ToInt32(row["ordenes_en_disputa"]),
                OrdenesAltaPrioridad      = Convert.ToInt32(row["ordenes_alta_prioridad"]),
                MontoTotalComprometido    = Convert.ToDecimal(row["monto_total_comprometido"]),
                PorcentajeCumplimiento    = row["porcentaje_cumplimiento"] == DBNull.Value
                                            ? 0 : Convert.ToDecimal(row["porcentaje_cumplimiento"]),
                AlertasPendientes         = Convert.ToInt32(row["alertas_pendientes"]),
                IncidenciasAbiertas       = Convert.ToInt32(row["incidencias_abiertas"])
            };
        }

        /// <summary>
        /// Obtiene alertas no leídas del sistema.
        /// </summary>
        public List<Alerta> ObtenerAlertas(bool soloNoLeidas = true)
        {
            string sql = @"
                SELECT a.alerta_id, a.orden_id, oc.numero_orden,
                       ta.nombre AS tipo_alerta, a.mensaje,
                       a.fecha_generada, a.leida
                FROM Alertas a
                INNER JOIN OrdenesCompra oc ON a.orden_id = oc.orden_id
                INNER JOIN TiposAlerta ta   ON a.tipo_alerta_id = ta.tipo_alerta_id
                WHERE (@soloNoLeidas = 0 OR a.leida = 0)
                ORDER BY a.fecha_generada DESC";

            var parameters = new[]
            {
                new SqlParameter("@soloNoLeidas", soloNoLeidas ? 1 : 0)
            };

            var dt = DatabaseHelper.ExecuteQuery(sql, parameters);
            var lista = new List<Alerta>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new Alerta
                {
                    AlertaId      = Convert.ToInt32(row["alerta_id"]),
                    OrdenId       = Convert.ToInt32(row["orden_id"]),
                    NumeroOrden   = row["numero_orden"].ToString(),
                    TipoAlerta    = row["tipo_alerta"].ToString(),
                    Mensaje       = row["mensaje"].ToString(),
                    FechaGenerada = Convert.ToDateTime(row["fecha_generada"]),
                    Leida         = Convert.ToBoolean(row["leida"])
                });
            }
            return lista;
        }

        /// <summary>
        /// Marca una alerta como leída.
        /// </summary>
        public void MarcarAlertaLeida(int alertaId)
        {
            string sql = "UPDATE Alertas SET leida = 1, fecha_leida = GETDATE() WHERE alerta_id = @id";
            DatabaseHelper.ExecuteNonQueryText(sql, new[] { new SqlParameter("@id", alertaId) });
        }

        /// <summary>
        /// Cambia el estatus de una orden.
        /// </summary>
        public void CambiarEstatus(int ordenId, int nuevoEstatusId, int usuarioId, string comentario)
        {
            var parameters = new[]
            {
                new SqlParameter("@orden_id",         ordenId),
                new SqlParameter("@nuevo_estatus_id", nuevoEstatusId),
                new SqlParameter("@usuario_id",       usuarioId),
                new SqlParameter("@comentario",       (object)comentario ?? DBNull.Value)
            };
            DatabaseHelper.ExecuteNonQuery("sp_CambiarEstatusOrden", parameters);
        }

        /// <summary>
        /// Obtiene el historial de estatus de una orden.
        /// </summary>
        public DataTable ObtenerHistorial(int ordenId)
        {
            string sql = @"
                SELECT
                    h.fecha_cambio,
                    ea.nombre AS estatus_anterior,
                    en.nombre AS estatus_nuevo,
                    u.nombre + ' ' + u.apellido AS usuario,
                    h.comentario
                FROM HistorialEstatusOrden h
                LEFT  JOIN EstatusOrden ea ON h.estatus_ant_id    = ea.estatus_id
                INNER JOIN EstatusOrden en ON h.estatus_nuevo_id  = en.estatus_id
                INNER JOIN Usuarios u      ON h.usuario_id        = u.usuario_id
                WHERE h.orden_id = @orden_id
                ORDER BY h.fecha_cambio DESC";

            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@orden_id", ordenId) });
        }

        // DBNull.Value no es null, así que "?." no sirve para columnas NULL de SQL.
        private static string TextoONull(object valor)
        {
            return valor == null || valor == DBNull.Value ? null : valor.ToString();
        }

        // -------------------------------------------------------
        // Mapeo interno DataTable → List<OrdenCompra>
        // -------------------------------------------------------
        private List<OrdenCompra> MapearOrdenes(DataTable dt)
        {
            var lista = new List<OrdenCompra>();
            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new OrdenCompra
                {
                    OrdenId              = Convert.ToInt32(row["orden_id"]),
                    NumeroOrden          = row["numero_orden"].ToString(),
                    FechaEmision         = Convert.ToDateTime(row["fecha_emision"]),
                    FechaRequerida       = Convert.ToDateTime(row["fecha_requerida"]),
                    FechaEntregaReal     = row["fecha_entrega_real"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["fecha_entrega_real"]),
                    MontoTotal           = Convert.ToDecimal(row["monto_total"]),
                    Moneda               = row["moneda"].ToString(),
                    Prioridad            = Convert.ToInt32(row["prioridad"]),
                    PrioridadTexto       = row["prioridad_texto"].ToString(),
                    Observaciones        = TextoONull(row["observaciones"]),
                    ProveedorId          = Convert.ToInt32(row["proveedor_id"]),
                    ProveedorCodigo      = row["proveedor_codigo"].ToString(),
                    ProveedorNombre      = row["proveedor_nombre"].ToString(),
                    ProveedorCalificacion= row["proveedor_calificacion"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(row["proveedor_calificacion"]),
                    EstatusId            = Convert.ToInt32(row["estatus_id"]),
                    EstatusNombre        = row["estatus_nombre"].ToString(),
                    EstatusColor         = TextoONull(row["estatus_color"]),
                    CreadorId            = Convert.ToInt32(row["creador_id"]),
                    CreadorNombre        = row["creador_nombre"].ToString(),
                    DiasTranscurridos    = Convert.ToInt32(row["dias_transcurridos"]),
                    DiasParaVencer       = Convert.ToInt32(row["dias_para_vencer"]),
                    Cumplimiento         = row["cumplimiento"].ToString(),
                    IncidenciasAbiertas  = Convert.ToInt32(row["incidencias_abiertas"]),
                    AlertasNoLeidas      = Convert.ToInt32(row["alertas_no_leidas"])
                });
            }
            return lista;
        }
    }
}
