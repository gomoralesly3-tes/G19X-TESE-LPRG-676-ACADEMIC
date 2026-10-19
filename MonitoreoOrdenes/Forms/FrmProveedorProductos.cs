using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;

namespace MonitoreoOrdenes.Forms
{
    /// <summary>
    /// Define qué productos vende cada proveedor, con su precio pactado y días de entrega.
    /// Solo para el rol Administrador (1). Al crear una orden, cada proveedor ofrece únicamente
    /// los productos marcados aquí.
    /// </summary>
    public class FrmProveedorProductos : Form
    {
        private readonly CatalogoRepository _repo = new CatalogoRepository();

        private ComboBox     cboProveedor;
        private DataGridView dgv;
        private Button       btnGuardar, btnCerrar;
        private Label        lblEstado;

        private int  _proveedorId;      // proveedor cuyos productos se muestran
        private bool _cargando;         // true mientras se llena la grilla
        private bool _sinGuardar;       // hay cambios pendientes

        private static int AdminId
        {
            get { var u = FrmLogin.UsuarioActual; return u == null ? 0 : u.UsuarioId; }
        }

        private static bool EsAdministrador()
        {
            var u = FrmLogin.UsuarioActual;
            return u != null && u.RolId == 1;
        }

        public FrmProveedorProductos()
        {
            this.Text          = "Productos por proveedor";
            this.Size          = new Size(1040, 640);
            this.MinimumSize   = new Size(940, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor     = EstiloAdmin.Fondo;

            // Selector de proveedor
            var barra = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Color.White };
            barra.Controls.Add(EstiloAdmin.Etiqueta("Proveedor:", 20, 14));
            cboProveedor = new ComboBox
            {
                Location      = new Point(100, 10),
                Size          = new Size(420, 28),
                Font          = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboProveedor.SelectionChangeCommitted += CboProveedor_SelectionChangeCommitted;
            barra.Controls.Add(cboProveedor);
            barra.Controls.Add(new Label
            {
                Text      = "Marca los productos que vende este proveedor y define su precio pactado. " +
                            "Al crear una orden, el proveedor solo ofrecerá estos productos.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = EstiloAdmin.Gris,
                Location  = new Point(20, 48),
                Size      = new Size(980, 30)
            });

            // Grilla
            dgv = new DataGridView
            {
                Dock        = DockStyle.Fill,
                MultiSelect = false,
                EditMode    = DataGridViewEditMode.EditOnEnter
            };
            EstiloAdmin.EstiloGrid(dgv);
            dgv.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Vende", HeaderText = "Vende", Width = 60 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Codigo",        HeaderText = "Código",             Width = 90,  ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Producto",      HeaderText = "Producto",           AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Categoria",     HeaderText = "Categoría",          Width = 160, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Unidad",        HeaderText = "Unidad",             Width = 80,  ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioRef",     HeaderText = "Precio de referencia", Width = 130, ReadOnly = true });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PrecioPactado", HeaderText = "Precio pactado",     Width = 120 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dias",          HeaderText = "Días de entrega",    Width = 110 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductoId",    Visible = false });
            dgv.Columns["PrecioRef"].DefaultCellStyle.Alignment     = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["PrecioPactado"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgv.Columns["Dias"].DefaultCellStyle.Alignment          = DataGridViewContentAlignment.MiddleCenter;

            dgv.CurrentCellDirtyStateChanged += (s, e) =>
            {
                // El casillero se confirma al instante (sin esperar a salir de la celda)
                if (dgv.IsCurrentCellDirty && dgv.CurrentCell is DataGridViewCheckBoxCell)
                    dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgv.CellValueChanged += Dgv_CellValueChanged;

            // Pie
            var pie = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
            lblEstado = new Label
            {
                Location  = new Point(20, 19),
                AutoSize  = true,
                ForeColor = EstiloAdmin.Gris,
                Font      = new Font("Segoe UI", 9)
            };
            btnGuardar = EstiloAdmin.Boton("Guardar cambios", 780, 12, 140, 34, EstiloAdmin.Azul);
            btnCerrar  = EstiloAdmin.BotonSecundario("Cerrar", 930, 12, 80, 34);
            btnGuardar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCerrar.Anchor  = AnchorStyles.Top | AnchorStyles.Right;
            btnGuardar.Click += (s, e) => Guardar();
            btnCerrar.Click  += (s, e) => this.Close();
            pie.Controls.AddRange(new Control[] { lblEstado, btnGuardar, btnCerrar });

            this.Controls.Add(dgv);
            this.Controls.Add(pie);
            this.Controls.Add(barra);
            this.Controls.Add(EstiloAdmin.Encabezado("Productos por proveedor"));

            this.FormClosing += (s, e) =>
            {
                if (_sinGuardar && !ConfirmarDescartar())
                    e.Cancel = true;
            };

            this.Shown += (s, e) =>
            {
                if (!EsAdministrador())
                {
                    MessageBox.Show("Solo el Administrador puede administrar los productos de cada proveedor.",
                        "Sin permiso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _sinGuardar = false;
                    this.Close();
                    return;
                }
                CargarProveedores();
            };
        }

        private void CargarProveedores()
        {
            try
            {
                DataTable dt = _repo.ObtenerProveedoresActivos();

                // Texto visible: "PROV-001 - Nombre"
                dt.Columns.Add("etiqueta", typeof(string));
                foreach (DataRow r in dt.Rows)
                    r["etiqueta"] = r["codigo"] + " - " + r["nombre"];

                cboProveedor.DataSource    = dt;
                cboProveedor.DisplayMember = "etiqueta";
                cboProveedor.ValueMember   = "proveedor_id";

                if (dt.Rows.Count == 0)
                {
                    lblEstado.Text = "No hay proveedores activos.";
                    return;
                }
                CargarProductos(Convert.ToInt32(dt.Rows[0]["proveedor_id"]));
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron cargar los proveedores: " + ex);
                MessageBox.Show("No se pudieron cargar los proveedores.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarProductos(int proveedorId)
        {
            try
            {
                DataTable dt = _repo.ObtenerProductosDeProveedor(proveedorId);

                _cargando = true;
                dgv.Rows.Clear();

                foreach (DataRow r in dt.Rows)
                {
                    int idx = dgv.Rows.Add(
                        Convert.ToBoolean(r["vende"]),
                        r["codigo"],
                        r["nombre"],
                        r["categoria"] == DBNull.Value ? "—" : r["categoria"],
                        r["unidad_medida"],
                        r["precio_ref"] == DBNull.Value ? "" : Convert.ToDecimal(r["precio_ref"]).ToString("N2"),
                        r["precio_pactado"] == DBNull.Value ? "" : Convert.ToDecimal(r["precio_pactado"]).ToString("N2"),
                        r["tiempo_entrega_dias"] == DBNull.Value ? "" : r["tiempo_entrega_dias"].ToString(),
                        r["producto_id"]);
                    AplicarEstilo(dgv.Rows[idx]);
                }

                _cargando    = false;
                _proveedorId = proveedorId;
                _sinGuardar  = false;
                ActualizarEstado();
            }
            catch (Exception ex)
            {
                _cargando = false;
                Trace.TraceError("No se pudieron cargar los productos: " + ex);
                MessageBox.Show("No se pudieron cargar los productos.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Los que no vende el proveedor se ven en gris y sin precio editable
        private void AplicarEstilo(DataGridViewRow fila)
        {
            bool vende = Convert.ToBoolean(fila.Cells["Vende"].Value);
            fila.DefaultCellStyle.ForeColor     = vende ? Color.Black : EstiloAdmin.Gris;
            fila.Cells["PrecioPactado"].ReadOnly = !vende;
            fila.Cells["Dias"].ReadOnly          = !vende;
        }

        private void Dgv_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_cargando || e.RowIndex < 0) return;

            var fila = dgv.Rows[e.RowIndex];

            if (dgv.Columns[e.ColumnIndex].Name == "Vende")
            {
                bool vende = Convert.ToBoolean(fila.Cells["Vende"].Value);
                AplicarEstilo(fila);

                // Al marcarlo por primera vez, propone el precio de referencia
                if (vende && string.IsNullOrWhiteSpace(Convert.ToString(fila.Cells["PrecioPactado"].Value)))
                    fila.Cells["PrecioPactado"].Value = fila.Cells["PrecioRef"].Value;
            }

            _sinGuardar = true;
            ActualizarEstado();
        }

        private void ActualizarEstado()
        {
            int vende = 0;
            foreach (DataGridViewRow f in dgv.Rows)
                if (Convert.ToBoolean(f.Cells["Vende"].Value)) vende++;

            lblEstado.Text = vende + " de " + dgv.Rows.Count + " productos asignados" +
                             (_sinGuardar ? "   —   hay cambios sin guardar" : "");
            lblEstado.ForeColor = _sinGuardar ? EstiloAdmin.Rojo : EstiloAdmin.Gris;
        }

        private bool ConfirmarDescartar()
        {
            return MessageBox.Show("Hay cambios sin guardar. ¿Descartarlos?", "Cambios sin guardar",
                       MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        private void CboProveedor_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cboProveedor.SelectedValue == null) return;
            int nuevoId = Convert.ToInt32(cboProveedor.SelectedValue);
            if (nuevoId == _proveedorId) return;

            if (_sinGuardar && !ConfirmarDescartar())
            {
                cboProveedor.SelectedValue = _proveedorId;   // se queda en el proveedor anterior
                return;
            }
            CargarProductos(nuevoId);
        }

        private void Guardar()
        {
            dgv.EndEdit();

            // Orden de columnas = orden del tipo dbo.TipoProveedorProducto
            var items = new DataTable();
            items.Columns.Add("producto_id",         typeof(int));
            items.Columns.Add("vende",               typeof(bool));
            items.Columns.Add("precio_pactado",      typeof(decimal));
            items.Columns.Add("tiempo_entrega_dias", typeof(int));

            foreach (DataGridViewRow fila in dgv.Rows)
            {
                int    id     = Convert.ToInt32(fila.Cells["ProductoId"].Value);
                string nombre = Convert.ToString(fila.Cells["Producto"].Value);
                bool   vende  = Convert.ToBoolean(fila.Cells["Vende"].Value);

                if (!vende)
                {
                    items.Rows.Add(id, false, DBNull.Value, DBNull.Value);
                    continue;
                }

                decimal precio;
                if (!decimal.TryParse(Convert.ToString(fila.Cells["PrecioPactado"].Value),
                        NumberStyles.Number, CultureInfo.CurrentCulture, out precio) || precio < 0)
                {
                    Rechazar(fila, "PrecioPactado", "\"" + nombre + "\": el precio pactado debe ser un número (0 o mayor).");
                    return;
                }

                object dias = DBNull.Value;
                string textoDias = (Convert.ToString(fila.Cells["Dias"].Value) ?? "").Trim();
                if (textoDias != "")
                {
                    int d;
                    if (!int.TryParse(textoDias, out d) || d <= 0)
                    {
                        Rechazar(fila, "Dias", "\"" + nombre + "\": los días de entrega deben ser un número entero mayor a 0, o dejarse vacíos.");
                        return;
                    }
                    dias = d;
                }

                items.Rows.Add(id, true, Math.Round(precio, 2), dias);
            }

            try
            {
                btnGuardar.Enabled = false;
                _repo.GuardarProductosDeProveedor(AdminId, _proveedorId, items);
                CargarProductos(_proveedorId);   // recarga y deja el estado "sin cambios"
                MessageBox.Show("Los productos del proveedor se guardaron correctamente.", "Guardado",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron guardar los productos del proveedor: " + ex);
                MessageBox.Show("No se pudo guardar.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGuardar.Enabled = true;
            }
        }

        private void Rechazar(DataGridViewRow fila, string columna, string mensaje)
        {
            MessageBox.Show(mensaje, "Revisa los datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            dgv.CurrentCell = fila.Cells[columna];
            dgv.BeginEdit(true);
        }
    }
}
