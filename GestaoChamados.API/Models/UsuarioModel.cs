namespace GestaoChamados.Models
{
    /// <summary>
    /// Modelo de usuário do sistema
    /// Representa usuários com diferentes níveis de acesso: Usuario, Tecnico, Gerente, Admin
    /// Senha armazenada com hash BCrypt para segurança
    /// </summary>
    public class UsuarioModel
    {
        /// <summary>ID único do usuário (chave primária)</summary>
        public int Id { get; set; }
        
        /// <summary>Nome completo do usuário</summary>
        public string Nome { get; set; }
        
        /// <summary>Email do usuário (único no sistema, usado para login)</summary>
        public string Email { get; set; }
        
        /// <summary>Senha criptografada com BCrypt</summary>
        public string Senha { get; set; }
        
        /// <summary>Nível de acesso: "Usuario", "Tecnico", "Gerente" ou "Admin"</summary>
        public string Role { get; set; }
    }
}   