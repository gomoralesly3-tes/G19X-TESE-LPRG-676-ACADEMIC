using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Forms
{
    public class FrmDashboard : Form
    {
        private readonly OrdenCompraRepository _repo = new OrdenCompraRepository();

        // Controles del dashboard
        private Panel panelMenu, panelContent, panelHeader;
        private Label lblBienvenida, lblFecha;
        private Panel cardTotal, cardActivas, cardVencidas, cardCumplimiento, cardAlertas, cardDisputa;
        private DataGridView dgvOrdenes;
        private Button btnVerOrdenes, btnVerAlertas, btnDashboard, btnSalir;
        private Label lblSeccion;
        private Timer timerActualizar;

        public FrmDashboard()
        {
            InitializeComponent();
            CargarDashboard();
            ConfigurarTimer();
        }

        private void InitializeComponent()
        {
            this.Text            = "Monitoreo de Órdenes de Compra — Develop Talent & Technology";
            this.Size            = new Size(1280, 780);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.MinimumSize     = new Size(1100, 650);
            this.BackColor       = Color.FromArgb(245, 247, 250);
            this.FormClosing     += FrmDashboard_FormClosing;

            ConstruirHeader();
            ConstruirMenu();
            ConstruirAreaContenido();
        }

        // -------------------------------------------------------
        // HEADER
        // -------------------------------------------------------
        private void ConstruirHeader()
        {
            panelHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 60,
                BackColor = Color.FromArgb(30, 58, 95)
            };

            var lblTitulo = new Label
            {
                Text      = "📦  Plataforma de Monitoreo de Órdenes de Compra",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                Location  = new Point(220, 18),
                AutoSize  = true
            };

            lblBienvenida = new Label
            {
                Text      = $"👤 {FrmLogin.UsuarioActual?.NombreCompleto} | {FrmLogin.UsuarioActual?.RolNombre}",
                ForeColor = Color.LightGray,
                Font      = new Font("Segoe UI", 9),
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };

            lblFecha = new Label
            {
                Text      = DateTime.Now.ToString("dddd, dd MMMM yyyy", new System.Globalization.CultureInfo("es-MX")),
                ForeColor = Color.LightSkyBlue,
                Font      = new Font("Segoe UI", 8),
                AutoSize  = true,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };

            panelHeader.Controls.AddRange(new Control[] { lblTitulo, lblBienvenida, lblFecha });
            panelHeader.Resize += (s, e) =>
            {
                lblBienvenida.Location = new Point(panelHeader.Width - lblBienvenida.Width - 20, 12);
                lblFecha.Location      = new Point(panelHeader.Width - lblFecha.Width - 20, 36);
            };

            this.Controls.Add(panelHeader);
        }

        // -------------------------------------------------------
        // MENÚ LATERAL
        // -------------------------------------------------------
        private void ConstruirMenu()
        {
            panelMenu = new Panel
            {
                Width     = 200,
                BackColor = Color.FromArgb(22, 44, 72),
                Dock      = DockStyle.Left
            };

            // Logo empresa
            var lblLogo = new Label
            {
                Text      = "PLURIONE",
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Top,
                Height    = 50,
                BackColor = Color.FromArgb(30, 58, 95)
            };
            panelMenu.Controls.Add(lblLogo);

            btnDashboard  = CrearBotonMenu("🏠  Dashboard",  new Point(0, 60));
            btnVerOrdenes = CrearBotonMenu("📋  Órdenes",    new Point(0, 105));
            btnVerAlertas = CrearBotonMenu("🔔  Alertas",    new Point(0, 150));
            btnSalir      = CrearBotonMenu("🚪  Cerrar Sesión", new Point(0, 195));

            btnDashboard.BackColor  = Color.FromArgb(42, 74, 112);  // activo por defecto
            btnDashboard.Click  += (s, e) => { MostrarDashboard();        ActualizarMenuActivo(btnDashboard); };
            btnVerOrdenes.Click += (s, e) => { MostrarListaOrdenes();     ActualizarMenuActivo(btnVerOrdenes); };
            btnVerAlertas.Click += (s, e) => { MostrarAlertas();          ActualizarMenuActivo(btnVerAlertas); };
            btnSalir.Click      += (s, e) => CerrarSesion();

            panelMenu.Controls.AddRange(new Control[] { btnDashboard, btnVerOrdenes, btnVerAlertas, btnSalir });
            this.Controls.Add(panelMenu);
        }

        private Button CrearBotonMenu(string texto, Point ubicacion)
        {
            return new Button
            {
                Text      = texto,
                Location  = ubicacion,
                Size      = new Size(200, 44),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 44, 72),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 10),
                Cursor    = Cursors.Hand,
                Padding   = new Padding(15, 0, 0, 0)
            };
        }

        private void ActualizarMenuActivo(Button activo)
        {
            foreach (Control c in panelMenu.Controls)
                if (c is Button btn && btn != btnSalir)
                    btn.BackColor = Color.FromArgb(22, 44, 72);
            activo.BackColor = Color.FromArgb(42, 74, 112);
        }

        // -------------------------------------------------------
        // ÁREA DE CONTENIDO
        // -------------------------------------------------------
        private void ConstruirAreaContenido()
        {
            panelContent = new Panel
            {
                BackColor = Color.FromArgb(245, 247, 250),
                Dock      = DockStyle.Fill,
                Padding   = new Padding(20)
            };

            lblSeccion = new Label
            {
                Text     = "Dashboard",
                Font     = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor= Color.FromArgb(30, 58, 95),
                Location = new Point(20, 10),
                AutoSize = true
            };
            panelContent.Controls.Add(lblSeccion);

            ConstruirTarjetasKPI();
            ConstruirGridOrdenes();

            this.Controls.Add(panelContent);
        }

        // -------------------------------------------------------
        // TARJETAS KPI
        // -------------------------------------------------------
        private void ConstruirTarjetasKPI()
        {
            cardTotal         = CrearTarjeta("Total Órdenes",   "—", Color.FromArgb(30, 58, 95),  new Point(20, 50));
            cardActivas       = CrearTarjeta("Activas",          "—", Color.FromArgb(33, 150, 243), new Point(185, 50));
            cardVencidas      = CrearTarjeta("Vencidas",         "—", Color.FromArgb(244, 67, 54),  new Point(350, 50));
            cardCumplimiento  = CrearTarjeta("Cumplimiento",     "—%",Color.FromArgb(76, 175, 80),  new Point(515, 50));
            cardAlertas       = CrearTarjeta("Alertas",          "—", Color.FromArgb(255, 152, 0),  new Point(680, 50));
            cardDisputa       = CrearTarjeta("En Disputa",       "—", Color.FromArgb(255, 87, 34),  new Point(845, 50));

            panelContent.Controls.AddRange(new Control[]
            {
                cardTotal, cardActivas, cardVencidas,
                cardCumplimiento, cardAlertas, cardDisputa
            });
        }

        private Panel CrearTarjeta(string titulo, string valor, Color color, Point ubicacion)
        {
            var card = new Panel
            {
                Location  = ubicacion,
                Size      = new Size(155, 90),
                BackColor = Color.White
            };

            // Borde de color superior
            var barraColor = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 5,
                BackColor = color
            };

            var lblValor = new Label
            {
                Name      = "lblValor",
                Text      = valor,
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = color,
                Location  = new Point(10, 14),
                AutoSize  = true
            };

            var lblTit = new Label
            {
                Text      = titulo,
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.Gray,
                Location  = new Point(10, 58),
                AutoSize  = true
            };

            card.Controls.AddRange(new Control[] { barraColor, lblValor, lblTit });
            return card;
        }

        private void ActualizarTarjeta(Panel card, string valor)
        {
            foreach (Control c in card.Controls)
                if (c is Label lbl && lbl.Name == "lblValor")
                    lbl.Text = valor;
        }

        // -------------------------------------------------------
        // GRID DE ÓRDENES
        // -------------------------------------------------------
        private void ConstruirGridOrdenes()
        {
            dgvOrdenes = new DataGridView
            {
                Location             = new Point(20, 155),
                Anchor               = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackgroundColor      = Color.White,
                BorderStyle          = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                ReadOnly             = true,
                AllowUserToAddRows   = false,
                AllowUserToDeleteRows= false,
                SelectionMode        = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible    = false,
                AutoSizeColumnsMode  = DataGridViewAutoSizeColumnsMode.AllCells,
                Font                 = new Font("Segoe UI", 9),
                GridColor            = Color.FromArgb(230, 230, 230),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 58, 95),
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 9, FontStyle.Bold)
                }
            };
            dgvOrdenes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 245);
            dgvOrdenes.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvOrdenes.CellDoubleClick += DgvOrdenes_CellDoubleClick;

            // Calcular tamaño dinámico
            panelContent.Resize += (s, e) =>
                dgvOrdenes.Size = new Size(panelContent.Width - 40, panelContent.Height - 180);

            panelContent.Controls.Add(dgvOrdenes);
        }

        // -------------------------------------------------------
        // CARGA DE DATOS
        // -------------------------------------------------------
        private void CargarDashboard()
        {
            try
            {
                var kpis = _repo.ObtenerKPIs();
                ActualizarTarjeta(cardTotal,        kpis.TotalOrdenes.ToString());
                ActualizarTarjeta(cardActivas,       kpis.OrdenesActivas.ToString());
                ActualizarTarjeta(cardVencidas,      kpis.OrdenesVencidas.ToString());
                ActualizarTarjeta(cardCumplimiento,  $"{kpis.PorcentajeCumplimiento}%");
                ActualizarTarjeta(cardAlertas,       kpis.AlertasPendientes.ToString());
                ActualizarTarjeta(cardDisputa,       kpis.OrdenesEnDisputa.ToString());

                CargarGridOrdenes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar datos:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGridOrdenes(int? estatusId = null, string busqueda = null)
        {
            try
            {
                var ordenes = _repo.ObtenerOrdenes(estatusId: estatusId, numeroOrden: busqueda);
                dgvOrdenes.Rows.Clear();
                dgvOrdenes.Columns.Clear();

                // Definir columnas
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "NumeroOrden",     HeaderText = "# Orden",      Width = 120 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProveedorNombre", HeaderText = "Proveedor",    Width = 180 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaEmision",    HeaderText = "Emisión",      Width = 95  });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "FechaRequerida",  HeaderText = "Requerida",    Width = 95  });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "EstatusNombre",   HeaderText = "Estatus",      Width = 130 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Prioridad",       HeaderText = "Prioridad",    Width = 80  });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cumplimiento",    HeaderText = "Cumplimiento", Width = 110 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "MontoTotal",      HeaderText = "Monto (MXN)", Width = 110 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "Alertas",         HeaderText = "⚠ Alertas",   Width = 65  });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "DiasOC",          HeaderText = "Días OC",     Width = 65  });

                // Guardar referencia al objeto completo en tag
                dgvOrdenes.Tag = ordenes;

                foreach (var o in ordenes)
                {
                    int rowIdx = dgvOrdenes.Rows.Add(
                        o.NumeroOrden,
                        o.ProveedorNombre,
                        o.FechaEmision.ToString("dd/MM/yyyy"),
                        o.FechaRequerida.ToString("dd/MM/yyyy"),
                        o.EstatusNombre,
                        o.PrioridadTexto,
                        o.Cumplimiento,
                        o.MontoTotal.ToString("N2"),
                        o.AlertasNoLeidas > 0 ? $"🔔 {o.AlertasNoLeidas}" : "—",
                        o.DiasTranscurridos
                    );

                    // Colorear filas por cumplimiento/estatus
                    var row = dgvOrdenes.Rows[rowIdx];
                    switch (o.Cumplimiento)
                    {
                        case "Vencida":
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235); break;
                        case "Próxima a Vencer":
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225); break;
                        case "A Tiempo":
                            row.DefaultCellStyle.BackColor = Color.FromArgb(232, 245, 233); break;
                    }
                    if (o.Prioridad == 1)
                        row.Cells["Prioridad"].Style.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar órdenes:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -------------------------------------------------------
        // NAVEGACIÓN
        // -------------------------------------------------------
        private void MostrarDashboard()
        {
            lblSeccion.Text = "Dashboard";
            CargarDashboard();
            dgvOrdenes.Visible = true;
            foreach (Control c in panelContent.Controls)
                if (c is Panel p && p != panelContent)
                    p.Visible = (p == cardTotal || p == cardActivas || p == cardVencidas
                                || p == cardCumplimiento || p == cardAlertas || p == cardDisputa);
        }

        private void MostrarListaOrdenes()
        {
            lblSeccion.Text = "Órdenes de Compra";
            CargarGridOrdenes();
        }

        private void MostrarAlertas()
        {
            lblSeccion.Text = "Alertas del Sistema";
            try
            {
                var alertas = _repo.ObtenerAlertas();
                dgvOrdenes.Rows.Clear();
                dgvOrdenes.Columns.Clear();

                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColNumeroOrden", HeaderText = "# Orden",  Width = 120 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColTipo",        HeaderText = "Tipo",      Width = 150 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColMensaje",     HeaderText = "Mensaje",   Width = 450 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColFecha",       HeaderText = "Fecha",     Width = 130 });
                dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColEstado",      HeaderText = "Estado",    Width = 80  });

                foreach (var a in alertas)
                {
                    int rowIdx = dgvOrdenes.Rows.Add(
                        a.NumeroOrden,
                        a.TipoAlerta,
                        a.Mensaje,
                        a.FechaGenerada.ToString("dd/MM/yyyy HH:mm"),
                        a.Leida ? "Leída" : "🔴 Nueva"
                    );
                    if (!a.Leida)
                        dgvOrdenes.Rows[rowIdx].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 205);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar alertas:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -------------------------------------------------------
        // EVENTOS
        // -------------------------------------------------------
        private void DgvOrdenes_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || lblSeccion.Text != "Órdenes de Compra") return;

            var ordenes = dgvOrdenes.Tag as List<OrdenCompra>;
            if (ordenes == null || e.RowIndex >= ordenes.Count) return;

            var orden = ordenes[e.RowIndex];
            var detalle = new FrmDetalleOrden(orden);
            detalle.ShowDialog(this);
            CargarDashboard(); // Refrescar tras posibles cambios
        }

        private void CerrarSesion()
        {
            if (MessageBox.Show("¿Desea cerrar la sesión?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.OpenForms["FrmLogin"]?.Show();
                this.Close();
            }
        }

        private void FrmDashboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            timerActualizar?.Stop();
            Application.Exit();
        }

        private void ConfigurarTimer()
        {
            timerActualizar = new Timer { Interval = 60000 }; // Refrescar cada 60 segundos
            timerActualizar.Tick += (s, e) => CargarDashboard();
            timerActualizar.Start();
        }
    }
}
