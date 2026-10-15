using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Helpers;
using MonitoreoOrdenes.Models;

namespace MonitoreoOrdenes.Forms
{
    public class FrmLogin : Form
    {
        public static Usuario UsuarioActual { get; private set; }

        private readonly UsuarioRepository _repo = new UsuarioRepository();

        private ModernTextBox txtUsuario, txtPassword;
        private RoundedButton btnEntrar;
        private Label lblError;
        private Panel panelIzquierdo, panelDerecho;

        public FrmLogin()
        {
            InitializeComponent();
            ConstruirPanelIzquierdo();
            ConstruirPanelDerecho();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FrmLogin
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.ClientSize = new System.Drawing.Size(900, 560);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmLogin";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Monitoreo OC — PluriOne";
            this.Load += new System.EventHandler(this.FrmLogin_Load);
            this.ResumeLayout(false);

        }

        // ── Panel izquierdo — imagen/branding ───────────────────
        private void ConstruirPanelIzquierdo()
        {
            panelIzquierdo = new Panel
            {
                Size      = new Size(400, 560),
                Location  = new Point(0, 0),
                BackColor = UITheme.Primario
            };

            // Círculos decorativos de fondo
            panelIzquierdo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var br = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                {
                    g.FillEllipse(br, -60, -60, 250, 250);
                    g.FillEllipse(br, 200, 350, 280, 280);
                    g.FillEllipse(br, 150, 100, 150, 150);
                }
            };

            var lblIcono = new Label
            {
                Text      = "📦",
                Font      = new Font("Segoe UI", 52),
                ForeColor = Color.White,
                Location  = new Point(150, 130),
                AutoSize  = true
            };

            var lblTitulo = new Label
            {
                Text      = "Monitoreo de",
                Font      = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location  = new Point(60, 230),
                AutoSize  = true
            };

            var lblTitulo2 = new Label
            {
                Text      = "Órdenes de Compra",
                Font      = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = UITheme.Acento,
                Location  = new Point(60, 260),
                AutoSize  = true
            };

            var lblEmpresa = new Label
            {
                Text      = "PluriOne S.A. de C.V.",
                Font      = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 255, 255, 255),
                Location  = new Point(60, 310),
                AutoSize  = true
            };

            var lblSlogan = new Label
            {
                Text      = "Develop Talent & Technology",
                Font      = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 255, 255, 255),
                Location  = new Point(60, 335),
                AutoSize  = true
            };

            // Versión
            var lblVersion = new Label
            {
                Text      = "v1.0.0",
                Font      = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(100, 255, 255, 255),
                Location  = new Point(175, 500),
                AutoSize  = true
            };

            panelIzquierdo.Controls.AddRange(new Control[]
            {
                lblIcono, lblTitulo, lblTitulo2, lblEmpresa, lblSlogan, lblVersion
            });

            this.Controls.Add(panelIzquierdo);
        }

        private void FrmLogin_Load(object sender, EventArgs e)
        {

        }

        // ── Panel derecho — formulario ──────────────────────────
        private void ConstruirPanelDerecho()
        {
            panelDerecho = new Panel
            {
                Size      = new Size(500, 560),
                Location  = new Point(400, 0),
                BackColor = Color.White
            };

            var lblBienvenida = new Label
            {
                Text      = "Bienvenida 👋",
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = UITheme.TextoPrimario,
                Location  = new Point(60, 80),
                AutoSize  = true
            };

            var lblSubtitulo = new Label
            {
                Text      = "Ingresa tus credenciales para continuar",
                Font      = UITheme.FuenteNormal,
                ForeColor = UITheme.TextoSecundario,
                Location  = new Point(60, 120),
                AutoSize  = true
            };

            // ── Usuario ──
            var lblUsuario = new Label
            {
                Text      = "USUARIO",
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = UITheme.TextoSecundario,
                Location  = new Point(60, 180),
                AutoSize  = true
            };

            txtUsuario = new ModernTextBox
            {
                Location = new Point(60, 198),
                Width    = 360,
                Text     = ""
            };

            // ── Contraseña ──
            var lblPassword = new Label
            {
                Text      = "CONTRASEÑA",
                Font      = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = UITheme.TextoSecundario,
                Location  = new Point(60, 260),
                AutoSize  = true
            };

            txtPassword = new ModernTextBox
            {
                Location              = new Point(60, 278),
                Width                 = 360,
                UseSystemPasswordChar = true
            };

            // ── Error ──
            lblError = new Label
            {
                Text      = "",
                Font      = UITheme.FuentePequena,
                ForeColor = UITheme.Peligro,
                Location  = new Point(60, 332),
                Size      = new Size(360, 18)
            };

            // ── Botón ──
            btnEntrar = new RoundedButton
            {
                Text     = "Iniciar Sesión →",
                Location = new Point(60, 358),
                Size     = new Size(360, 46),
                Font     = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnEntrar.Click += BtnEntrar_Click;

            // ── Hint contraseña ──
            var lblHint = new Label
            {
                
                Font      = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = UITheme.TextoMuted,
                Location  = new Point(60, 420),
                AutoSize  = true
            };

            // ── Footer ──
            var lblFooter = new Label
            {
                Text      = "© 2026 PluriOne S.A. de C.V. Todos los derechos reservados.",
                Font      = new Font("Segoe UI", 7),
                ForeColor = UITheme.TextoMuted,
                Location  = new Point(60, 510),
                AutoSize  = true
            };

            panelDerecho.Controls.AddRange(new Control[]
            {
                lblBienvenida, lblSubtitulo,
                lblUsuario, txtUsuario,
                lblPassword, txtPassword,
                lblError, btnEntrar, lblHint, lblFooter
            });

            this.Controls.Add(panelDerecho);

            // Enter en password = login
            txtPassword.InnerTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnEntrar_Click(s, e);
                }
            };
        }

        /// <summary>
        /// Deja el login limpio para volver a mostrarlo tras cerrar sesión.
        /// </summary>
        public void Reiniciar()
        {
            UsuarioActual    = null;
            txtPassword.Text = "";
            lblError.Text    = "";
            txtPassword.InnerTextBox.Focus();
        }

        // ── Evento login ────────────────────────────────────────
        private void BtnEntrar_Click(object sender, EventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "⚠ Ingresa usuario y contraseña.";
                return;
            }

            btnEntrar.Enabled = false;
            btnEntrar.Text    = "Verificando...";

            try
            {
                var usuario = _repo.Login(txtUsuario.Text.Trim(), txtPassword.Text);

                if (usuario == null)
                {
                    lblError.Text = "⚠ Usuario o contraseña incorrectos.";
                    txtPassword.Text = "";
                    txtPassword.InnerTextBox.Focus();
                    return;
                }

                UsuarioActual = usuario;
                var dashboard = new FrmDashboard();
                dashboard.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnEntrar.Enabled = true;
                btnEntrar.Text    = "Iniciar Sesión →";
            }
        }
    }
}
