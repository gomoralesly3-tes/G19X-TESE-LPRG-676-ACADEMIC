namespace MonitoreoOrdenes.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public int RolId { get; set; }
        public string RolNombre { get; set; }
        public bool Activo { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
