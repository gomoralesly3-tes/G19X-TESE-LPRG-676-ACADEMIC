namespace MonitoreoOrdenes.Models
{
    public class KpiDashboard
    {
        public int TotalOrdenes { get; set; }
        public int OrdenesCompletadas { get; set; }
        public int OrdenesVencidas { get; set; }
        public int OrdenesActivas { get; set; }
        public int OrdenesEnDisputa { get; set; }
        public int OrdenesAltaPrioridad { get; set; }
        public decimal MontoTotalComprometido { get; set; }
        public decimal PorcentajeCumplimiento { get; set; }
        public int AlertasPendientes { get; set; }
        public int IncidenciasAbiertas { get; set; }
    }
}
