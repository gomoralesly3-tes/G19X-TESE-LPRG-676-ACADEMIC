using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;

namespace MonitoreoOrdenes.Forms
{
    /// <summary>
    /// Formulario para crear una nueva Orden de Compra.
    /// Solo accesible para usuarios con rol Administrador o Comprador.
    /// </summary>
    public class FrmNuevaOrden : Form
    {
        // Controles cabecera
        private ComboBox   cboProveedor;
        private DateTimePicker dtpFechaRequerida;
        private ComboBox   cboPrioridad;
        private TextBox    txtObservaciones;
        private TextBox    txtNumeroOrden;

        // Grid de productos
        private DataGridView dgvProductos;
        private Button btnAgregarLinea, btnEliminarLinea;

        // Totales
        private Label lblTotal;

        // Botones
        private Button btnGuardar, btnCancelar;

        // Datos
        private DataTable _dtProductos;
        private DataTable _dtProveedores;
        private int        _proveedorActualId;   // proveedor cuyos productos estan cargados en la grilla

        // Roles que pueden crear ordenes: 1 Administrador, 2 Comprador.
        // Debe coincidir con sp_CrearOrdenCompra (la base de datos tambien lo valida).
        private static readonly int[] RolesQueCreanOrdenes = { 1, 2 };

        private static bool PuedeCrearOrdenes()
        {
            var usuario = FrmLogin.UsuarioActual;
            return usuario != null && Array.IndexOf(RolesQueCreanOrdenes, usuario.RolId) >= 0;
        }

