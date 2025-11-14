using System.ComponentModel.DataAnnotations;

namespace GestaoChamados.DTOs
{
    public class CriarUsuarioDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(200)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres")]
        [MaxLength(500)]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role é obrigatório")]
        [RegularExpression("^(Usuario|Tecnico)$", ErrorMessage = "Role deve ser 'Usuario' ou 'Tecnico'")]
        public string Role { get; set; } = string.Empty;
    }
}
