using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MonitoreoOrdenes.Helpers
{
    /// <summary>
    /// Controles personalizados reutilizables con diseño moderno.
    /// </summary>

    // ── Botón redondeado ────────────────────────────────────────
    public class RoundedButton : Button
    {
        public int CornerRadius { get; set; } = 8;
        public Color HoverColor { get; set; } = UITheme.PrimarioHover;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = UITheme.FuenteNegrita;
            ForeColor = UITheme.TextoBlanco;
            BackColor = UITheme.Primario;
        }

        protected override void OnMouseEnter(System.EventArgs e)
        {
            base.OnMouseEnter(e);
            BackColor = HoverColor;
            Invalidate();
        }

        protected override void OnMouseLeave(System.EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = UITheme.Primario;
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var path = RoundedRect(ClientRectangle, CornerRadius))
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            var textRect = ClientRectangle;
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ── TextBox moderno con borde de color ──────────────────────
    public class ModernTextBox : Panel
    {
        public TextBox InnerTextBox { get; private set; }
        private bool _focused = false;

        public new string Text
        {
            get => InnerTextBox.Text;
            set => InnerTextBox.Text = value;
        }

        public bool UseSystemPasswordChar
        {
            get => InnerTextBox.UseSystemPasswordChar;
            set => InnerTextBox.UseSystemPasswordChar = value;
        }

        public ModernTextBox()
        {
            Height = 42;
            BackColor = UITheme.FondoTarjeta;
            Padding = new Padding(1);

            InnerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font        = new Font("Segoe UI", 10),
                ForeColor   = UITheme.TextoPrimario,
                BackColor   = UITheme.FondoTarjeta,
                Dock        = DockStyle.Fill
            };

            InnerTextBox.Enter += (s, e) => { _focused = true;  Invalidate(); };
            InnerTextBox.Leave += (s, e) => { _focused = false; Invalidate(); };

            Controls.Add(InnerTextBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var color = _focused ? UITheme.Acento : UITheme.Borde;
            using (var pen = new Pen(color, _focused ? 2 : 1))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }

    // ── Tarjeta KPI con sombra y barra de color ─────────────────
    public class KpiCard : Panel
    {
        private Label _lblValor;
        private Label _lblTitulo;
        private Label _lblIcono;
        private Panel _barraColor;
        private Color _colorAccento;

        public string Titulo  { set => _lblTitulo.Text = value; }
        public string Valor   { set => _lblValor.Text  = value; }
        public string Icono   { set => _lblIcono.Text  = value; }

        public KpiCard(string icono, string titulo, string valor, Color colorAccento)
        {
            _colorAccento = colorAccento;
            Size      = new Size(170, 100);
            BackColor = UITheme.FondoTarjeta;

            // Barra de color izquierda
            _barraColor = new Panel
            {
                Width     = 5,
                Dock      = DockStyle.Left,
                BackColor = colorAccento
            };

            _lblIcono = new Label
            {
                Text      = icono,
                Font      = new Font("Segoe UI", 18),
                Location  = new Point(16, 12),
                Size      = new Size(36, 36),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblValor = new Label
            {
                Text      = valor,
                Font      = UITheme.FuenteKPI,
                ForeColor = colorAccento,
                Location  = new Point(55, 8),
                Size      = new Size(105, 45),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _lblTitulo = new Label
            {
                Text      = titulo,
                Font      = UITheme.FuentePequena,
                ForeColor = UITheme.TextoSecundario,
                Location  = new Point(55, 55),
                Size      = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            Controls.AddRange(new Control[] { _barraColor, _lblIcono, _lblValor, _lblTitulo });
        }
    }
}
