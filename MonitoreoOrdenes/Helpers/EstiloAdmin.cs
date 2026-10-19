using System.Drawing;
using System.Windows.Forms;

namespace MonitoreoOrdenes.Helpers
{
    /// <summary>
    /// Controles con el estilo visual de las pantallas de administración,
    /// para no repetir el mismo código en cada formulario.
    /// </summary>
    public static class EstiloAdmin
    {
        public static readonly Color Azul  = Color.FromArgb(15, 52, 96);
        public static readonly Color Gris  = Color.FromArgb(100, 116, 139);
        public static readonly Color Borde = Color.FromArgb(226, 232, 240);
        public static readonly Color Fondo = Color.FromArgb(245, 247, 250);
        public static readonly Color Verde = Color.FromArgb(16, 185, 129);
        public static readonly Color Rojo  = Color.FromArgb(239, 68, 68);

        public static Panel Encabezado(string titulo)
        {
            var panel = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Azul };
            panel.Controls.Add(new Label
            {
                Text      = titulo,
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(20, 14),
                AutoSize  = true
            });
            return panel;
        }

        public static Label Etiqueta(string texto, int x, int y)
        {
            return new Label
            {
                Text      = texto,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Gris,
                Location  = new Point(x, y),
                AutoSize  = true
            };
        }

        public static TextBox Caja(int x, int y, int ancho, bool esClave = false)
        {
            return new TextBox
            {
                Location              = new Point(x, y),
                Size                  = new Size(ancho, 26),
                Font                  = new Font("Segoe UI", 10),
                BorderStyle           = BorderStyle.FixedSingle,
                UseSystemPasswordChar = esClave
            };
        }

        public static Button Boton(string texto, int x, int y, int ancho, int alto, Color fondo)
        {
            var btn = new Button
            {
                Text      = texto,
                Location  = new Point(x, y),
                Size      = new Size(ancho, alto),
                BackColor = fondo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public static Button BotonSecundario(string texto, int x, int y, int ancho, int alto)
        {
            var btn = new Button
            {
                Text      = texto,
                Location  = new Point(x, y),
                Size      = new Size(ancho, alto),
                BackColor = Fondo,
                ForeColor = Gris,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = Borde;
            return btn;
        }

        public static void EstiloGrid(DataGridView g)
        {
            g.BackgroundColor       = Color.White;
            g.BorderStyle           = BorderStyle.None;
            g.AllowUserToAddRows    = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.RowHeadersVisible     = false;
            g.SelectionMode         = DataGridViewSelectionMode.FullRowSelect;
            g.Font                  = new Font("Segoe UI", 9);
            g.GridColor             = Borde;
            g.RowTemplate.Height    = 34;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Azul,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding   = new Padding(4)
            };
            g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            g.DefaultCellStyle.SelectionForeColor = Color.Black;
        }
    }
}
