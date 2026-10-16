using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
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

            tabControl.TabPages.AddRange(new[] { tabInfo, tabHistorial });
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
