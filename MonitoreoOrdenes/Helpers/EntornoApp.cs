using System;
using System.Configuration;

namespace MonitoreoOrdenes.Helpers
{
    /// <summary>
    /// Entorno en el que corre la aplicación ("Desarrollo" o "Produccion").
    /// Se define en entorno.config, un archivo propio de cada equipo que Git ignora.
    /// </summary>
    public static class EntornoApp
    {
        public static string Nombre
        {
            get
            {
                string valor = ConfigurationManager.AppSettings["Entorno"];
                return string.IsNullOrWhiteSpace(valor) ? "Desarrollo" : valor.Trim();
            }
        }

        public static bool EsProduccion
        {
            get
            {
                string n = Nombre;
                return n.Equals("Produccion", StringComparison.OrdinalIgnoreCase)
                    || n.Equals("Producción", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>Texto para el título de las ventanas: vacío en producción, "[DESARROLLO]" en otro caso.</summary>
        public static string Sufijo
        {
            get { return EsProduccion ? "" : "  [" + Nombre.ToUpperInvariant() + "]"; }
        }
    }
}
