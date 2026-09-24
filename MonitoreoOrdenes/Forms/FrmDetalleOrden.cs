using System;
using System.Data;
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

            this.Controls.AddRange(new Control[] { btnCambiarEstatus, cboNuevoEstatus, btnCerrar });

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
            catch { /* El historial es opcional */ }
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

        private void CargarEstatusCombo()
        {
            try
            {
                var dt = DatabaseHelper.ExecuteQuery(
                    "SELECT estatus_id, nombre FROM EstatusOrden ORDER BY estatus_id");
                cboNuevoEstatus.DataSource    = dt;
                cboNuevoEstatus.DisplayMember = "nombre";
                cboNuevoEstatus.ValueMember   = "estatus_id";

                // Seleccionar estatus actual
                foreach (DataRowView item in cboNuevoEstatus.Items)
                    if (Convert.ToInt32(item["estatus_id"]) == _orden.EstatusId)
                    {
                        cboNuevoEstatus.SelectedItem = item;
                        break;
                    }
            }
            catch { /* Continuar sin combo */ }
        }

        private void BtnCambiarEstatus_Click(object sender, EventArgs e)
        {
            if (cboNuevoEstatus.SelectedItem == null) return;

            var row        = ((DataRowView)cboNuevoEstatus.SelectedItem).Row;
            int nuevoId    = Convert.ToInt32(row["estatus_id"]);
            string nuevoNombre = row["nombre"].ToString();

            if (nuevoId == _orden.EstatusId)
            {
                MessageBox.Show("Seleccione un estatus diferente al actual.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string comentario = Microsoft.VisualBasic.Interaction.InputBox(
                $"Ingrese comentario para el cambio:\n{_orden.EstatusNombre} → {nuevoNombre}",
                "Cambio de Estatus", "");

            if (MessageBox.Show(
                $"¿Cambiar estatus a '{nuevoNombre}'?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    _repo.CambiarEstatus(_orden.OrdenId, nuevoId,
                                         FrmLogin.UsuarioActual.UsuarioId, comentario);
                    MessageBox.Show("Estatus actualizado correctamente.", "Éxito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _orden.EstatusId    = nuevoId;
                    _orden.EstatusNombre= nuevoNombre;
                    CargarDatos();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
