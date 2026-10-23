using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Forms
{
    /// <summary>
    /// Formulario de detalle de una orden de compra.
    /// Muestra información completa, historial y permite cambiar estatus.
    /// </summary>
    public class FrmDetalleOrden : Form
    {
        private readonly OrdenCompra _orden;
        private readonly OrdenCompraRepository _repo = new OrdenCompraRepository();

        // Controles
        private TabControl tabControl;
        private TabPage tabInfo, tabHistorial;
        private DataGridView dgvHistorial;
        private Button btnCambiarEstatus, btnCerrar;
        private ComboBox cboNuevoEstatus;
        private Label    lblEstatusFinal;

        // Incidencias de la orden
        private readonly IncidenciaRepository _incRepo = new IncidenciaRepository();
        private TabPage      tabIncidencias;
        private DataGridView dgvIncidencias;
        private Button       btnRegistrarInc, btnResolverInc;

        // Registran incidencias: 1 Administrador, 2 Comprador, 3 Almacén, 4 Supervisor.
        // Las resuelven: 1 Administrador, 2 Comprador y 4 Supervisor. La base de datos valida lo mismo
        // (sp_RegistrarIncidencia y sp_ResolverIncidencia) y limita al Comprador a sus propias órdenes.
        private static readonly int[] RolesQueRegistranIncidencias = { 1, 2, 3, 4 };
        private static readonly int[] RolesQueResuelvenIncidencias = { 1, 2, 4 };

        private static bool TieneRol(int[] roles)
        {
            var usuario = FrmLogin.UsuarioActual;
            return usuario != null && Array.IndexOf(roles, usuario.RolId) >= 0;
        }

        // Roles que pueden cambiar estatus alguna vez: 1 Administrador, 2 Comprador,
        // 3 Almacén, 4 Supervisor. QUE cambios concretos puede hacer cada rol lo define
        // la tabla EstatusTransicionRoles y lo valida sp_CambiarEstatusOrden.
        private static readonly int[] RolesQueCambianEstatus = { 1, 2, 3, 4 };

        private static bool PuedeCambiarEstatus()
        {
            var usuario = FrmLogin.UsuarioActual;
            return usuario != null && Array.IndexOf(RolesQueCambianEstatus, usuario.RolId) >= 0;
        }

        /// <summary>True si se cambió el estatus; el llamador solo refresca si es necesario.</summary>
        public bool HuboCambios { get; private set; }

        public FrmDetalleOrden(OrdenCompra orden)
        {
            _orden = orden;
            InitializeComponent();
            CargarIncidencias();   // antes que CargarDatos: deja actualizado el conteo de incidencias abiertas
            CargarDatos();
        }

        private void InitializeComponent()
        {
            this.Text            = $"Detalle: {_orden.NumeroOrden}";
            this.Size            = new Size(820, 620);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.White;

            // Tab control
            tabControl = new TabControl
            {
                Location = new Point(10, 10),
                Size     = new Size(785, 510)
            };

            tabInfo      = new TabPage("📋 Información General");
            tabHistorial = new TabPage("🕐 Historial de Estatus");
            tabIncidencias = new TabPage("⚠ Incidencias");

            tabControl.TabPages.AddRange(new[] { tabInfo, tabHistorial, tabIncidencias });
            this.Controls.Add(tabControl);

            // Botones inferiores
            btnCambiarEstatus = new Button
            {
                Text      = "Cambiar Estatus",
                Location  = new Point(10, 530),
                Size      = new Size(150, 35),
                BackColor = Color.FromArgb(30, 58, 95),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnCambiarEstatus.FlatAppearance.BorderSize = 0;
            btnCambiarEstatus.Click += BtnCambiarEstatus_Click;

            cboNuevoEstatus = new ComboBox
            {
                Location     = new Point(170, 535),
                Size         = new Size(200, 28),
                DropDownStyle= ComboBoxStyle.DropDownList,
                Font         = new Font("Segoe UI", 9)
            };
            lblEstatusFinal = new Label
            {
                Text      = "No hay cambios de estatus disponibles para esta orden con su rol.",
                Location  = new Point(10, 540),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Visible   = false
            };

            CargarEstatusCombo();

            btnCerrar = new Button
            {
                Text      = "Cerrar",
                Location  = new Point(385, 530),
                Size      = new Size(100, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9),
                Cursor    = Cursors.Hand
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { btnCambiarEstatus, cboNuevoEstatus, lblEstatusFinal, btnCerrar });

            // Sin permiso: solo consulta (no se muestran los controles de cambio)
            if (!PuedeCambiarEstatus())
            {
                btnCambiarEstatus.Visible = false;
                cboNuevoEstatus.Visible   = false;
            }

            // Tab Historial
            dgvHistorial = new DataGridView
            {
                Dock                = DockStyle.Fill,
                ReadOnly            = true,
                AllowUserToAddRows  = false,
                RowHeadersVisible   = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                Font                = new Font("Segoe UI", 9),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 58, 95),
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 9, FontStyle.Bold)
                }
            };
            tabHistorial.Controls.Add(dgvHistorial);

            // Tab Incidencias
            dgvIncidencias = new DataGridView
            {
                Dock                = DockStyle.Fill,
                ReadOnly            = true,
                MultiSelect         = false,
                AllowUserToAddRows  = false,
                RowHeadersVisible   = false,
                SelectionMode       = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor     = Color.White,
                BorderStyle         = BorderStyle.None,
                Font                = new Font("Segoe UI", 9),
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(30, 58, 95),
                    ForeColor = Color.White,
                    Font      = new Font("Segoe UI", 9, FontStyle.Bold)
                }
            };
            dgvIncidencias.Columns.Add("Fecha",       "Fecha");
            dgvIncidencias.Columns.Add("Tipo",        "Tipo");
            dgvIncidencias.Columns.Add("Gravedad",    "Gravedad");
            dgvIncidencias.Columns.Add("Descripcion", "Descripción");
            dgvIncidencias.Columns.Add("Reporto",     "Reportó");
            dgvIncidencias.Columns.Add("Estado",      "Estado");
            dgvIncidencias.Columns.Add("Resolucion",  "Resolución");
            dgvIncidencias.Columns["Descripcion"].FillWeight = 220;
            dgvIncidencias.Columns["Resolucion"].FillWeight  = 170;
            dgvIncidencias.SelectionChanged += (s, e) => ActualizarBotonesIncidencia();

            var barraInc = new Panel { Dock = DockStyle.Top, Height = 46 };
            btnRegistrarInc = EstiloAdmin.Boton("+ Registrar incidencia",   8, 7, 170, 32, EstiloAdmin.Rojo);
            btnResolverInc  = EstiloAdmin.Boton("✓ Resolver seleccionada", 188, 7, 190, 32, EstiloAdmin.Verde);
            btnRegistrarInc.Click += BtnRegistrarInc_Click;
            btnResolverInc.Click  += BtnResolverInc_Click;
            barraInc.Controls.AddRange(new Control[] { btnRegistrarInc, btnResolverInc });

            tabIncidencias.Controls.Add(dgvIncidencias);
            tabIncidencias.Controls.Add(barraInc);   // agregado al final: queda arriba de la grilla
        }

        // ── Incidencias ─────────────────────────────────────────

        private static string TextoGravedad(int gravedad)
        {
            return gravedad == 1 ? "Alta" : gravedad == 2 ? "Media" : "Baja";
        }

        private void CargarIncidencias()
        {
            try
            {
                DataTable dt = _incRepo.ObtenerIncidencias(_orden.OrdenId);
                dgvIncidencias.Rows.Clear();

                int abiertas = 0;
                foreach (DataRow r in dt.Rows)
                {
                    bool resuelta = Convert.ToBoolean(r["resuelta"]);
                    if (!resuelta) abiertas++;

                    string resolucion = "";
                    if (resuelta)
                    {
                        string nota  = r["resolucion_nota"] == DBNull.Value ? "" : r["resolucion_nota"].ToString();
                        string fecha = r["fecha_resolucion"] == DBNull.Value
                            ? "" : Convert.ToDateTime(r["fecha_resolucion"]).ToString("dd/MM/yyyy");
                        resolucion = nota + (fecha == "" ? "" : "  (" + fecha + ")");
                    }

                    int idx = dgvIncidencias.Rows.Add(
                        Convert.ToDateTime(r["fecha_reporte"]).ToString("dd/MM/yyyy HH:mm"),
                        r["tipo"],
                        TextoGravedad(Convert.ToInt32(r["gravedad"])),
                        r["descripcion"],
                        r["reportada_por"],
                        resuelta ? "Resuelta" : "Abierta",
                        resolucion);

                    var fila = dgvIncidencias.Rows[idx];
                    fila.Tag = Convert.ToInt32(r["incidencia_id"]);
                    if (!resuelta) fila.DefaultCellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                }

                _orden.IncidenciasAbiertas = abiertas;
                tabIncidencias.Text = abiertas > 0 ? "⚠ Incidencias (" + abiertas + ")" : "⚠ Incidencias";
                ActualizarBotonesIncidencia();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron cargar las incidencias: " + ex);
                MessageBox.Show("No se pudieron cargar las incidencias.\n\n" + ex.Message,
                    "Incidencias", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ActualizarBotonesIncidencia()
        {
            // No se registran incidencias en órdenes canceladas (8) ni cerradas (10)
            bool ordenAbierta = _orden.EstatusId != 8 && _orden.EstatusId != 10;
            btnRegistrarInc.Visible = TieneRol(RolesQueRegistranIncidencias) && ordenAbierta;

            var fila = dgvIncidencias.CurrentRow;
            bool hayAbierta = fila != null && fila.Tag != null &&
                              Convert.ToString(fila.Cells["Estado"].Value) == "Abierta";
            btnResolverInc.Visible = TieneRol(RolesQueResuelvenIncidencias);
            btnResolverInc.Enabled = hayAbierta;
        }

        private void BtnRegistrarInc_Click(object sender, EventArgs e)
        {
            DataTable tipos;
            try
            {
                tipos = _incRepo.ObtenerTipos();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron cargar los tipos de incidencia: " + ex);
                MessageBox.Show("No se pudieron cargar los tipos de incidencia.\n\n" + ex.Message,
                    "Incidencias", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int tipoId;
            string descripcion;
            if (!PedirIncidencia(this, tipos, out tipoId, out descripcion)) return;

            try
            {
                _incRepo.Registrar(_orden.OrdenId, tipoId, descripcion);
                HuboCambios = true;
                CargarIncidencias();
                CargarDatos();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo registrar la incidencia: " + ex);
                MessageBox.Show(ex.Message, "No se pudo registrar la incidencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnResolverInc_Click(object sender, EventArgs e)
        {
            var fila = dgvIncidencias.CurrentRow;
            if (fila == null || fila.Tag == null) return;
            if (Convert.ToString(fila.Cells["Estado"].Value) != "Abierta") return;

            string nota;
            if (!PedirResolucion(this, out nota)) return;

            try
            {
                _incRepo.Resolver((int)fila.Tag, nota);
                HuboCambios = true;
                CargarIncidencias();
                CargarDatos();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo resolver la incidencia: " + ex);
                MessageBox.Show(ex.Message, "No se pudo resolver la incidencia",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Pide tipo y descripción de la incidencia. Devuelve false si se cancela.</summary>
        private static bool PedirIncidencia(IWin32Window owner, DataTable tipos, out int tipoId, out string descripcion)
        {
            tipoId = 0;
            descripcion = null;
            int    tipoElegido = 0;
            string textoElegido = null;

            using (var dlg = new Form())
            {
                dlg.Text            = "Registrar incidencia";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.ShowInTaskbar   = false;
                dlg.BackColor       = Color.White;
                dlg.ClientSize      = new Size(460, 330);

                var cbo = new ComboBox
                {
                    Location      = new Point(20, 34),
                    Size          = new Size(420, 28),
                    Font          = new Font("Segoe UI", 10),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DataSource    = tipos,
                    DisplayMember = "nombre",
                    ValueMember   = "tipo_id"
                };
                var txt = new TextBox
                {
                    Location    = new Point(20, 98),
                    Size        = new Size(420, 120),
                    Multiline   = true,
                    MaxLength   = 500,
                    ScrollBars  = ScrollBars.Vertical,
                    Font        = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle
                };
                var error = new Label
                {
                    Location  = new Point(20, 226),
                    Size      = new Size(420, 36),
                    ForeColor = EstiloAdmin.Rojo,
                    Font      = new Font("Segoe UI", 9)
                };
                var ok     = EstiloAdmin.Boton("Registrar", 250, 280, 110, 34, EstiloAdmin.Azul);
                var cancel = EstiloAdmin.BotonSecundario("Cancelar", 370, 280, 70, 34);
                cancel.DialogResult = DialogResult.Cancel;

                ok.Click += (s, e) =>
                {
                    if (cbo.SelectedValue == null) { error.Text = "Selecciona el tipo de incidencia."; return; }
                    if (string.IsNullOrWhiteSpace(txt.Text)) { error.Text = "Describe la incidencia."; return; }
                    tipoElegido  = Convert.ToInt32(cbo.SelectedValue);
                    textoElegido = txt.Text.Trim();
                    dlg.DialogResult = DialogResult.OK;
                };

                dlg.Controls.AddRange(new Control[]
                {
                    EstiloAdmin.Etiqueta("Tipo de incidencia",           20, 12), cbo,
                    EstiloAdmin.Etiqueta("Descripción (qué pasó, cuánto)", 20, 76), txt,
                    error, ok, cancel
                });
                dlg.AcceptButton = null;       // Enter en la descripción debe crear un renglón, no cerrar
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(owner) != DialogResult.OK) return false;
            }

            tipoId = tipoElegido;
            descripcion = textoElegido;
            return true;
        }

        /// <summary>Pide cómo se resolvió la incidencia. Devuelve false si se cancela.</summary>
        private static bool PedirResolucion(IWin32Window owner, out string nota)
        {
            nota = null;
            string textoElegido = null;

            using (var dlg = new Form())
            {
                dlg.Text            = "Resolver incidencia";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.ShowInTaskbar   = false;
                dlg.BackColor       = Color.White;
                dlg.ClientSize      = new Size(460, 280);

                var txt = new TextBox
                {
                    Location    = new Point(20, 34),
                    Size        = new Size(420, 120),
                    Multiline   = true,
                    MaxLength   = 500,
                    ScrollBars  = ScrollBars.Vertical,
                    Font        = new Font("Segoe UI", 10),
                    BorderStyle = BorderStyle.FixedSingle
                };
                var error = new Label
                {
                    Location  = new Point(20, 162),
                    Size      = new Size(420, 36),
                    ForeColor = EstiloAdmin.Rojo,
                    Font      = new Font("Segoe UI", 9)
                };
                var ok     = EstiloAdmin.Boton("Resolver", 250, 230, 110, 34, EstiloAdmin.Verde);
                var cancel = EstiloAdmin.BotonSecundario("Cancelar", 370, 230, 70, 34);
                cancel.DialogResult = DialogResult.Cancel;

                ok.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txt.Text)) { error.Text = "Describe cómo se resolvió."; return; }
                    textoElegido = txt.Text.Trim();
                    dlg.DialogResult = DialogResult.OK;
                };

                dlg.Controls.AddRange(new Control[]
                {
                    EstiloAdmin.Etiqueta("¿Cómo se resolvió?", 20, 12), txt, error, ok, cancel
                });
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(owner) != DialogResult.OK) return false;
            }

            nota = textoElegido;
            return true;
        }

        private void CargarDatos()
        {
            // ---- TAB INFO ----
            tabInfo.Controls.Clear();

            int y = 20;
            AgregarCampo(tabInfo, "Número de Orden:",   _orden.NumeroOrden, ref y);
            AgregarCampo(tabInfo, "Proveedor:",          _orden.ProveedorNombre, ref y);
            AgregarCampo(tabInfo, "Fecha de Emisión:",   _orden.FechaEmision.ToString("dd/MM/yyyy"), ref y);
            AgregarCampo(tabInfo, "Fecha Requerida:",    _orden.FechaRequerida.ToString("dd/MM/yyyy"), ref y);
            AgregarCampo(tabInfo, "Fecha Entrega Real:", _orden.FechaEntregaReal.HasValue
                                                         ? _orden.FechaEntregaReal.Value.ToString("dd/MM/yyyy")
                                                         : "Pendiente", ref y);
            AgregarCampo(tabInfo, "Estatus:",            _orden.EstatusNombre, ref y, _orden.EstatusColor);
            AgregarCampo(tabInfo, "Prioridad:",          _orden.PrioridadTexto, ref y,
                                                         _orden.Prioridad == 1 ? "#F44336" : null);
            AgregarCampo(tabInfo, "Cumplimiento:",       _orden.Cumplimiento, ref y);
            AgregarCampo(tabInfo, "Monto Total:",        $"{_orden.MontoTotal:N2} {_orden.Moneda}", ref y);
            AgregarCampo(tabInfo, "Creado por:",         _orden.CreadorNombre, ref y);
            AgregarCampo(tabInfo, "Días transcurridos:", _orden.DiasTranscurridos.ToString(), ref y);
            AgregarCampo(tabInfo, "Incidencias abiertas:", _orden.IncidenciasAbiertas.ToString(), ref y,
                                                          _orden.IncidenciasAbiertas > 0 ? "#F44336" : null);
            AgregarCampo(tabInfo, "Alertas no leídas:", _orden.AlertasNoLeidas.ToString(), ref y,
                                                         _orden.AlertasNoLeidas > 0 ? "#FF9800" : null);
            if (!string.IsNullOrEmpty(_orden.Observaciones))
                AgregarCampo(tabInfo, "Observaciones:", _orden.Observaciones, ref y);

            // ---- TAB HISTORIAL ----
            try
            {
                var dt = _repo.ObtenerHistorial(_orden.OrdenId);
                dgvHistorial.DataSource = dt;
                if (dgvHistorial.Columns.Count > 0)
                {
                    dgvHistorial.Columns["fecha_cambio"].HeaderText     = "Fecha";
                    dgvHistorial.Columns["estatus_anterior"].HeaderText  = "Estatus Anterior";
                    dgvHistorial.Columns["estatus_nuevo"].HeaderText     = "Estatus Nuevo";
                    dgvHistorial.Columns["usuario"].HeaderText           = "Usuario";
                    dgvHistorial.Columns["comentario"].HeaderText        = "Comentario";
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo cargar el historial: " + ex);
                MessageBox.Show("No se pudo cargar el historial de estatus.\n\n" + ex.Message,
                    "Historial", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AgregarCampo(Control parent, string etiqueta, string valor, ref int y, string colorHex = null)
        {
            var lblEtiqueta = new Label
            {
                Text      = etiqueta,
                Location  = new Point(20, y),
                Size      = new Size(170, 22),
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            var lblValor = new Label
            {
                Text      = valor,
                Location  = new Point(195, y),
                Size      = new Size(550, 22),
                Font      = new Font("Segoe UI", 9),
                ForeColor = colorHex != null ? ColorTranslator.FromHtml(colorHex) : Color.Black
            };
            parent.Controls.AddRange(new Control[] { lblEtiqueta, lblValor });
            y += 28;
        }

        // Muestra solo los estatus a los que se puede pasar desde el actual
        // (tablas EstatusTransiciones y EstatusTransicionRoles).
        private void CargarEstatusCombo()
        {
            try
            {
                int rolId = FrmLogin.UsuarioActual != null ? FrmLogin.UsuarioActual.RolId : 0;

                // Solo los cambios validos desde el estatus actual Y permitidos para el rol.
                // "nombre" es lo que se muestra (la accion, p. ej. "Aprobar y enviar al
                // proveedor"); "estatus_nombre" es el nombre real del estatus destino.
                var dt = DatabaseHelper.ExecuteQuery(
                    @"SELECT e.estatus_id,
                             e.nombre AS estatus_nombre,
                             COALESCE(t.accion, e.nombre) AS nombre
                      FROM EstatusTransiciones t
                      INNER JOIN EstatusTransicionRoles r
                              ON r.estatus_origen_id  = t.estatus_origen_id
                             AND r.estatus_destino_id = t.estatus_destino_id
                      INNER JOIN EstatusOrden e ON e.estatus_id = t.estatus_destino_id
                      WHERE t.estatus_origen_id = @origen
                        AND r.rol_id            = @rol
                      ORDER BY e.estatus_id",
                    new[]
                    {
                        new SqlParameter("@origen", _orden.EstatusId),
                        new SqlParameter("@rol",    rolId)
                    });

                cboNuevoEstatus.DataSource    = dt;
                cboNuevoEstatus.DisplayMember = "nombre";
                cboNuevoEstatus.ValueMember   = "estatus_id";

                bool puede       = PuedeCambiarEstatus();
                bool hayOpciones = dt.Rows.Count > 0;

                cboNuevoEstatus.Visible    = puede && hayOpciones;
                btnCambiarEstatus.Visible  = puede && hayOpciones;
                btnCambiarEstatus.Enabled  = true;
                lblEstatusFinal.Visible    = puede && !hayOpciones;
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo cargar el catálogo de estatus: " + ex);
                btnCambiarEstatus.Enabled = false;   // sin catálogo no se puede cambiar el estatus
                MessageBox.Show("No se pudo cargar los estatus disponibles.\n\n" + ex.Message,
                    "Estatus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnCambiarEstatus_Click(object sender, EventArgs e)
        {
            if (!PuedeCambiarEstatus())
            {
                MessageBox.Show("Su rol no tiene permiso para cambiar el estatus de una orden.",
                    "Sin permiso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboNuevoEstatus.SelectedItem == null) return;

            if (FrmLogin.UsuarioActual == null)
            {
                MessageBox.Show("La sesión expiró. Vuelva a iniciar sesión.", "Sesión",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var row            = ((DataRowView)cboNuevoEstatus.SelectedItem).Row;
            int nuevoId        = Convert.ToInt32(row["estatus_id"]);
            string nuevoNombre = row["estatus_nombre"].ToString();

            if (nuevoId == _orden.EstatusId)
            {
                MessageBox.Show("Seleccione un estatus diferente al actual.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Un solo paso: el diálogo confirma y captura el comentario. Cancelar aborta.
            string comentario;
            if (!PedirComentario(this, $"{_orden.EstatusNombre} → {nuevoNombre}", out comentario))
                return;

            try
            {
                _repo.CambiarEstatus(_orden.OrdenId, nuevoId,
                                     FrmLogin.UsuarioActual.UsuarioId, comentario);

                _orden.EstatusId     = nuevoId;
                _orden.EstatusNombre = nuevoNombre;
                HuboCambios          = true;
                CargarDatos();
                CargarEstatusCombo();

                MessageBox.Show("Estatus actualizado correctamente.", "Éxito",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error al cambiar estatus: " + ex);
                MessageBox.Show("No se pudo cambiar el estatus.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Diálogo de confirmación con comentario opcional.
        /// Devuelve false si el usuario cancela (a diferencia de InputBox, que
        /// devuelve "" tanto al cancelar como al dejar el campo vacío).
        /// </summary>
        private static bool PedirComentario(IWin32Window owner, string detalle, out string comentario)
        {
            comentario = null;

            using (var dlg = new Form())
            {
                dlg.Text            = "Cambio de Estatus";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.ShowInTaskbar   = false;
                dlg.ClientSize      = new Size(420, 200);

                var lbl = new Label
                {
                    Text     = "¿Confirmar el cambio de estatus?\n" + detalle + "\n\nComentario (opcional):",
                    Location = new Point(12, 10),
                    Size     = new Size(396, 62)
                };
                var txt = new TextBox
                {
                    Location   = new Point(12, 76),
                    Size       = new Size(396, 70),
                    Multiline  = true,
                    ScrollBars = ScrollBars.Vertical
                };
                var ok = new Button
                {
                    Text         = "Cambiar",
                    DialogResult = DialogResult.OK,
                    Location     = new Point(232, 160),
                    Size         = new Size(85, 28)
                };
                var cancel = new Button
                {
                    Text         = "Cancelar",
                    DialogResult = DialogResult.Cancel,
                    Location     = new Point(323, 160),
                    Size         = new Size(85, 28)
                };

                dlg.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(owner) != DialogResult.OK)
                    return false;

                comentario = string.IsNullOrWhiteSpace(txt.Text) ? null : txt.Text.Trim();
                return true;
            }
        }
    }
}
