using System.Drawing;

namespace MonitoreoOrdenes.Helpers
{
    /// <summary>
    /// Paleta de colores y estilos centralizados para toda la aplicación.
    /// Cambiar aquí afecta toda la UI.
    /// </summary>
    public static class UITheme
    {
        // ── Colores principales ──────────────────────────────────
        public static readonly Color Primario        = Color.FromArgb(15, 52, 96);   // Azul marino
        public static readonly Color PrimarioClaro   = Color.FromArgb(25, 75, 135);  // Azul medio
        public static readonly Color PrimarioHover   = Color.FromArgb(35, 95, 160);  // Azul hover
        public static readonly Color Acento          = Color.FromArgb(0, 173, 181);  // Cyan
        public static readonly Color AcentoClaro     = Color.FromArgb(170, 233, 238);

        // ── Fondos ──────────────────────────────────────────────
        public static readonly Color FondoPagina     = Color.FromArgb(240, 244, 248);
        public static readonly Color FondoTarjeta    = Color.White;
        public static readonly Color FondoMenu       = Color.FromArgb(10, 37, 64);
        public static readonly Color FondoMenuActivo = Color.FromArgb(0, 173, 181);
        public static readonly Color FondoHeader     = Color.FromArgb(15, 52, 96);

        // ── Textos ──────────────────────────────────────────────
        public static readonly Color TextoPrimario   = Color.FromArgb(30, 40, 55);
        public static readonly Color TextoSecundario = Color.FromArgb(100, 116, 139);
        public static readonly Color TextoBlanco     = Color.White;
        public static readonly Color TextoMuted      = Color.FromArgb(148, 163, 184);

        // ── Semáforo de estados ──────────────────────────────────
        public static readonly Color Exito          = Color.FromArgb(16, 185, 129);  // Verde
        public static readonly Color Advertencia    = Color.FromArgb(245, 158, 11);  // Amarillo
        public static readonly Color Peligro        = Color.FromArgb(239, 68, 68);   // Rojo
        public static readonly Color Info           = Color.FromArgb(59, 130, 246);  // Azul
        public static readonly Color Neutro         = Color.FromArgb(100, 116, 139); // Gris

        // ── Bordes ──────────────────────────────────────────────
        public static readonly Color Borde          = Color.FromArgb(226, 232, 240);
        public static readonly Color BordeFocus     = Color.FromArgb(0, 173, 181);

        // ── Tipografías ─────────────────────────────────────────
        public static readonly Font FuenteTitulo    = new Font("Segoe UI", 20, FontStyle.Bold);
        public static readonly Font FuenteSubtitulo = new Font("Segoe UI", 13, FontStyle.Bold);
        public static readonly Font FuenteNormal    = new Font("Segoe UI", 9);
        public static readonly Font FuenteNegrita   = new Font("Segoe UI", 9, FontStyle.Bold);
        public static readonly Font FuenteKPI       = new Font("Segoe UI", 26, FontStyle.Bold);
        public static readonly Font FuenteMenu      = new Font("Segoe UI", 10);
        public static readonly Font FuentePequena   = new Font("Segoe UI", 8);

        // ── Colores por prioridad ────────────────────────────────
        public static Color ColorPrioridad(int prioridad)
        {
            if (prioridad == 1) return Peligro;
            if (prioridad == 2) return Advertencia;
            return Exito;
        }

        // ── Colores por cumplimiento ─────────────────────────────
        public static Color ColorCumplimiento(string cumplimiento)
        {
            if (cumplimiento == "Vencida")          return Color.FromArgb(254, 226, 226);
            if (cumplimiento == "Próxima a Vencer") return Color.FromArgb(254, 243, 199);
            if (cumplimiento == "A Tiempo")         return Color.FromArgb(209, 250, 229);
            if (cumplimiento == "Retrasada")        return Color.FromArgb(254, 226, 226);
            return Color.White;
        }
    }
}