        public FrmNuevaOrden()
        {
            InitializeComponent();
            CargarCatalogos();

            // El numero definitivo lo asigna la base de datos al guardar
            // (asi no se repite si dos usuarios crean ordenes a la vez).
            txtNumeroOrden.Text = "(automático)";

            this.Shown += (s, e) =>
            {
                if (!PuedeCrearOrdenes())
                {
                    MessageBox.Show("Su rol no tiene permiso para crear órdenes de compra.",
                        "Sin permiso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
            };
        }

        private void InitializeComponent()
        {
            this.Text            = "Nueva Orden de Compra";
            this.Size            = new Size(900, 680);
            this.StartPosition   = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.BackColor       = Color.FromArgb(245, 247, 250);

            // ── Header ──────────────────────────────────────────
            var panelHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 55,
                BackColor = Color.FromArgb(15, 52, 96)
            };
            panelHeader.Controls.Add(new Label
            {
                Text      = "Nueva Orden de Compra",
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(20, 14),
                AutoSize  = true
            });
            this.Controls.Add(panelHeader);

            // ── Panel formulario ─────────────────────────────────
            var panelForm = new Panel
            {
                Location  = new Point(0, 55),
                Size      = new Size(884, 530),
                BackColor = Color.White,
                Padding   = new Padding(20)
            };

            // Número de orden
            AgregarEtiqueta(panelForm, "Numero de Orden:", 20, 20);
            txtNumeroOrden = new TextBox
            {
                Location    = new Point(170, 18),
                Size        = new Size(150, 26),
                Font        = new Font("Segoe UI", 9),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(245, 247, 250),
                BorderStyle = BorderStyle.FixedSingle
            };
            panelForm.Controls.Add(txtNumeroOrden);

            // Proveedor
            AgregarEtiqueta(panelForm, "Proveedor:", 20, 56);
            cboProveedor = new ComboBox
            {
                Location      = new Point(170, 54),
                Size          = new Size(300, 26),
                Font          = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboProveedor.SelectionChangeCommitted += CboProveedor_SelectionChangeCommitted;
            panelForm.Controls.Add(cboProveedor);

            // Fecha requerida
            AgregarEtiqueta(panelForm, "Fecha Requerida:", 490, 56);
            dtpFechaRequerida = new DateTimePicker
            {
                Location = new Point(620, 54),
                Size     = new Size(200, 26),
                Font     = new Font("Segoe UI", 9),
                Format   = DateTimePickerFormat.Short,
                Value    = DateTime.Today.AddDays(7)
            };
            panelForm.Controls.Add(dtpFechaRequerida);

            // Prioridad
            AgregarEtiqueta(panelForm, "Prioridad:", 20, 94);
            cboPrioridad = new ComboBox
            {
                Location      = new Point(170, 92),
                Size          = new Size(150, 26),
                Font          = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboPrioridad.Items.AddRange(new object[] { "Alta", "Media", "Baja" });
            cboPrioridad.SelectedIndex = 1;
            panelForm.Controls.Add(cboPrioridad);

            // Observaciones
            AgregarEtiqueta(panelForm, "Observaciones:", 20, 132);
            txtObservaciones = new TextBox
            {
                Location    = new Point(170, 130),
                Size        = new Size(650, 26),
                Font        = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            txtObservaciones.MaxLength = 500;
            panelForm.Controls.Add(txtObservaciones);

            // Separador
            var sep = new Label
            {
                Location  = new Point(20, 168),
                Size      = new Size(840, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };
            panelForm.Controls.Add(sep);

            // Título líneas
            panelForm.Controls.Add(new Label
            {
                Text      = "Lineas de Productos",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 52, 96),
                Location  = new Point(20, 178),
                AutoSize  = true
            });

            // Botones líneas
            btnAgregarLinea = new Button
            {
                Text      = "+ Agregar Linea",
                Location  = new Point(620, 174),
                Size      = new Size(120, 28),
                BackColor = Color.FromArgb(16, 185, 129),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnAgregarLinea.FlatAppearance.BorderSize = 0;
            btnAgregarLinea.Click += BtnAgregarLinea_Click;
            panelForm.Controls.Add(btnAgregarLinea);

            btnEliminarLinea = new Button
            {
                Text      = "- Eliminar",
                Location  = new Point(748, 174),
                Size      = new Size(90, 28),
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9),
                Cursor    = Cursors.Hand
            };
            btnEliminarLinea.FlatAppearance.BorderSize = 0;
            btnEliminarLinea.Click += BtnEliminarLinea_Click;
            panelForm.Controls.Add(btnEliminarLinea);

            // Grid de productos
            ConstruirGridProductos(panelForm);

            // Total
            lblTotal = new Label
            {
                Text      = "Total: $0.00 MXN",
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 52, 96),
                Location  = new Point(600, 460),
                AutoSize  = true
            };
            panelForm.Controls.Add(lblTotal);

            this.Controls.Add(panelForm);

            // ── Botones footer ───────────────────────────────────
            var panelFooter = new Panel
            {
                Location  = new Point(0, 590),
                Size      = new Size(884, 55),
                BackColor = Color.FromArgb(248, 250, 252)
            };

            btnGuardar = new Button
            {
                Text      = "Guardar Orden",
                Location  = new Point(640, 12),
                Size      = new Size(140, 36),
                BackColor = Color.FromArgb(15, 52, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor    = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            btnCancelar = new Button
            {
                Text      = "Cancelar",
                Location  = new Point(790, 12),
                Size      = new Size(80, 36),
                BackColor = Color.FromArgb(245, 247, 250),
                ForeColor = Color.FromArgb(100, 116, 139),
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 10),
                Cursor    = Cursors.Hand
            };
            btnCancelar.FlatAppearance.BorderSize  = 1;
            btnCancelar.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            panelFooter.Controls.AddRange(new Control[] { btnGuardar, btnCancelar });
            this.Controls.Add(panelFooter);
        }

        private void AgregarEtiqueta(Panel parent, string texto, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text      = texto,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location  = new Point(x, y + 3),
                AutoSize  = true
            });
        }

        private void ConstruirGridProductos(Panel parent)
        {
            dgvProductos = new DataGridView
            {
                Location                    = new Point(20, 208),
                Size                        = new Size(840, 245),
                BackgroundColor             = Color.White,
                BorderStyle                 = BorderStyle.FixedSingle,
                AllowUserToAddRows          = false,
                AllowUserToDeleteRows       = false,
                AllowUserToResizeRows       = false,
                RowHeadersVisible           = false,
                SelectionMode               = DataGridViewSelectionMode.FullRowSelect,
                Font                        = new Font("Segoe UI", 9),
                GridColor                   = Color.FromArgb(226, 232, 240),
                RowTemplate                 = { Height = 34 },
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            dgvProductos.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor          = Color.FromArgb(15, 52, 96),
                ForeColor          = Color.White,
                Font               = new Font("Segoe UI", 9, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(15, 52, 96)
            };

            // Columnas
            dgvProductos.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name            = "Producto",
                HeaderText      = "Producto",
                Width           = 280,
                DisplayStyle    = DataGridViewComboBoxDisplayStyle.DropDownButton
            });

            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "Cantidad",
                HeaderText = "Cantidad",
                Width      = 100
            });

            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "PrecioUnitario",
                HeaderText = "Precio Unitario",
                Width      = 130
            });

            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "Subtotal",
                HeaderText = "Subtotal",
                Width      = 130,
                ReadOnly   = true
            });

            dgvProductos.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name       = "Unidad",
                HeaderText = "Unidad",
                Width      = 80,
                ReadOnly   = true
            });

            dgvProductos.CellValueChanged     += DgvProductos_CellValueChanged;
            dgvProductos.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvProductos.IsCurrentCellDirty)
                    dgvProductos.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            parent.Controls.Add(dgvProductos);
        }

        // ═══════════════════════════════════════════════════════
        // CATÁLOGOS
        // ═══════════════════════════════════════════════════════
        private void CargarCatalogos()
        {
            try
            {
                // Proveedores
                _dtProveedores = DatabaseHelper.ExecuteQuery(
                    "SELECT proveedor_id, nombre FROM Proveedores WHERE activo=1 ORDER BY nombre");
                cboProveedor.DataSource    = _dtProveedores;
                cboProveedor.DisplayMember = "nombre";
                cboProveedor.ValueMember   = "proveedor_id";

                // Productos del proveedor seleccionado (cada proveedor vende los suyos)
                CargarProductosDelProveedor();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catalogos:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Carga en el combo de la grilla SOLO los productos que vende el proveedor elegido,
        // con el precio pactado con ese proveedor (tabla ProveedorProductos).
        private void CargarProductosDelProveedor()
        {
            if (cboProveedor.SelectedItem == null) return;

            int proveedorId = Convert.ToInt32(((DataRowView)cboProveedor.SelectedItem)["proveedor_id"]);

            _dtProductos = DatabaseHelper.ExecuteQuery(
                @"SELECT p.producto_id, p.nombre, p.unidad_medida, pp.precio_pactado
                  FROM ProveedorProductos pp
                  INNER JOIN Productos p ON p.producto_id = pp.producto_id
                  WHERE pp.proveedor_id = @prov
                    AND pp.activo = 1
                    AND p.activo  = 1
                  ORDER BY p.nombre",
                new[] { new SqlParameter("@prov", proveedorId) });

            var col = (DataGridViewComboBoxColumn)dgvProductos.Columns["Producto"];
            col.DataSource    = _dtProductos;
            col.DisplayMember = "nombre";
            col.ValueMember   = "producto_id";

            _proveedorActualId = proveedorId;
        }

        // El usuario eligio otro proveedor: sus productos son distintos
        private void CboProveedor_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (cboProveedor.SelectedItem == null) return;

            int nuevoId = Convert.ToInt32(((DataRowView)cboProveedor.SelectedItem)["proveedor_id"]);
            if (nuevoId == _proveedorActualId) return;

            if (dgvProductos.Rows.Count > 0)
            {
                var respuesta = MessageBox.Show(
                    "Cada proveedor ofrece sus propios productos.\n" +
                    "Al cambiar de proveedor se quitarán las líneas actuales.\n\n¿Continuar?",
                    "Cambiar proveedor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (respuesta != DialogResult.Yes)
                {
                    cboProveedor.SelectedValue = _proveedorActualId;   // deja el proveedor anterior
                    return;
                }

                dgvProductos.Rows.Clear();
                CalcularTotal();
            }

            try
            {
                CargarProductosDelProveedor();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron cargar los productos del proveedor: " + ex);
                MessageBox.Show("No se pudieron cargar los productos del proveedor.\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // EVENTOS GRID
        // ═══════════════════════════════════════════════════════
        private void BtnAgregarLinea_Click(object sender, EventArgs e)
        {
            if (_dtProductos == null || _dtProductos.Rows.Count == 0)
            {
                Aviso("Este proveedor no tiene productos asignados. " +
                      "Pide que se registren en ProveedorProductos o elige otro proveedor.");
                return;
            }

            int idx = dgvProductos.Rows.Add();
            dgvProductos.Rows[idx].Cells["Cantidad"].Value        = "1";
            dgvProductos.Rows[idx].Cells["PrecioUnitario"].Value  = "0.00";
            dgvProductos.Rows[idx].Cells["Subtotal"].Value        = "$0.00";
        }

        private void BtnEliminarLinea_Click(object sender, EventArgs e)
        {
            if (dgvProductos.SelectedRows.Count > 0)
            {
                dgvProductos.Rows.Remove(dgvProductos.SelectedRows[0]);
                CalcularTotal();
            }
        }

        private void DgvProductos_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Subtotal y Unidad los escribe el propio programa: ignorarlos evita recursion
            string columna = dgvProductos.Columns[e.ColumnIndex].Name;
            if (columna == "Subtotal" || columna == "Unidad") return;

            var row = dgvProductos.Rows[e.RowIndex];

            // Si cambia el producto, cargar el precio pactado con el proveedor
            if (dgvProductos.Columns[e.ColumnIndex].Name == "Producto")
            {
                var prodId = row.Cells["Producto"].Value;
                if (prodId != null && _dtProductos != null)
                {
                    foreach (DataRow dr in _dtProductos.Rows)
                    {
                        if (dr["producto_id"].ToString() == prodId.ToString())
                        {
                            row.Cells["PrecioUnitario"].Value =
                                dr["precio_pactado"] != DBNull.Value
                                ? Convert.ToDecimal(dr["precio_pactado"]).ToString("F2")
                                : "0.00";
                            row.Cells["Unidad"].Value = dr["unidad_medida"].ToString();
                            break;
                        }
                    }
                }
            }

            // Recalcular subtotal
            decimal cant, precio;
            row.Cells["Subtotal"].Value = TryLinea(row, out cant, out precio)
                ? "$" + Math.Round(cant * precio, 2).ToString("N2")
                : "$0.00";

            CalcularTotal();
        }

        // Lee cantidad y precio de una fila respetando el formato regional
        private static bool TryLinea(DataGridViewRow row, out decimal cantidad, out decimal precio)
        {
            bool okCantidad = decimal.TryParse(Convert.ToString(row.Cells["Cantidad"].Value),
                NumberStyles.Number, CultureInfo.CurrentCulture, out cantidad);
            bool okPrecio = decimal.TryParse(Convert.ToString(row.Cells["PrecioUnitario"].Value),
                NumberStyles.Number, CultureInfo.CurrentCulture, out precio);
            return okCantidad && okPrecio;
        }

        private void CalcularTotal()
        {
            decimal total = 0;
            foreach (DataGridViewRow row in dgvProductos.Rows)
            {
                decimal cantidad, precio;
                if (TryLinea(row, out cantidad, out precio))
                    total += Math.Round(cantidad * precio, 2);
            }
            lblTotal.Text = "Total: $" + total.ToString("N2") + " MXN";
        }

        // ═══════════════════════════════════════════════════════
        // GUARDAR
        // ═══════════════════════════════════════════════════════
        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (!PuedeCrearOrdenes())
            {
                Aviso("Su rol no tiene permiso para crear órdenes de compra.");
                return;
            }

            dgvProductos.EndEdit();   // confirma la celda que se este editando

            // Validaciones
            if (cboProveedor.SelectedItem == null)
            {
                Aviso("Selecciona un proveedor.");
                return;
            }

            if (dgvProductos.Rows.Count == 0)
            {
                Aviso("Agrega al menos una línea de producto.");
                return;
            }

            // Lineas que se envian a la base de datos (el orden de columnas importa)
            var lineas = new DataTable();
            lineas.Columns.Add("producto_id",     typeof(int));
            lineas.Columns.Add("cantidad",        typeof(decimal));
            lineas.Columns.Add("precio_unitario", typeof(decimal));

            int numLinea = 0;
            foreach (DataGridViewRow row in dgvProductos.Rows)
            {
                numLinea++;

                if (row.Cells["Producto"].Value == null)
                {
                    Aviso("Línea " + numLinea + ": selecciona un producto.");
                    return;
                }

                decimal cantidad, precio;
                if (!TryLinea(row, out cantidad, out precio))
                {
                    Aviso("Línea " + numLinea + ": la cantidad o el precio no son números válidos.");
                    return;
                }
                if (cantidad <= 0)
                {
                    Aviso("Línea " + numLinea + ": la cantidad debe ser mayor a 0.");
                    return;
                }
                if (precio < 0)
                {
                    Aviso("Línea " + numLinea + ": el precio no puede ser negativo.");
                    return;
                }

                // Se redondea al tamano de las columnas (3 y 2 decimales)
                lineas.Rows.Add(Convert.ToInt32(row.Cells["Producto"].Value),
                                Math.Round(cantidad, 3), Math.Round(precio, 2));
            }

            try
            {
                btnGuardar.Enabled = false;
                btnGuardar.Text    = "Guardando...";

                int proveedorId = Convert.ToInt32(
                    ((DataRowView)cboProveedor.SelectedItem)["proveedor_id"]);

                var parametros = new[]
                {
                    new SqlParameter("@proveedor_id",    proveedorId),
                    new SqlParameter("@usuario_id",      FrmLogin.UsuarioActual.UsuarioId),
                    new SqlParameter("@fecha_requerida", dtpFechaRequerida.Value.Date),
                    new SqlParameter("@prioridad",       (byte)(cboPrioridad.SelectedIndex + 1)), // 1=Alta 2=Media 3=Baja
                    new SqlParameter("@observaciones",   string.IsNullOrWhiteSpace(txtObservaciones.Text)
                                                             ? (object)DBNull.Value
                                                             : txtObservaciones.Text.Trim()),
                    new SqlParameter("@lineas", SqlDbType.Structured)
                    {
                        TypeName = "dbo.TipoLineaOrden",
                        Value    = lineas
                    }
                };

                // Cabecera, lineas, historial y numero de orden: todo en una transaccion
                var resultado = DatabaseHelper.ExecuteProcedure("sp_CrearOrdenCompra", parametros);
                string numeroOrden = resultado.Rows[0]["numero_orden"].ToString();

                MessageBox.Show(
                    "Orden " + numeroOrden + " creada correctamente.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error al guardar la orden: " + ex);
                MessageBox.Show("No se pudo guardar la orden.\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGuardar.Enabled = true;
                btnGuardar.Text    = "Guardar Orden";
            }
        }

        private static void Aviso(string mensaje)
        {
            MessageBox.Show(mensaje, "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
