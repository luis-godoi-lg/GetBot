using System.ComponentModel.DataAnnotations;

namespace GestaoChamados.DTOs
{
    public class CriarChamadoDto
    {
        [Required(ErrorMessage = "Assunto é obrigatório")]
        [MaxLength(300)]
        public string Assunto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Descrição é obrigatória")]
        [MaxLength(2000)]
        public string Descricao { get; set; } = string.Empty;

        public string? AnexoNomeArquivo { get; set; }
    }

    public class AtualizarChamadoDto
    {
        [MaxLength(300)]
        public string? Assunto { get; set; }

        [MaxLength(2000)]
        public string? Descricao { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(200)]
        public string? TecnicoAtribuidoEmail { get; set; }

        [Range(1, 5, ErrorMessage = "Rating deve ser entre 1 e 5")]
        public int? Rating { get; set; }
    }

    public class ChamadoDto
    {
        public int Protocolo { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime DataAbertura { get; set; }
        public string UsuarioCriadorEmail { get; set; } = string.Empty;
        public string? TecnicoAtribuidoEmail { get; set; }
        public string? AnexoNomeArquivo { get; set; }
        public int? Rating { get; set; }
    }
}
