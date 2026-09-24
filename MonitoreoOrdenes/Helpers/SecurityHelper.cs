
using System.Security.Cryptography;
using System.Text;

namespace MonitoreoOrdenes.Helpers
{
    public static class SecurityHelper
    {
        /// <summary>
        /// Genera hash SHA-256 con UTF-8.
        /// El script 06_fix_login.sql actualiza la BD con el mismo encoding.
        /// </summary>
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}

