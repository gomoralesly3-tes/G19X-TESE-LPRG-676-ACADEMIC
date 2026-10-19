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
            string error;
            if (!DatabaseHelper.TestConnection(out error))
            {
                MessageBox.Show(
                    "No se puede conectar a la base de datos.\n\n" +
                    "Revise el servidor en el archivo conexion.config (junto al programa) " +
                    "y que SQL Server esté en ejecución.\n\n" +
                    "Detalle: " + error,
                    "Error de Conexión",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FrmLogin());
        }
    }
}
