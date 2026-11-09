using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MonitoreoOrdenes.Helpers
{
    /// <summary>
    /// Logo de MOC (Monitoreo Órdenes Compra) dibujado con GDI+.
    /// Es vectorial: se ve nítido a cualquier tamaño o escala de pantalla y no
    /// necesita archivos de imagen ni recursos en el proyecto.
    ///
    /// Isotipo: anillo exterior (monitoreo) + "C" con flecha (flujo de la orden)
    /// + punto focal central. Colores de la marca: turquesa #00ADB5 / #007882,
    /// azul marino #0F3460 y gris pizarra #64748B.
    /// </summary>
    public class LogoMoc : Control
    {
        public enum TipoLogo { Horizontal, Vertical, SoloIsotipo }

        private static readonly Color Turquesa      = Color.FromArgb(0, 173, 181);
        private static readonly Color TurquesaOscuro = Color.FromArgb(0, 120, 130);
        private static readonly Color Marino        = Color.FromArgb(15, 52, 96);
        private static readonly Color Pizarra       = Color.FromArgb(100, 116, 139);

        private TipoLogo _tipo = TipoLogo.Horizontal;
        private bool _fondoOscuro;
        private bool _mostrarSubtitulo;

        /// <summary>Horizontal (isotipo + texto), Vertical (isotipo arriba, texto abajo) o solo el isotipo.</summary>
        [DefaultValue(TipoLogo.Horizontal)]
        public TipoLogo Tipo
        {
            get { return _tipo; }
            set { _tipo = value; Invalidate(); }
        }

        /// <summary>True cuando el logo va sobre un fondo oscuro: texto blanco y anillo translúcido.</summary>
        [DefaultValue(false)]
        public bool FondoOscuro
        {
            get { return _fondoOscuro; }
            set { _fondoOscuro = value; Invalidate(); }
        }

        /// <summary>Muestra «Monitoreo Órdenes Compra» bajo las siglas (sólo si hay espacio suficiente).</summary>
        [DefaultValue(false)]
        public bool MostrarSubtitulo
        {
            get { return _mostrarSubtitulo; }
            set { _mostrarSubtitulo = value; Invalidate(); }
        }

        public LogoMoc()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            TabStop   = false;
            Size      = new Size(200, 60);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.PixelOffsetMode   = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            float w = ClientSize.Width;
            float h = ClientSize.Height;
            if (w < 8 || h < 8) return;

            Color colorTexto = _fondoOscuro ? Color.White : Marino;
            Color colorSub   = _fondoOscuro ? Color.FromArgb(175, 255, 255, 255) : Pizarra;

            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.FormatFlags |= StringFormatFlags.NoWrap;

                switch (_tipo)
                {
                    case TipoLogo.SoloIsotipo:
                    {
                        float lado = Math.Min(w, h);
                        DibujarIsotipo(g, (w - lado) / 2f, (h - lado) / 2f, lado);
                        break;
                    }

                    case TipoLogo.Vertical:
                    {
                        bool sub    = _mostrarSubtitulo;
                        float lado  = Math.Min(w * 0.55f, h * (sub ? 0.58f : 0.66f));
                        float tTit  = lado * 0.32f;
                        float tSub  = lado * 0.12f;
                        float gap   = lado * 0.08f;

                        using (var fTit = new Font("Segoe UI", tTit, FontStyle.Bold, GraphicsUnit.Pixel))
                        using (var fSub = new Font("Segoe UI", tSub, FontStyle.Regular, GraphicsUnit.Pixel))
                        {
                            float hTit = fTit.GetHeight(g);
                            float hSub = sub ? fSub.GetHeight(g) : 0f;
                            float total = lado + gap + hTit + hSub;
                            float y = (h - total) / 2f;

                            DibujarIsotipo(g, (w - lado) / 2f, y, lado);
                            y += lado + gap;

                            sf.Alignment = StringAlignment.Center;
                            using (var br = new SolidBrush(colorTexto))
                                g.DrawString("MOC", fTit, br, new RectangleF(0, y, w, hTit), sf);

                            if (sub)
                                using (var br = new SolidBrush(colorSub))
                                    g.DrawString("Monitoreo Órdenes Compra", fSub, br,
                                        new RectangleF(0, y + hTit, w, hSub), sf);
                        }
                        break;
                    }

                    default: // Horizontal
                    {
                        bool sub   = _mostrarSubtitulo && h >= 56f;
                        float lado = h;
                        float tTit = lado * (sub ? 0.30f : 0.42f);
                        float tSub = lado * 0.125f;
                        float x    = lado + lado * 0.10f;

                        using (var fTit = new Font("Segoe UI", tTit, FontStyle.Bold, GraphicsUnit.Pixel))
                        using (var fSub = new Font("Segoe UI", tSub, FontStyle.Regular, GraphicsUnit.Pixel))
                        {
                            float hTit  = fTit.GetHeight(g);
                            float hSub  = sub ? fSub.GetHeight(g) : 0f;
                            float y     = (h - (hTit + hSub)) / 2f;

                            DibujarIsotipo(g, 0f, 0f, lado);

                            sf.Alignment = StringAlignment.Near;
                            using (var br = new SolidBrush(colorTexto))
                                g.DrawString("MOC", fTit, br, new PointF(x, y), sf);

                            if (sub)
                                using (var br = new SolidBrush(colorSub))
                                    g.DrawString("Monitoreo Órdenes Compra", fSub, br,
                                        new PointF(x, y + hTit), sf);
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Dibuja el isotipo dentro de un cuadrado de «lado» px. El dibujo está
        /// definido en una cuadrícula de 120 x 120 y se escala al tamaño pedido.
        /// </summary>
        private void DibujarIsotipo(Graphics g, float x, float y, float lado)
        {
            var estado = g.Save();
            g.TranslateTransform(x, y);
            g.ScaleTransform(lado / 120f, lado / 120f);

            // 1) Anillo exterior (monitoreo continuo)
            Color colorAnillo = _fondoOscuro
                ? Color.FromArgb(70, 255, 255, 255)
                : Color.FromArgb(226, 232, 240);
            using (var pen = new Pen(colorAnillo, 8f))
                g.DrawEllipse(pen, 20f, 20f, 80f, 80f);

            using (var degradado = new LinearGradientBrush(
                new RectangleF(20f, 20f, 80f, 80f), Turquesa, TurquesaOscuro, 45f))
            {
                // 2) "C" abierta hacia la derecha: de arriba, por la izquierda, hasta abajo (280°)
                using (var pen = new Pen(degradado, 10f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap   = LineCap.Flat;
                    g.DrawArc(pen, 35f, 35f, 50f, 50f, 320f, -280f);
                }

                // 3) Punta de flecha al final de la "C" (el flujo de la orden)
                const float radio = 25f;
                double ang = 40.0 * Math.PI / 180.0;
                float cos = (float)Math.Cos(ang);
                float sin = (float)Math.Sin(ang);

                var fin = new PointF(60f + radio * cos, 60f + radio * sin);  // extremo del arco
                var tan = new PointF(sin, -cos);                             // dirección del recorrido
                var rad = new PointF(cos, sin);                              // hacia afuera del centro

                var punta = new[]
                {
                    new PointF(fin.X + rad.X * 10.5f, fin.Y + rad.Y * 10.5f),
                    new PointF(fin.X + tan.X * 15f,   fin.Y + tan.Y * 15f),
                    new PointF(fin.X - rad.X * 10.5f, fin.Y - rad.Y * 10.5f)
                };
                g.FillPolygon(degradado, punta);

                // 4) Punto focal (alerta / ojo)
                g.FillEllipse(degradado, 50f, 50f, 20f, 20f);
            }

            g.Restore(estado);
        }
    }
}
