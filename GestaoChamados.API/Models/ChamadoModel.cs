namespace GestaoChamados.Models
{
    /// <summary>
    /// Modelo de chamado/ticket de suporte
    /// Representa uma solicitação de suporte técnico com seu ciclo de vida completo
    /// </summary>
    public class ChamadoModel
    {
        /// <summary>Número de protocolo do chamado (ID único, auto-incrementado)</summary>
        public int Protocolo { get; set; }
        
        /// <summary>Título/Assunto do chamado</summary>
        public string Assunto { get; set; }
        
        /// <summary>Descrição detalhada do problema</summary>
        public string Descricao { get; set; }
        
        /// <summary>Status atual: "Aberto", "Aguardando Atendente", "Em Atendimento", "Resolvido"</summary>
        public string Status { get; set; }
        
        /// <summary>Data e hora de abertura do chamado</summary>
        public DateTime DataAbertura { get; set; }

        /// <summary>Email do usuário que criou o chamado</summary>
        public string UsuarioCriadorEmail { get; set; }
        
        /// <summary>Email do técnico responsável pelo atendimento (null se não atribuído)</summary>
        public string? TecnicoAtribuidoEmail { get; set; }
       
        /// <summary>Nome do arquivo anexado ao chamado (opcional)</summary>
        public string? AnexoNomeArquivo { get; set; }

        /// <summary>Avaliação do atendimento de 1 a 5 estrelas (null se não avaliado)</summary>
        public int? Rating { get; set; }
    }
}