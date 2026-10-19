using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;

namespace MonitoreoOrdenes.Forms
{
    /// <summary>Permite a cualquier usuario cambiar su propia contraseña.</summary>
    public class FrmCambiarPassword : Form
    {
        private readonly UsuarioRepository _repo = new UsuarioRepository();

        private TextBox txtActual, txtNueva, txtConfirmar;
        private Label   lblError;
        private Button  btnGuardar, btnCancelar;

        public FrmCambiarPassword()
        {
            this.Text            = "Cambiar contraseña";
            this.ClientSize      = new Size(440, 375);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.ShowInTaskbar   = false;
            this.BackColor       = Color.White;

            txtActual    = EstiloAdmin.Caja(30,  98, 380, true);
            txtNueva     = EstiloAdmin.Caja(30, 168, 380, true);
            txtConfirmar = EstiloAdmin.Caja(30, 250, 380, true);

            var hint = new Label
            {
                Text      = "Mínimo 8 caracteres, con al menos una letra y un número.",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = EstiloAdmin.Gris,
                Location  = new Point(30, 198),
                AutoSize  = true
            };

            lblError = new Label
            {
                Location  = new Point(30, 286),
                Size      = new Size(380, 36),
                ForeColor = EstiloAdmin.Rojo,
                Font      = new Font("Segoe UI", 9)
            };

            btnGuardar  = EstiloAdmin.Boton("Guardar", 230, 328, 100, 34, EstiloAdmin.Azul);
            btnCancelar = EstiloAdmin.BotonSecundario("Cancelar", 340, 328, 80, 34);
            btnCancelar.DialogResult = DialogResult.Cancel;
            btnGuardar.Click += BtnGuardar_Click;

            this.Controls.AddRange(new Control[]
            {
                EstiloAdmin.Encabezado("Cambiar contraseña"),
                EstiloAdmin.Etiqueta("Contraseña actual",           30,  75),
                txtActual,
                EstiloAdmin.Etiqueta("Nueva contraseña",            30, 145),
                txtNueva, hint,
                EstiloAdmin.Etiqueta("Confirmar nueva contraseña",  30, 227),
                txtConfirmar, lblError, btnGuardar, btnCancelar
            });

            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            lblError.Text = "";

            var usuario = FrmLogin.UsuarioActual;
            if (usuario == null)
            {
                lblError.Text = "La sesión expiró. Vuelve a iniciar sesión.";
                return;
            }

            if (string.IsNullOrEmpty(txtActual.Text))
            {
                lblError.Text = "Escribe tu contraseña actual.";
                return;
            }

            string motivo;
            if (!SecurityHelper.EsPasswordValida(txtNueva.Text, out motivo))
            {
                lblError.Text = motivo;
                return;
            }

            if (txtNueva.Text != txtConfirmar.Text)
            {
                lblError.Text = "La confirmación no coincide con la nueva contraseña.";
                return;
            }

            if (txtNueva.Text == txtActual.Text)
            {
                lblError.Text = "La nueva contraseña debe ser distinta de la actual.";
                return;
            }

            try
            {
                btnGuardar.Enabled = false;

                bool ok = _repo.CambiarPassword(usuario.Username, usuario.UsuarioId,
                                                txtActual.Text, txtNueva.Text);
                if (!ok)
                {
                    lblError.Text = "La contraseña actual no es correcta.";
                    txtActual.Clear();
                    txtActual.Focus();
                    return;
                }

                MessageBox.Show("Tu contraseña se cambió correctamente.", "Contraseña",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error al cambiar la contraseña: " + ex);
                lblError.Text = "No se pudo cambiar la contraseña: " + ex.Message;
            }
            finally
            {
                btnGuardar.Enabled = true;
            }
        }
    }
}
