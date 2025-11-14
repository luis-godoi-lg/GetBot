namespace GestaoChamados.Models
{
    public class UsuarioModel
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public string? Senha { get; set; }
        public string? Role { get; set; } // "Usuario", "Tecnico", "Gerente"
        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}   