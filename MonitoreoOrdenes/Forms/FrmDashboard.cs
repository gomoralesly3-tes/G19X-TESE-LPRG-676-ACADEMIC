using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Forms
{
    public class FrmDashboard : Form
    {
        private readonly OrdenCompraRepository _repo = new OrdenCompraRepository();

        // Layout
        private Panel panelHeader, panelMenu, panelContent;

        // Menú
        private Button btnDashboard, btnOrdenes, btnAlertas,
                       btnNuevaOrden, btnSalir,
                       btnCatalogo, btnUsuarios, btnPassword,
                       btnIndicadores;
        private Button _btnActivo;

        // KPI Cards (paneles simples)
        private Panel cardTotal, cardActivas, cardVencidas,
                      cardCumplimiento, cardAlertas, cardDisputa;

        // Contenido
        private Label    lblSeccion, lblSubtitulo;
        private Panel    panelFiltros, panelKpis;
        private TextBox  txtBusqueda;
        private Button   btnBuscar, btnLimpiar;
        private DataGridView dgvOrdenes;
        private Label    lblContador;
        private Timer    timerRefresh;

        // Vista actual (en vez de comparar el texto de una etiqueta)
        private enum Vista { Dashboard, Ordenes, Alertas }
        private Vista _vista = Vista.Dashboard;
        private bool  _cerrandoSesion;

        public FrmDashboard()
        {
            InitializeComponent();
            CargarDashboard();
            IniciarTimer();
        }

        // ═══════════════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════════════
        private void InitializeComponent()
        {
            this.Text            = "Monitoreo OC — PluriOne S.A. de C.V." + EntornoApp.Sufijo;
            this.Size            = new Size(1300, 800);
            this.MinimumSize     = new Size(1100, 680);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = Color.FromArgb(245, 247, 250);
            this.FormClosing    += FrmDashboard_FormClosing;

            ConstruirHeader();
            ConstruirMenu();
            ConstruirContenido();
        }

        private void FrmDashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            timerRefresh?.Stop();
            if (!_cerrandoSesion)
                Application.Exit();
        }

        // ═══════════════════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════════════════
        private void ConstruirHeader()
        {
            panelHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 55,
                BackColor = Color.FromArgb(15, 52, 96)
            };

            var lblTitulo = new Label
            {
                Text      = "Plataforma de Monitoreo de Órdenes de Compra",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(230, 16),
                AutoSize  = true
            };

            var lblUsuario = new Label
            {
                Name      = "lblUsuario",
                Text      = $"  {FrmLogin.UsuarioActual?.NombreCompleto}  |  {FrmLogin.UsuarioActual?.RolNombre}",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(170, 233, 238),
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };

            var lblFecha = new Label
            {
                Text      = DateTime.Now.ToString("ddd dd/MMM/yyyy",
                            new System.Globalization.CultureInfo("es-MX")).ToUpper(),
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };

            panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblUsuario, lblFecha });
            panelHeader.Resize += (s, e) =>
            {
                lblUsuario.Location = new Point(panelHeader.Width - lblUsuario.Width - 20, 10);
                lblFecha.Location   = new Point(panelHeader.Width - lblFecha.Width - 20, 33);
            };

            this.Controls.Add(panelHeader);
        }

        // ═══════════════════════════════════════════════════════
        // MENÚ LATERAL
        // ═══════════════════════════════════════════════════════
        private void ConstruirMenu()
        {
            panelMenu = new Panel
            {
                Width     = 200,
                Dock      = DockStyle.Left,
                BackColor = Color.FromArgb(10, 37, 64)
            };

            // Logo
            var panelLogo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 55,
                BackColor = Color.FromArgb(5, 25, 50)
            };
            panelLogo.Controls.Add(new Label
            {
                Text      = "PLURIONE",
                Font      = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 173, 181),
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            });
            panelMenu.Controls.Add(panelLogo);

            // Separador
            panelMenu.Controls.Add(new Label
            {
                Height    = 1,
                Dock      = DockStyle.Top,
                BackColor = Color.FromArgb(30, 255, 255, 255)
            });

            // Etiqueta
            panelMenu.Controls.Add(new Label
            {
                Text      = "  NAVEGACIÓN",
                Font      = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 148, 163, 184),
                Dock      = DockStyle.Top,
                Height    = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(12, 0, 0, 0)
            });

            // Solo mostrar "Nueva Orden" si es Admin o Comprador
            bool puedeCrear = FrmLogin.UsuarioActual?.RolId <= 2;

            btnDashboard  = CrearBtnMenu("  Dashboard");
            btnOrdenes    = CrearBtnMenu("  Ordenes");
            btnAlertas    = CrearBtnMenu("  Alertas");
            btnNuevaOrden = CrearBtnMenu("  + Nueva Orden");
            btnSalir      = CrearBtnMenu("  Cerrar Sesion");

            // Administración: catálogo de proveedores y usuarios solo para el Administrador (rol 1);
            // cambiar la contraseña propia lo puede hacer cualquier usuario.
            bool esAdmin = FrmLogin.UsuarioActual?.RolId == 1;
            btnCatalogo  = CrearBtnMenu("  Prov. y Productos");
            btnUsuarios  = CrearBtnMenu("  Usuarios");
            btnPassword  = CrearBtnMenu("  Cambiar contraseña");
            btnCatalogo.Visible = esAdmin;
            btnUsuarios.Visible = esAdmin;

            // Indicadores: lo ven todos los roles (el Comprador solo ve las cifras de sus órdenes)
            btnIndicadores = CrearBtnMenu("  Indicadores");
            btnIndicadores.Click += (s, e) => { using (var f = new FrmIndicadores()) f.ShowDialog(this); };

            btnCatalogo.Click += (s, e) => { using (var f = new FrmProveedorProductos()) f.ShowDialog(this); };
            btnUsuarios.Click += (s, e) => { using (var f = new FrmUsuarios())           f.ShowDialog(this); };
            btnPassword.Click += (s, e) => { using (var f = new FrmCambiarPassword())    f.ShowDialog(this); };

            btnNuevaOrden.BackColor = Color.FromArgb(0, 120, 130);
            btnNuevaOrden.ForeColor = Color.White;
            btnNuevaOrden.Visible   = puedeCrear;

            btnDashboard.Click  += (s, e) => { ActivarMenu(btnDashboard);  MostrarDashboard(); };
            btnOrdenes.Click    += (s, e) => { ActivarMenu(btnOrdenes);    MostrarOrdenes(); };
            btnAlertas.Click    += (s, e) => { ActivarMenu(btnAlertas);    MostrarAlertas(); };
            btnNuevaOrden.Click += (s, e) => AbrirNuevaOrden();
            btnSalir.Click      += (s, e) => CerrarSesion();

            var spacer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(10, 37, 64) };

            panelMenu.Controls.Add(spacer);
            panelMenu.Controls.Add(btnSalir);
            panelMenu.Controls.Add(btnPassword);
            if (esAdmin) panelMenu.Controls.Add(btnUsuarios);
            if (esAdmin) panelMenu.Controls.Add(btnCatalogo);
            if (puedeCrear) panelMenu.Controls.Add(btnNuevaOrden);
            panelMenu.Controls.Add(btnIndicadores);
            panelMenu.Controls.Add(btnAlertas);
            panelMenu.Controls.Add(btnOrdenes);
            panelMenu.Controls.Add(btnDashboard);

            ActivarMenu(btnDashboard);
            this.Controls.Add(panelMenu);
        }

        private Button CrearBtnMenu(string texto)
        {
            var btn = new Button
            {
                Text      = texto,
                Dock      = DockStyle.Top,
                Height    = 44,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 220, 240, 255),
                BackColor = Color.FromArgb(10, 37, 64),
                Font      = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(16, 0, 0, 0),
                Cursor    = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 255, 255, 255);
            return btn;
        }

        private void ActivarMenu(Button btn)
        {
            if (_btnActivo != null)
            {
                _btnActivo.BackColor = Color.FromArgb(10, 37, 64);
                _btnActivo.ForeColor = Color.FromArgb(180, 220, 240, 255);
            }
            btn.BackColor = Color.FromArgb(0, 173, 181);
            btn.ForeColor = Color.White;
            _btnActivo = btn;
        }

        // ═══════════════════════════════════════════════════════
        // CONTENIDO
        // ═══════════════════════════════════════════════════════
        private void ConstruirContenido()
        {
            panelContent = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding   = new Padding(0)
            };

            // Encabezado sección
            lblSeccion = new Label
            {
                Text      = "Dashboard",
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 40, 55),
                Location  = new Point(20, 16),
                AutoSize  = true
            };
            lblSubtitulo = new Label
            {
                Text      = "Resumen general del estado de las ordenes de compra",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(20, 44),
                AutoSize  = true
            };

            panelContent.Controls.Add(lblSubtitulo);
            panelContent.Controls.Add(lblSeccion);

            ConstruirKPIs();
            ConstruirFiltros();
            ConstruirGrid();

            this.Controls.Add(panelContent);
        }

        // ── KPI ─────────────────────────────────────────────────
        private void ConstruirKPIs()
        {
            panelKpis = new Panel
            {
                Location  = new Point(20, 70),
                Height    = 95,
                BackColor = Color.Transparent
            };

            cardTotal        = CrearCard("Total",       "—",   Color.FromArgb(15, 52, 96));
            cardActivas      = CrearCard("Activas",      "—",   Color.FromArgb(59, 130, 246));
            cardVencidas     = CrearCard("Vencidas",     "—",   Color.FromArgb(239, 68, 68));
            cardCumplimiento = CrearCard("Cumplimiento", "— %", Color.FromArgb(16, 185, 129));
            cardAlertas      = CrearCard("Alertas",      "—",   Color.FromArgb(245, 158, 11));
            cardDisputa      = CrearCard("En Disputa",   "—",   Color.FromArgb(239, 68, 120));

            panelKpis.Controls.AddRange(new Control[]
            {
                cardTotal, cardActivas, cardVencidas,
                cardCumplimiento, cardAlertas, cardDisputa
            });

            panelContent.Controls.Add(panelKpis);

            // Reposicionar cards cuando cambie el ancho
            panelContent.Resize += (s, e) => ReposicionarCards();
            ReposicionarCards();
        }

        private void ReposicionarCards()
        {
            int ancho    = (panelContent.Width - 40 - 5 * 10) / 6;
            if (ancho < 130) ancho = 130;
            panelKpis.Width = panelContent.Width - 40;

            var cards = new[] { cardTotal, cardActivas, cardVencidas,
                                cardCumplimiento, cardAlertas, cardDisputa };
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].Width    = ancho;
                cards[i].Location = new Point(i * (ancho + 10), 0);
            }
        }

        private Panel CrearCard(string titulo, string valor, Color color)
        {
            var card = new Panel
            {
                Size      = new Size(160, 90),
                BackColor = Color.White
            };

            // Barra izquierda de color
            var barra = new Panel
            {
                Width     = 5,
                Dock      = DockStyle.Left,
                BackColor = color
            };

            var lblValor = new Label
            {
                Name      = "val",
                Text      = valor,
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = color,
                Location  = new Point(16, 10),
                Size      = new Size(130, 42),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblTit = new Label
            {
                Text      = titulo,
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(16, 55),
                Size      = new Size(130, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.AddRange(new Control[] { barra, lblValor, lblTit });
            return card;
        }

        private void SetCardValor(Panel card, string valor)
        {
            foreach (Control c in card.Controls)
                if (c is Label l && l.Name == "val")
                    l.Text = valor;
        }

        // ── Filtros ─────────────────────────────────────────────
        private void ConstruirFiltros()
        {
            panelFiltros = new Panel
            {
                Location  = new Point(20, 175),
                Height    = 44,
                BackColor = Color.White
            };
            panelFiltros.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(226, 232, 240)))
                    e.Graphics.DrawRectangle(pen, 0, 0,
                        panelFiltros.Width - 1, panelFiltros.Height - 1);
            };

            var lblIcon = new Label
            {
                Text     = "Buscar:",
                Font     = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor= Color.FromArgb(100, 116, 139),
                Location = new Point(10, 12),
                AutoSize = true
            };

            txtBusqueda = new TextBox
            {
                Location    = new Point(70, 10),
                Size        = new Size(200, 24),
                Font        = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                Text        = ""
            };

            btnBuscar = new Button
            {
                Text      = "Buscar",
                Location  = new Point(280, 8),
                Size      = new Size(75, 28),
                BackColor = Color.FromArgb(15, 52, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnBuscar.FlatAppearance.BorderSize = 0;
            btnBuscar.Click += (s, e) => AplicarFiltros();

            btnLimpiar = new Button
            {
                Text      = "Limpiar",
                Location  = new Point(362, 8),
                Size      = new Size(70, 28),
                BackColor = Color.FromArgb(245, 247, 250),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9),
                Cursor    = Cursors.Hand
            };
            btnLimpiar.FlatAppearance.BorderSize  = 1;
            btnLimpiar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnLimpiar.Click += (s, e) =>
            {
                txtBusqueda.Text = "";
                CargarGridOrdenes();
            };

            lblContador = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(445, 13),
                AutoSize  = true
            };

            panelFiltros.Controls.AddRange(new Control[]
            { lblIcon, txtBusqueda, btnBuscar, btnLimpiar, lblContador });

            txtBusqueda.KeyDown += (s, e) =>
            { if (e.KeyCode == Keys.Enter) AplicarFiltros(); };

            panelContent.Controls.Add(panelFiltros);

            panelContent.Resize += (s, e) =>
            {
                panelFiltros.Width = panelContent.Width - 40;
            };
        }

        // ── Grid ────────────────────────────────────────────────
        private void ConstruirGrid()
        {
            dgvOrdenes = new DataGridView
            {
                Location                    = new Point(20, 228),
                BackgroundColor             = Color.White,
                BorderStyle                 = BorderStyle.None,
                ReadOnly                    = true,
                AllowUserToAddRows          = false,
                AllowUserToDeleteRows       = false,
                AllowUserToResizeRows       = false,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible           = false,
                MultiSelect                 = false,
                Font                        = new Font("Segoe UI", 9),
                GridColor                   = Color.FromArgb(226, 232, 240),
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowTemplate                 = { Height = 36 },
                Cursor                      = Cursors.Hand,
                AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvOrdenes.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(15, 52, 96),
                ForeColor          = Color.White,
                Font               = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding            = new Padding(8, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(15, 52, 96)
            };

            dgvOrdenes.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(170, 233, 238),
                SelectionForeColor = Color.FromArgb(30, 40, 55),
                Padding            = new Padding(6, 0, 6, 0)
            };

            dgvOrdenes.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(248, 250, 252),
                SelectionBackColor = Color.FromArgb(170, 233, 238),
                SelectionForeColor = Color.FromArgb(30, 40, 55)
            };

            dgvOrdenes.CellDoubleClick += DgvOrdenes_CellDoubleClick;

            panelContent.Resize += (s, e) =>
            {
                int top = dgvOrdenes.Location.Y;
                dgvOrdenes.Width  = panelContent.Width  - 40;
                dgvOrdenes.Height = panelContent.Height - top - 10;
            };

            panelContent.Controls.Add(dgvOrdenes);
        }

        // ═══════════════════════════════════════════════════════
        // DATOS
        // ═══════════════════════════════════════════════════════
        private void CargarDashboard()
        {
            try
            {
                var kpi = _repo.ObtenerKPIs();
                SetCardValor(cardTotal,        kpi.TotalOrdenes.ToString());
                SetCardValor(cardActivas,       kpi.OrdenesActivas.ToString());
                SetCardValor(cardVencidas,      kpi.OrdenesVencidas.ToString());
                SetCardValor(cardCumplimiento,  kpi.PorcentajeCumplimiento.ToString("0.#") + "%");
                SetCardValor(cardAlertas,       kpi.AlertasPendientes.ToString());
                SetCardValor(cardDisputa,       kpi.OrdenesEnDisputa.ToString());

                CargarGridOrdenes();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGridOrdenes(string busqueda = null)
        {
            try
            {
                if (busqueda == null && txtBusqueda != null &&
                    !string.IsNullOrWhiteSpace(txtBusqueda.Text))
                    busqueda = txtBusqueda.Text.Trim();

                var ordenes = _repo.ObtenerOrdenes(numeroOrden: busqueda);
                dgvOrdenes.Rows.Clear();
                dgvOrdenes.Columns.Clear();

                // Columnas con ancho proporcional
                Columna("NumOrd",    "# Orden",       90);
                Columna("Prov",      "Proveedor",      160);
                Columna("FecEm",     "Emision",        82);
                Columna("FecReq",    "Requerida",      82);
                Columna("Estatus",   "Estatus",        110);
                Columna("Prior",     "Prioridad",      72);
                Columna("Cumpl",     "Cumplimiento",   105);
                Columna("Monto",     "Monto",          125);
                Columna("Alert",     "Alertas",        55);
                Columna("Dias",      "Dias",           45);

                foreach (var o in ordenes)
                {
                    int idx = dgvOrdenes.Rows.Add(
                        o.NumeroOrden,
                        o.ProveedorNombre,
                        o.FechaEmision.ToString("dd/MM/yy"),
                        o.FechaRequerida.ToString("dd/MM/yy"),
                        o.EstatusNombre,
                        o.PrioridadTexto,
                        o.Cumplimiento,
                        o.MontoTotal.ToString("N0") + " " + o.Moneda,
                        o.AlertasNoLeidas > 0 ? o.AlertasNoLeidas.ToString() : "",
                        o.DiasTranscurridos.ToString()
                    );

                    var row = dgvOrdenes.Rows[idx];
                    row.Tag = o;   // la fila lleva su propia orden (sirve aunque se ordene la grilla)

                    // Color fondo por cumplimiento
                    if (o.Cumplimiento == "Vencida")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 226, 226);
                        row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 200, 200);
                    }
                    else if (o.Cumplimiento == "Proxima a Vencer" || o.Cumplimiento == "Próxima a Vencer")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(254, 243, 199);
                        row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 230, 160);
                    }
                    else if (o.Cumplimiento == "A Tiempo")
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(209, 250, 229);
                        row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 240, 210);
                    }

                    // Color prioridad
                    Color cPrior = o.Prioridad == 1
                        ? Color.FromArgb(239, 68, 68)
                        : o.Prioridad == 2
                            ? Color.FromArgb(245, 158, 11)
                            : Color.FromArgb(16, 185, 129);

                    row.Cells["Prior"].Style.ForeColor = cPrior;
                    row.Cells["Prior"].Style.Font      = new Font("Segoe UI", 9, FontStyle.Bold);

                    // Color estatus
                    if (!string.IsNullOrEmpty(o.EstatusColor))
                    {
                        try
                        {
                            row.Cells["Estatus"].Style.ForeColor =
                                ColorTranslator.FromHtml(o.EstatusColor);
                        }
                        catch { }
                    }

                    // Alertas en rojo
                    if (o.AlertasNoLeidas > 0)
                        row.Cells["Alert"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                }

                lblContador.Text = ordenes.Count + " orden(es) encontrada(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar ordenes:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Columna(string name, string header, int width)
        {
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name            = name,
                HeaderText      = header,
                Width           = width,
                AutoSizeMode    = DataGridViewAutoSizeColumnMode.None,
                DefaultCellStyle = new DataGridViewCellStyle
                    { Padding = new Padding(6, 0, 6, 0) }
            });
        }

        // ═══════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ═══════════════════════════════════════════════════════
        private void MostrarDashboard()
        {
            _vista = Vista.Dashboard;
            lblSeccion.Text   = "Dashboard";
            lblSubtitulo.Text = "Resumen general del estado de las ordenes de compra";
            panelKpis.Visible    = true;
            panelFiltros.Visible = true;
            panelFiltros.Location = new Point(20, 175);
            dgvOrdenes.Location   = new Point(20, 228);
            dgvOrdenes.Height     = panelContent.Height - 238;
            CargarDashboard();
        }

        private void MostrarOrdenes()
        {
            _vista = Vista.Ordenes;
            lblSeccion.Text   = "Ordenes de Compra";
            lblSubtitulo.Text = "Gestion y seguimiento de todas las ordenes";
            panelKpis.Visible    = false;
            panelFiltros.Visible = true;
            panelFiltros.Location = new Point(20, 70);
            dgvOrdenes.Location   = new Point(20, 123);
            dgvOrdenes.Height     = panelContent.Height - 133;
            CargarGridOrdenes();
        }

        private void MostrarAlertas()
        {
            _vista = Vista.Alertas;
            lblSeccion.Text   = "Alertas del Sistema";
            lblSubtitulo.Text = "Notificaciones pendientes. Doble clic en una alerta para marcarla como leida";
            panelKpis.Visible    = false;
            panelFiltros.Visible = false;
            dgvOrdenes.Location  = new Point(20, 70);
            dgvOrdenes.Height    = panelContent.Height - 80;

            try
            {
                var alertas = _repo.ObtenerAlertas();
                dgvOrdenes.Rows.Clear();
                dgvOrdenes.Columns.Clear();

                Columna("ColOrden",   "# Orden",  90);
                Columna("ColTipo",    "Tipo",      130);
                Columna("ColMensaje", "Mensaje",   450);
                Columna("ColFecha",   "Fecha",     120);
                Columna("ColEstado",  "Estado",    70);

                // Rellenar columna Mensaje para que ocupe el espacio
                dgvOrdenes.Columns["ColMensaje"].AutoSizeMode =
                    DataGridViewAutoSizeColumnMode.Fill;

                foreach (var a in alertas)
                {
                    int idx = dgvOrdenes.Rows.Add(
                        a.NumeroOrden,
                        a.TipoAlerta,
                        a.Mensaje,
                        a.FechaGenerada.ToString("dd/MM/yy HH:mm"),
                        a.Leida ? "Leida" : "Nueva"
                    );
                    dgvOrdenes.Rows[idx].Tag = a;
                    if (!a.Leida)
                    {
                        dgvOrdenes.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(254, 252, 232);
                        dgvOrdenes.Rows[idx].Cells["ColEstado"].Style.ForeColor =
                            Color.FromArgb(180, 83, 9);
                        dgvOrdenes.Rows[idx].Cells["ColEstado"].Style.Font =
                            new Font("Segoe UI", 9, FontStyle.Bold);
                    }
                }

                lblContador.Text = alertas.Count + " alerta(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AplicarFiltros()
        {
            string b = txtBusqueda.Text.Trim();
            CargarGridOrdenes(string.IsNullOrEmpty(b) ? null : b);
        }

        private void AbrirNuevaOrden()
        {
            var frm = new FrmNuevaOrden();
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                ActivarMenu(btnOrdenes);
                MostrarOrdenes();
            }
        }

        // ═══════════════════════════════════════════════════════
        // EVENTOS
        // ═══════════════════════════════════════════════════════
        private void DgvOrdenes_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var tag = dgvOrdenes.Rows[e.RowIndex].Tag;

            var orden = tag as OrdenCompra;
            if (orden != null)
            {
                using (var frm = new FrmDetalleOrden(orden))
                    frm.ShowDialog(this);
                RefrescarVistaActual();
                return;
            }

            // Doble clic en una alerta nueva = marcarla como leida
            var alerta = tag as Alerta;
            if (alerta != null && !alerta.Leida)
            {
                try
                {
                    _repo.MarcarAlertaLeida(alerta.AlertaId);
                    MostrarAlertas();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo marcar la alerta:\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Recarga solo la pantalla que el usuario esta viendo
        private void RefrescarVistaActual()
        {
            switch (_vista)
            {
                case Vista.Alertas: MostrarAlertas();    break;
                case Vista.Ordenes: CargarGridOrdenes(); break;
                default:            CargarDashboard();   break;
            }
        }

        private void CerrarSesion()
        {
            if (MessageBox.Show("Deseas cerrar la sesion?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _cerrandoSesion = true;
            timerRefresh?.Stop();

            var login = Application.OpenForms.OfType<FrmLogin>().FirstOrDefault();
            if (login != null)
            {
                login.Reiniciar();
                login.Show();
            }
            this.Close();
        }

        private void IniciarTimer()
        {
            timerRefresh = new Timer { Interval = 60000 };
            timerRefresh.Tick += (s, e) => RefrescarVistaActual();
            timerRefresh.Start();
        }
    }
}
