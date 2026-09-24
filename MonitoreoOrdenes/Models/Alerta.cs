using System;

namespace MonitoreoOrdenes.Models
{
    public class Alerta
    {
        public int AlertaId { get; set; }
        public int OrdenId { get; set; }
        public string NumeroOrden { get; set; }
        public string TipoAlerta { get; set; }
        public string Mensaje { get; set; }
        public DateTime FechaGenerada { get; set; }
        public bool Leida { get; set; }
    }
}
