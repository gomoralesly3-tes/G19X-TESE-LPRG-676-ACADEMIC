using System;
using System.Drawing;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Forms
{
    public class FrmLogin : Form
    {
        // Usuario autenticado, accesible desde Program.cs
        public static Usuario UsuarioActual { get; private set; }

        private readonly UsuarioRepository _repo = new UsuarioRepository();

        public FrmLogin()
        {
            InitializeComponent();
            ConfigurarUI();
        }

        // -------------------------------------------------------
        // Construcción de controles (sin diseñador .designer.cs)
        // -------------------------------------------------------
        private Label lblTitulo, lblSubtitulo, lblUsuario, lblPassword, lblError;
        private TextBox txtUsuario, txtPassword;
        private Button btnEntrar;
        private Panel panelCard;

        private void InitializeComponent()
        {
            this.Text            = "Acceso al Sistema";
            this.Size            = new Size(480, 520);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.MinimizeBox     = false;
            this.BackColor       = Color.FromArgb(30, 58, 95);  // Azul oscuro

            // Card central
            panelCard = new Panel
            {
                Size      = new Size(380, 360),
                Location  = new Point(50, 70),
                BackColor = Color.White
            };
            panelCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var pen = new Pen(Color.FromArgb(200, 200, 200)))
                    g.DrawRectangle(pen, 0, 0, panelCard.Width - 1, panelCard.Height - 1);
            };

            // Logo / título
            lblTitulo = new Label
            {
                Text      = "📦 Monitoreo OC",
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 58, 95),
                Location  = new Point(50, 80),
                AutoSize  = true
            };

            lblSubtitulo = new Label
            {
                Text      = "Develop Talent & Technology",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Location  = new Point(100, 128),
                AutoSize  = true
            };

            // Campos
            lblUsuario = new Label { Text = "Usuario", Location = new Point(40, 175), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtUsuario = new TextBox
            {
                Location = new Point(40, 195),
                Size     = new Size(300, 30),
                Font     = new Font("Segoe UI", 11),
                Text     = "admin"
            };

            lblPassword = new Label { Text = "Contraseña", Location = new Point(40, 235), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            txtPassword = new TextBox
            {
                Location         = new Point(40, 255),
                Size             = new Size(300, 30),
                Font             = new Font("Segoe UI", 11),
                UseSystemPasswordChar = true
            };

            lblError = new Label
            {
                Text      = "",
                ForeColor = Color.Red,
                Location  = new Point(40, 295),
                Size      = new Size(300, 20),
                Font      = new Font("Segoe UI", 8)
            };

            btnEntrar = new Button
            {
                Text      = "Iniciar Sesión",
                Location  = new Point(40, 310),
                Size      = new Size(300, 38),
                BackColor = Color.FromArgb(30, 58, 95),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand
            };
            btnEntrar.FlatAppearance.BorderSize = 0;
            btnEntrar.Click += BtnEntrar_Click;

            // Agregar controles al card
            panelCard.Controls.AddRange(new Control[]
            {
                lblTitulo, lblSubtitulo,
                lblUsuario, txtUsuario,
                lblPassword, txtPassword,
                lblError, btnEntrar
            });

            this.Controls.Add(panelCard);

            // Enter en password = login
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnEntrar_Click(s, e); };
        }

        private void ConfigurarUI()
        {
            // Barra de título personalizada con logo empresa
            Label lblHeader = new Label
            {
                Text      = "PluriOne S.A. de C.V.",
                Font      = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                Location  = new Point(0, 30),
                Width     = this.Width,
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblHeader);
        }

        // -------------------------------------------------------
        // Eventos
        // -------------------------------------------------------
        private void BtnEntrar_Click(object sender, EventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "Ingrese usuario y contraseña.";
                return;
            }

            btnEntrar.Enabled = false;
            btnEntrar.Text    = "Verificando...";

            try
            {
                string hash   = SecurityHelper.HashPassword(txtPassword.Text.Trim());

                // DEBUG TEMPORAL: muestra el hash que se está generando
                MessageBox.Show(
                    $"Usuario: {txtUsuario.Text.Trim()}\nHash generado:\n{hash}",
                    "DEBUG - Hash",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                var usuario   = _repo.Login(txtUsuario.Text.Trim(), hash);

                if (usuario == null)
                {
                    lblError.Text     = "Usuario o contraseña incorrectos.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;
                }

                UsuarioActual = usuario;

                var dashboard = new FrmDashboard();
                dashboard.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error de conexión:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnEntrar.Enabled = true;
                btnEntrar.Text    = "Iniciar Sesión";
            }
        }
    }
}
