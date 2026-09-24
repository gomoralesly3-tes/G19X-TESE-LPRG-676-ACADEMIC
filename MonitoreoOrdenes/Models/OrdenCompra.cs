using System;

namespace MonitoreoOrdenes.Models
{
    public class OrdenCompra
    {
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaRequerida { get; set; }
        public DateTime? FechaEntregaReal { get; set; }
        public decimal MontoTotal { get; set; }
        public string Moneda { get; set; }
        public int Prioridad { get; set; }
        public string PrioridadTexto { get; set; }
        public string Observaciones { get; set; }

        // Proveedor
        public int ProveedorId { get; set; }
        public string ProveedorCodigo { get; set; }
        public string ProveedorNombre { get; set; }
        public decimal? ProveedorCalificacion { get; set; }

        // Estatus
        public int EstatusId { get; set; }
        public string EstatusNombre { get; set; }
        public string EstatusColor { get; set; }

        // Creador
        public int CreadorId { get; set; }
        public string CreadorNombre { get; set; }

        // Indicadores
        public int DiasTranscurridos { get; set; }
        public int DiasParaVencer { get; set; }
        public string Cumplimiento { get; set; }
        public int IncidenciasAbiertas { get; set; }
        public int AlertasNoLeidas { get; set; }
    }
}
