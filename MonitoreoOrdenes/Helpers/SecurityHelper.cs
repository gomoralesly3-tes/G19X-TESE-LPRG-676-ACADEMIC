using System;
using System.Security.Cryptography;
using System.Text;

namespace MonitoreoOrdenes.Helpers
{
    /// <summary>
    /// Manejo de contraseñas.
    /// Formato nuevo  : PBKDF2$iteraciones$sal(base64)$hash(base64)  (PBKDF2-HMAC-SHA256, sal por usuario)
    /// Formato antiguo: SHA-256 en hexadecimal (64 caracteres), sin sal. Se acepta solo para
    /// poder migrar: al iniciar sesion correctamente se vuelve a guardar en el formato nuevo.
    /// </summary>
    public static class SecurityHelper
    {
        public enum ResultadoVerificacion
        {
            Incorrecta,
            Correcta,
            CorrectaRequiereActualizar   // la contraseña es valida pero el hash esta en formato antiguo/debil
        }

        private const string Prefijo     = "PBKDF2";
        private const int    Iteraciones = 100000;
        private const int    TamanoSal   = 16;
        private const int    TamanoHash  = 32;

        /// <summary>Genera el hash (con sal nueva) listo para guardar en la BD.</summary>
        public static string HashPassword(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var sal = new byte[TamanoSal];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(sal);

            byte[] hash = Derivar(password, sal, Iteraciones, TamanoHash);
            return string.Join("$", Prefijo, Iteraciones,
                               Convert.ToBase64String(sal), Convert.ToBase64String(hash));
        }

        /// <summary>Compara la contraseña escrita contra el hash guardado en la BD.</summary>
        public static ResultadoVerificacion Verificar(string password, string almacenado)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(almacenado))
                return ResultadoVerificacion.Incorrecta;

            if (almacenado.StartsWith(Prefijo + "$", StringComparison.Ordinal))
                return VerificarPbkdf2(password, almacenado);

            return VerificarSha256Antiguo(password, almacenado);
        }

        /// <summary>
        /// Gasta el mismo tiempo que una verificacion real. Se usa cuando el usuario no existe,
        /// para que no se pueda saber por el tiempo de respuesta si el usuario es valido.
        /// </summary>
        public static void SimularVerificacion(string password)
        {
            Derivar(password ?? "", new byte[TamanoSal], Iteraciones, TamanoHash);
        }

        // ── Privados ────────────────────────────────────────────
        private static ResultadoVerificacion VerificarPbkdf2(string password, string almacenado)
        {
            string[] partes = almacenado.Split('$');
            if (partes.Length != 4) return ResultadoVerificacion.Incorrecta;

            int iteraciones;
            if (!int.TryParse(partes[1], out iteraciones) || iteraciones <= 0)
                return ResultadoVerificacion.Incorrecta;

            byte[] sal, esperado;
            try
            {
                sal      = Convert.FromBase64String(partes[2]);
                esperado = Convert.FromBase64String(partes[3]);
            }
            catch (FormatException)
            {
                return ResultadoVerificacion.Incorrecta;
            }

            byte[] calculado = Derivar(password, sal, iteraciones, esperado.Length);
            if (!IgualesTiempoConstante(calculado, esperado))
                return ResultadoVerificacion.Incorrecta;

            // Si algun dia se sube el numero de iteraciones, los hashes viejos se actualizan solos.
            return iteraciones < Iteraciones
                ? ResultadoVerificacion.CorrectaRequiereActualizar
                : ResultadoVerificacion.Correcta;
        }

        private static ResultadoVerificacion VerificarSha256Antiguo(string password, string almacenado)
        {
            string hex;
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                hex = sb.ToString();
            }

            bool iguales = IgualesTiempoConstante(
                Encoding.ASCII.GetBytes(hex),
                Encoding.ASCII.GetBytes(almacenado.Trim().ToLowerInvariant()));

            return iguales ? ResultadoVerificacion.CorrectaRequiereActualizar
                           : ResultadoVerificacion.Incorrecta;
        }

        private static byte[] Derivar(string password, byte[] sal, int iteraciones, int longitud)
        {
            using (var kdf = new Rfc2898DeriveBytes(password, sal, iteraciones, HashAlgorithmName.SHA256))
                return kdf.GetBytes(longitud);
        }

        private static bool IgualesTiempoConstante(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diferencia = 0;
            for (int i = 0; i < a.Length; i++)
                diferencia |= a[i] ^ b[i];
            return diferencia == 0;
        }
    }
}
