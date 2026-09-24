using System;
using System.Windows.Forms;
using MonitoreoOrdenes.Data;
using MonitoreoOrdenes.Forms;

namespace MonitoreoOrdenes
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Verificar conexión antes de abrir
            if (!DatabaseHelper.TestConnection())
            {
                MessageBox.Show(
                    "No se puede conectar a la base de datos.\n\n" +
                    "Verifique la cadena de conexión en App.config.",
                    "Error de Conexión",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FrmLogin());
        }
    }
}
