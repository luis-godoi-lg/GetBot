using System.ComponentModel.DataAnnotations;

namespace GestaoChamados.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "Email inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}