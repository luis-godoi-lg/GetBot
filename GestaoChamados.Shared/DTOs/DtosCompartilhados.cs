namespace GestaoChamados.Shared.DTOs;

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ChamadoDto
{
    public int Id { get; set; } // Mapeia para Protocolo da API
    public string Titulo { get; set; } = string.Empty; // Mapeia para Assunto da API
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Prioridade { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; } // Mapeia para DataAbertura da API
    public DateTime? DataFinalizacao { get; set; }
    public int UsuarioId { get; set; }
    public string UsuarioNome { get; set; } = string.Empty;
    public string? UsuarioEmail { get; set; } // Mapeia para UsuarioCriadorEmail da API
    public int? TecnicoId { get; set; }
    public string? TecnicoNome { get; set; } // Mapeia para TecnicoAtribuidoEmail da API
    public int? Rating { get; set; }
}

public class CriarChamadoDto
{
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Prioridade { get; set; } = "Media";
    public string? Status { get; set; } = null; // ✅ Opcional: se null, usa "Aberto" por padrão
}

public class ChatMessageDto
{
    public int Id { get; set; }
    public int ChamadoId { get; set; }
    public string RemetenteNome { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public DateTime DataEnvio { get; set; }
    public bool IsBot { get; set; }
}

public class DashboardStatsDto
{
    public int TotalChamados { get; set; }
    public int ChamadosAbertos { get; set; }
    public int ChamadosEmAtendimento { get; set; }
    public int ChamadosFinalizados { get; set; }
    public double NotaMediaSatisfacao { get; set; }
    public int TotalAvaliacoes { get; set; }
}

public class ChatbotResponseDto
{
    public string Message { get; set; } = string.Empty;
    public bool IsITRelated { get; set; }
    public bool SuggestTicketCreation { get; set; }
}

public class ChatbotHistoryItemDto
{
    public string Sender { get; set; } = string.Empty; // "user" ou "bot"
    public string Message { get; set; } = string.Empty;
}

public class ChatbotMessageRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<ChatbotHistoryItemDto>? ConversationHistory { get; set; }
}

// ===== DTOs para Dashboard =====
public class DashboardDataDto
{
    public int TotalChamados { get; set; }
    public int ChamadosAbertos { get; set; }
    public int ChamadosEmAtendimento { get; set; }
    public int ChamadosResolvidos { get; set; }
    public int ChamadosNaFila { get; set; }
    
    // Dados do sistema (gráfico geral)
    public List<StatusGroup> StatusGroups { get; set; } = new();
    
    // **NOVO: Dados específicos do técnico**
    public List<string> MeuStatusLabels { get; set; } = new();
    public List<int> MeuStatusCounts { get; set; } = new();
    public double PercentualResolvidos { get; set; }
    public double NotaMediaSatisfacao { get; set; }
    public int TotalAvaliacoes { get; set; }
    
    public List<RankingModel> TopUsuarios { get; set; } = new();
    public List<RankingModel> TopTecnicos { get; set; } = new();
}

public class StatusGroup
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RankingModel
{
    public string Nome { get; set; } = string.Empty;
    public int Contagem { get; set; }
}

// ===== DTOs para Manager =====
public class ManagerDashboardDto
{
    public int ChamadosAbertos { get; set; }
    public int ChamadosResolvidos { get; set; }
    public int ChamadosNaoAtendidos { get; set; }
    public int ChamadosEmAtendimento { get; set; }
    public int TotalChamados { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalTecnicos { get; set; }
    public double TaxaResolucao { get; set; }
    public double AvaliaoMediaAtendimento { get; set; }
    public List<ChamadoPorCategoriaDto> ChamadosPorCategoria { get; set; } = new();
    public List<ChamadoPorPrioridadeDto> ChamadosPorPrioridade { get; set; } = new();
    public List<DesempenhoTecnicoDto> DesempenhoPorTecnico { get; set; } = new();
}

public class ChamadoPorCategoriaDto
{
    public string? Categoria { get; set; }
    public int Quantidade { get; set; }
}

public class ChamadoPorPrioridadeDto
{
    public string? Prioridade { get; set; }
    public int Quantidade { get; set; }
}

public class DesempenhoTecnicoDto
{
    public string? Nome { get; set; }
    public int Resolvidos { get; set; }
}

public class ListarUsuarioDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}

public class CriarEditarUsuarioDto
{
    public int? Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Senha { get; set; }
    public string Role { get; set; } = string.Empty;
}

public class RelatorioDetalhadoDto
{
    public int TotalChamados { get; set; }
    public int Abertos { get; set; }
    public int Resolvidos { get; set; }
    public int EmAtendimento { get; set; }
    public int NaoAtendidos { get; set; }
    public List<ChamadosPorTecnicoDto> ChamadosPorTecnico { get; set; } = new();
}

public class ChamadosPorTecnicoDto
{
    public string Tecnico { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Resolvidos { get; set; }
    public double NotaMedia { get; set; }
}

// ===== Modelos para Chat e Avaliação =====
public class Chamado
{
    public int Id { get; set; }
    public string? NomeCliente { get; set; }
    public string? Assunto { get; set; }
    public string? Descricao { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DataAbertura { get; set; }
    public int? TecnicoId { get; set; }
}

public class MensagemChat
{
    public int Id { get; set; }
    public int ChamadoId { get; set; }
    public int UsuarioId { get; set; }
    public string? Mensagem { get; set; }
    public DateTime DataEnvio { get; set; }
    public string? Remetente { get; set; }
    public bool IsTecnico { get; set; }
}

public class AvaliacaoChamado
{
    public int Id { get; set; }
    public int ChamadoId { get; set; }
    public int Nota { get; set; }
    public string? Comentario { get; set; }
    public DateTime DataAvaliacao { get; set; }
}
