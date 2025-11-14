namespace GestaoChamados.Models
{
    public class ChamadoModel
    {
        public int Protocolo { get; set; }
        public string Assunto { get; set; }
        public string Descricao { get; set; }
        public string Status { get; set; }
        public DateTime DataAbertura { get; set; }

        // CAMPOS ADICIONADOS
        public string UsuarioCriadorEmail { get; set; }
        public string? TecnicoAtribuidoEmail { get; set; } // '?' indica que pode ser nulo (não atribuído)

        // ... outras propriedades ...
       
        // NOVA PROPRIEDADE
        public string? AnexoNomeArquivo { get; set; } // '?' indica que o anexo é opcional

        // NOVA PROPRIEDADE
        public int? Rating { get; set; } // Avaliação do atendimento (1 a 5 estrelas)
    }
}