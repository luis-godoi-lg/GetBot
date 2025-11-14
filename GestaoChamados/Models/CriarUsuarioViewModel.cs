using System.ComponentModel.DataAnnotations;

namespace GestaoChamados.Models
{
    public class CriarUsuarioViewModel
    {
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O campo Email é obrigatório.")]
        [EmailAddress(ErrorMessage = "O formato do email é inválido.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo Senha é obrigatório.")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

        [Required(ErrorMessage = "É obrigatório definir um Cargo.")]
        public string Role { get; set; } // Usuario, Tecnico ou Superior
    }
}