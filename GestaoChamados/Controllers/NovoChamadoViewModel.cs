using System.ComponentModel.DataAnnotations;

namespace GestaoChamados.Models
{
    public class NovoChamadoViewModel
    {
        [Required(ErrorMessage = "O campo Assunto é obrigatório.")]
        [Display(Name = "Assunto do Chamado")]
        public string Assunto { get; set; }

        [Required(ErrorMessage = "O campo Descrição é obrigatório.")]
        [Display(Name = "Descrição Detalhada")]
        public string Descricao { get; set; }
    }
}