using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;

namespace MonitoreoOrdenes.Forms
{
    /// <summary>Administración de usuarios. Solo para el rol Administrador (1).</summary>
    public class FrmUsuarios : Form
    {
        private readonly UsuarioRepository _repo = new UsuarioRepository();

        private DataGridView dgv;
        private Label        lblContador;
        private Button       btnNuevo, btnEditar, btnClave, btnEstado;

        private static int AdminId
        {
            get { var u = FrmLogin.UsuarioActual; return u == null ? 0 : u.UsuarioId; }
        }

        private static bool EsAdministrador()
        {
            var u = FrmLogin.UsuarioActual;
            return u != null && u.RolId == 1;
        }

        private DataRow Seleccionado
        {
            get { return dgv.CurrentRow == null ? null : dgv.CurrentRow.Tag as DataRow; }
        }

        public FrmUsuarios()
        {
            this.Text          = "Administración de usuarios";
            this.Size          = new Size(1040, 620);
            this.MinimumSize   = new Size(900, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor     = EstiloAdmin.Fondo;

            // Barra de botones
            var barra = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            btnNuevo  = EstiloAdmin.Boton("+ Nuevo usuario",          20, 11, 140, 34, EstiloAdmin.Verde);
            btnEditar = EstiloAdmin.Boton("Editar",                  170, 11,  90, 34, EstiloAdmin.Azul);
            btnClave  = EstiloAdmin.Boton("Restablecer contraseña",  270, 11, 180, 34, EstiloAdmin.Azul);
            btnEstado = EstiloAdmin.Boton("Desactivar",              460, 11, 120, 34, EstiloAdmin.Rojo);
            btnNuevo.Click  += (s, e) => Nuevo();
            btnEditar.Click += (s, e) => Editar();
            btnClave.Click  += (s, e) => RestablecerClave();
            btnEstado.Click += (s, e) => CambiarEstado();
            barra.Controls.AddRange(new Control[] { btnNuevo, btnEditar, btnClave, btnEstado });

            // Grilla
            dgv = new DataGridView
            {
                Dock          = DockStyle.Fill,
                ReadOnly      = true,
                MultiSelect   = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            EstiloAdmin.EstiloGrid(dgv);
            dgv.Columns.Add("Nombre",  "Nombre");
            dgv.Columns.Add("Usuario", "Usuario");
            dgv.Columns.Add("Correo",  "Correo");
            dgv.Columns.Add("Rol",     "Rol");
            dgv.Columns.Add("Estado",  "Estado");
            dgv.Columns.Add("Acceso",  "Último acceso");
            dgv.Columns.Add("Creado",  "Creado");
            dgv.SelectionChanged += (s, e) => ActualizarBotones();
            dgv.CellDoubleClick  += (s, e) => { if (e.RowIndex >= 0) Editar(); };

            lblContador = new Label
            {
                Dock      = DockStyle.Bottom,
                Height    = 30,
                ForeColor = EstiloAdmin.Gris,
                Font      = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.White
            };

            // El control agregado al final queda arriba del todo
            this.Controls.Add(dgv);
            this.Controls.Add(lblContador);
            this.Controls.Add(barra);
            this.Controls.Add(EstiloAdmin.Encabezado("Administración de usuarios"));

            this.Shown += (s, e) =>
            {
                if (!EsAdministrador())
                {
                    MessageBox.Show("Solo el Administrador puede administrar usuarios.", "Sin permiso",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                    return;
                }
                Cargar();
            };
        }

        private void Cargar(int? seleccionarId = null)
        {
            try
            {
                DataTable dt = _repo.ListarUsuarios(AdminId);
                dgv.Rows.Clear();

                foreach (DataRow r in dt.Rows)
                {
                    bool activo = Convert.ToBoolean(r["activo"]);
                    int idx = dgv.Rows.Add(
                        r["nombre"] + " " + r["apellido"],
                        r["username"],
                        r["email"],
                        r["rol_nombre"],
                        activo ? "Activo" : "Inactivo",
                        r["ultimo_acceso"] == DBNull.Value
                            ? "Nunca"
                            : Convert.ToDateTime(r["ultimo_acceso"]).ToString("dd/MM/yyyy HH:mm"),
                        Convert.ToDateTime(r["fecha_creacion"]).ToString("dd/MM/yyyy"));

                    var fila = dgv.Rows[idx];
                    fila.Tag = r;
                    if (!activo) fila.DefaultCellStyle.ForeColor = EstiloAdmin.Gris;

                    if (seleccionarId.HasValue && Convert.ToInt32(r["usuario_id"]) == seleccionarId.Value)
                        dgv.CurrentCell = fila.Cells[0];
                }

                lblContador.Text = "   " + dt.Rows.Count + " usuario(s)";
                ActualizarBotones();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudieron cargar los usuarios: " + ex);
                MessageBox.Show("No se pudieron cargar los usuarios.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ActualizarBotones()
        {
            DataRow fila = Seleccionado;
            bool hay = fila != null;
            btnEditar.Enabled = btnClave.Enabled = btnEstado.Enabled = hay;

            if (hay)
            {
                bool activo = Convert.ToBoolean(fila["activo"]);
                btnEstado.Text      = activo ? "Desactivar" : "Activar";
                btnEstado.BackColor = activo ? EstiloAdmin.Rojo : EstiloAdmin.Verde;
            }
        }

        private void Nuevo()
        {
            using (var dlg = new FrmUsuarioEditor(AdminId, null))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Cargar(dlg.UsuarioIdGuardado);
            }
        }

        private void Editar()
        {
            DataRow fila = Seleccionado;
            if (fila == null) return;

            using (var dlg = new FrmUsuarioEditor(AdminId, fila))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Cargar(dlg.UsuarioIdGuardado);
            }
        }

        private void RestablecerClave()
        {
            DataRow fila = Seleccionado;
            if (fila == null) return;

            string nombre = fila["nombre"] + " " + fila["apellido"];
            string clave;
            if (!PedirClaveNueva(this, nombre, out clave)) return;

            try
            {
                _repo.RestablecerPassword(AdminId, Convert.ToInt32(fila["usuario_id"]), clave);
                MessageBox.Show(
                    "Contraseña restablecida para " + nombre + ".\n\n" +
                    "Entrégasela por un medio seguro. Puede cambiarla desde 'Cambiar contraseña'.",
                    "Contraseña", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo restablecer la contraseña: " + ex);
                MessageBox.Show("No se pudo restablecer la contraseña.\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CambiarEstado()
        {
            DataRow fila = Seleccionado;
            if (fila == null) return;

            bool activo   = Convert.ToBoolean(fila["activo"]);
            string nombre = fila["nombre"] + " " + fila["apellido"];

            string pregunta = activo
                ? "¿Desactivar a " + nombre + "?\n\nYa no podrá iniciar sesión."
                : "¿Activar a " + nombre + "?\n\nPodrá volver a iniciar sesión.";

            if (MessageBox.Show(pregunta, "Confirmar", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                int id = Convert.ToInt32(fila["usuario_id"]);
                _repo.GuardarUsuario(AdminId, id,
                    fila["nombre"].ToString(), fila["apellido"].ToString(),
                    fila["email"].ToString(), fila["username"].ToString(),
                    Convert.ToInt32(fila["rol_id"]), !activo, null);
                Cargar(id);
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo cambiar el estado del usuario: " + ex);
                MessageBox.Show(ex.Message, "No se pudo completar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>Pide y valida la contraseña nueva. Devuelve false si se cancela.</summary>
        private static bool PedirClaveNueva(IWin32Window owner, string usuario, out string clave)
        {
            clave = null;
            string resultado = null;

            using (var dlg = new Form())
            {
                dlg.Text            = "Restablecer contraseña";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition   = FormStartPosition.CenterParent;
                dlg.MaximizeBox     = false;
                dlg.MinimizeBox     = false;
                dlg.ShowInTaskbar   = false;
                dlg.BackColor       = Color.White;
                dlg.ClientSize      = new Size(400, 270);

                var titulo = new Label
                {
                    Text      = "Nueva contraseña para: " + usuario,
                    Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = EstiloAdmin.Azul,
                    Location  = new Point(20, 15),
                    Size      = new Size(360, 22)
                };
                var txt1 = EstiloAdmin.Caja(20,  88, 360, true);
                var txt2 = EstiloAdmin.Caja(20, 148, 360, true);
                var hint = new Label
                {
                    Text      = "Mínimo 8 caracteres, con al menos una letra y un número.",
                    Font      = new Font("Segoe UI", 8.5f),
                    ForeColor = EstiloAdmin.Gris,
                    Location  = new Point(20, 42),
                    AutoSize  = true
                };
                var error = new Label
                {
                    Location  = new Point(20, 182),
                    Size      = new Size(360, 34),
                    ForeColor = EstiloAdmin.Rojo,
                    Font      = new Font("Segoe UI", 9)
                };
                var ok     = EstiloAdmin.Boton("Restablecer", 170, 224, 120, 34, EstiloAdmin.Azul);
                var cancel = EstiloAdmin.BotonSecundario("Cancelar", 300, 224, 80, 34);
                cancel.DialogResult = DialogResult.Cancel;

                ok.Click += (s, e) =>
                {
                    string motivo;
                    if (!SecurityHelper.EsPasswordValida(txt1.Text, out motivo)) { error.Text = motivo; return; }
                    if (txt1.Text != txt2.Text) { error.Text = "Las contraseñas no coinciden."; return; }
                    resultado = txt1.Text;
                    dlg.DialogResult = DialogResult.OK;
                };

                dlg.Controls.AddRange(new Control[]
                {
                    titulo, hint,
                    EstiloAdmin.Etiqueta("Nueva contraseña",     20, 65),
                    txt1,
                    EstiloAdmin.Etiqueta("Confirmar contraseña", 20, 125),
                    txt2, error, ok, cancel
                });
                dlg.AcceptButton = ok;
                dlg.CancelButton = cancel;

                if (dlg.ShowDialog(owner) != DialogResult.OK) return false;
            }

            clave = resultado;
            return true;
        }
    }

    /// <summary>Alta y edición de un usuario (fila == null = usuario nuevo).</summary>
    internal class FrmUsuarioEditor : Form
    {
        private readonly UsuarioRepository _repo = new UsuarioRepository();
        private readonly int     _adminId;
        private readonly DataRow _fila;

        private TextBox  txtNombre, txtApellido, txtCorreo, txtUsuario, txtClave, txtConfirmar;
        private ComboBox cboRol;
        private CheckBox chkActivo;
        private Label    lblError;
        private Button   btnGuardar, btnCancelar;

        public int? UsuarioIdGuardado { get; private set; }

        private bool EsNuevo { get { return _fila == null; } }

        public FrmUsuarioEditor(int adminId, DataRow fila)
        {
            _adminId = adminId;
            _fila    = fila;

            this.Text            = EsNuevo ? "Nuevo usuario" : "Editar usuario";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.ShowInTaskbar   = false;
            this.BackColor       = Color.White;
            this.ClientSize      = new Size(440, EsNuevo ? 600 : 480);

            txtNombre   = EstiloAdmin.Caja(30,  92, 380);
            txtApellido = EstiloAdmin.Caja(30, 150, 380);
            txtCorreo   = EstiloAdmin.Caja(30, 208, 380);
            txtUsuario  = EstiloAdmin.Caja(30, 266, 380);

            cboRol = new ComboBox
            {
                Location      = new Point(30, 324),
                Size          = new Size(380, 28),
                Font          = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            DataTable roles = _repo.ObtenerRoles();
            cboRol.DataSource    = roles;
            cboRol.DisplayMember = "nombre";
            cboRol.ValueMember   = "rol_id";

            chkActivo = new CheckBox
            {
                Text     = "Usuario activo (puede iniciar sesión)",
                Font     = new Font("Segoe UI", 9),
                Location = new Point(30, 366),
                AutoSize = true,
                Checked  = true
            };

            this.Controls.Add(EstiloAdmin.Encabezado(EsNuevo ? "Nuevo usuario" : "Editar usuario"));
            this.Controls.AddRange(new Control[]
            {
                EstiloAdmin.Etiqueta("Nombre",   30,  69), txtNombre,
                EstiloAdmin.Etiqueta("Apellido", 30, 127), txtApellido,
                EstiloAdmin.Etiqueta("Correo",   30, 185), txtCorreo,
                EstiloAdmin.Etiqueta("Usuario (para iniciar sesión)", 30, 243), txtUsuario,
                EstiloAdmin.Etiqueta("Rol",      30, 301), cboRol,
                chkActivo
            });

            int yError, yBotones;
            if (EsNuevo)
            {
                txtClave     = EstiloAdmin.Caja(30, 436, 380, true);
                txtConfirmar = EstiloAdmin.Caja(30, 494, 380, true);
                this.Controls.AddRange(new Control[]
                {
                    EstiloAdmin.Etiqueta("Contraseña (mínimo 8, con letra y número)", 30, 413), txtClave,
                    EstiloAdmin.Etiqueta("Confirmar contraseña", 30, 471), txtConfirmar
                });
                yError = 526; yBotones = 556;
            }
            else
            {
                // El nombre de usuario no se cambia: es con el que la persona inicia sesión
                txtUsuario.ReadOnly  = true;
                txtUsuario.BackColor = EstiloAdmin.Fondo;
                txtNombre.Text   = _fila["nombre"].ToString();
                txtApellido.Text = _fila["apellido"].ToString();
                txtCorreo.Text   = _fila["email"].ToString();
                txtUsuario.Text  = _fila["username"].ToString();
                cboRol.SelectedValue = Convert.ToInt32(_fila["rol_id"]);
                chkActivo.Checked    = Convert.ToBoolean(_fila["activo"]);
                yError = 400; yBotones = 436;
            }

            lblError = new Label
            {
                Location  = new Point(30, yError),
                Size      = new Size(380, 28),
                ForeColor = EstiloAdmin.Rojo,
                Font      = new Font("Segoe UI", 9)
            };
            btnGuardar  = EstiloAdmin.Boton("Guardar", 230, yBotones, 100, 34, EstiloAdmin.Azul);
            btnCancelar = EstiloAdmin.BotonSecundario("Cancelar", 340, yBotones, 80, 34);
            btnCancelar.DialogResult = DialogResult.Cancel;
            btnGuardar.Click += BtnGuardar_Click;
            this.Controls.AddRange(new Control[] { lblError, btnGuardar, btnCancelar });

            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            lblError.Text = "";

            string nombre   = txtNombre.Text.Trim();
            string apellido = txtApellido.Text.Trim();
            string correo   = txtCorreo.Text.Trim();
            string usuario  = txtUsuario.Text.Trim();

            if (nombre == "" || apellido == "" || correo == "" || usuario == "")
            {
                lblError.Text = "Nombre, apellido, correo y usuario son obligatorios.";
                return;
            }
            if (!correo.Contains("@") || correo.IndexOf('.') < 0)
            {
                lblError.Text = "El correo no tiene un formato válido.";
                return;
            }
            if (usuario.Contains(" "))
            {
                lblError.Text = "El usuario no puede llevar espacios.";
                return;
            }
            if (cboRol.SelectedValue == null)
            {
                lblError.Text = "Selecciona un rol.";
                return;
            }

            string clave = null;
            if (EsNuevo)
            {
                string motivo;
                if (!SecurityHelper.EsPasswordValida(txtClave.Text, out motivo))
                {
                    lblError.Text = motivo;
                    return;
                }
                if (txtClave.Text != txtConfirmar.Text)
                {
                    lblError.Text = "La confirmación no coincide con la contraseña.";
                    return;
                }
                clave = txtClave.Text;
            }

            try
            {
                btnGuardar.Enabled = false;

                int? id = EsNuevo ? (int?)null : Convert.ToInt32(_fila["usuario_id"]);
                UsuarioIdGuardado = _repo.GuardarUsuario(_adminId, id, nombre, apellido, correo, usuario,
                                                         Convert.ToInt32(cboRol.SelectedValue),
                                                         chkActivo.Checked, clave);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Trace.TraceError("No se pudo guardar el usuario: " + ex);
                lblError.Text = ex.Message;   // los mensajes del servidor ya son claros
            }
            finally
            {
                btnGuardar.Enabled = true;
            }
        }
    }
}
