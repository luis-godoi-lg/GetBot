using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Shared.Services;

/// <summary>
/// Serviço compartilhado para comunicação com a API REST
/// Usado tanto pelo Desktop WPF quanto pelo Mobile MAUI
/// Gerencia autenticação JWT, requisições HTTP e serialização de dados
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public string? Token
    {
        get => _token;
        set
        {
            _token = value;
            if (!string.IsNullOrEmpty(_token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", _token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
    }

    public string BaseUrl { get; }

    public ApiService(string baseUrl = "https://localhost:7001")
    {
        BaseUrl = baseUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ==================== AUTENTICAÇÃO ====================

    public async Task<LoginResponseDto?> LoginAsync(string email, string senha)
    {
        try
        {
            var request = new LoginRequestDto { Email = email, Senha = senha };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (result != null)
                {
                    Token = result.Token;
                }
                return result;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no login: {ex.Message}");
            return null;
        }
    }

    public void Logout()
    {
        Token = null;
    }

    // ==================== CHAMADOS ====================

    public async Task<List<ChamadoDto>?> GetChamadosAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ChamadoDto>>("/api/chamados");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar chamados: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ChamadoDto>?> GetMeusChamadosAsync()
    {
        try
        {
            // A API já filtra automaticamente por usuário quando não é técnico
            return await _httpClient.GetFromJsonAsync<List<ChamadoDto>>("/api/chamados");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar meus chamados: {ex.Message}");
            return null;
        }
    }

    public async Task<ChamadoDto?> GetChamadoByIdAsync(int id)
    {
        try
        {
            Console.WriteLine($"[ApiService] Buscando chamado por ID #{id}...");
            var chamado = await _httpClient.GetFromJsonAsync<ChamadoDto>($"/api/chamados/{id}");
            Console.WriteLine($"[ApiService] Chamado #{id} encontrado: Status={chamado?.Status}, Usuario={chamado?.UsuarioNome}");
            return chamado;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"[ApiService] Chamado #{id} não encontrado (404) - ticket pode não ter sido criado ainda");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao buscar chamado {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<ChamadoDto?> CriarChamadoAsync(CriarChamadoDto chamado)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chamados", chamado);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChamadoDto>();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar chamado: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AssumarChamadoAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/chamados/{id}/assumir", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao assumir chamado: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> FinalizarChamadoAsync(int id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/chamados/{id}/finalizar", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao finalizar chamado: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AvaliarChamadoAsync(int id, int rating)
    {
        try
        {
            var avaliacaoDto = new { rating = rating };
            var response = await _httpClient.PostAsJsonAsync($"/api/chamados/{id}/avaliar", avaliacaoDto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao avaliar chamado: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AssumirChamadoAsync(int id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/chamados/{id}/assumir", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao assumir chamado: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SolicitarAtendimentoAsync(int id)
    {
        try
        {
            Console.WriteLine($"[ApiService] Solicitando atendimento para chamado #{id}...");
            var response = await _httpClient.PostAsync($"/api/chamados/{id}/solicitar-atendimento", null);
            Console.WriteLine($"[ApiService] SolicitarAtendimento para #{id} - StatusCode: {response.StatusCode}, IsSuccess: {response.IsSuccessStatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                // Buscar o chamado atualizado para confirmar o status
                var chamadoAtualizado = await GetChamadoByIdAsync(id);
                if (chamadoAtualizado != null)
                {
                    Console.WriteLine($"[ApiService] Chamado #{id} após solicitar atendimento - Status: {chamadoAtualizado.Status}");
                }
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] ERRO ao solicitar atendimento: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> MarcarComoResolvidoAsync(int id)
    {
        try
        {
            var response = await _httpClient.PutAsync($"/api/chamados/{id}/resolver", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao marcar chamado como resolvido: {ex.Message}");
            return false;
        }
    }

    public async Task<ChamadoDto?> GetChamadoAsync(int id)
    {
        try
        {
            Console.WriteLine($"[ApiService] Buscando chamado #{id}...");
            var chamado = await _httpClient.GetFromJsonAsync<ChamadoDto>($"/api/chamados/{id}");
            Console.WriteLine($"[ApiService] Chamado #{id} encontrado");
            return chamado;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Console.WriteLine($"[ApiService] Chamado #{id} não encontrado (404)");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao buscar chamado #{id}: {ex.Message}");
            return null;
        }
    }

    public async Task<int?> CriarChamadoAsync(string assunto, string descricao)
    {
        try
        {
            var request = new CriarChamadoDto 
            { 
                Titulo = assunto, 
                Descricao = descricao 
            };
            
            var response = await _httpClient.PostAsJsonAsync("/api/chamados", request);
            
            if (response.IsSuccessStatusCode)
            {
                var chamado = await response.Content.ReadFromJsonAsync<ChamadoDto>();
                return chamado?.Id;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar chamado: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> EnviarMensagemChatAsync(int chamadoId, string mensagem)
    {
        try
        {
            var msg = await EnviarMensagemAsync(chamadoId, mensagem);
            return msg != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
            return false;
        }
    }

    public string GetBaseUrl()
    {
        return BaseUrl;
    }

    // ==================== CHAT ====================

    public async Task<List<ChatMessageDto>?> GetMensagensChamadoAsync(int chamadoId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ChatMessageDto>>(
                $"/api/chamados/{chamadoId}/mensagens");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar mensagens: {ex.Message}");
            return null;
        }
    }

    public async Task<ChatMessageDto?> EnviarMensagemAsync(int chamadoId, string mensagem)
    {
        try
        {
            var request = new { Mensagem = mensagem };
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/chamados/{chamadoId}/mensagens", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChatMessageDto>();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
            return null;
        }
    }

    // ==================== DASHBOARD ====================

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DashboardStatsDto>("/api/dashboard/stats");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar estatísticas: {ex.Message}");
            return null;
        }
    }

    public async Task<DashboardDataDto?> GetDashboardDataAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DashboardDataDto>("/api/dashboard");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar dados do dashboard: {ex.Message}");
            return null;
        }
    }

    // ==================== MANAGER ====================

    public async Task<ManagerDashboardDto?> GetManagerDashboardAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ManagerDashboardDto>("/api/manager/dashboard");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar dashboard gerencial: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ListarUsuarioDto>?> GetUsuariosAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<ListarUsuarioDto>>("/api/manager/usuarios");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar usuários: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CriarUsuarioAsync(CriarEditarUsuarioDto usuario)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/manager/usuarios", usuario);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao criar usuário: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AtualizarUsuarioAsync(int id, CriarEditarUsuarioDto usuario)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/manager/usuarios/{id}", usuario);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao atualizar usuário: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeletarUsuarioAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/manager/usuarios/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao deletar usuário: {ex.Message}");
            return false;
        }
    }

    public async Task<RelatorioDetalhadoDto?> GetRelatorioDetalhadoAsync(DateTime? dataInicio = null, DateTime? dataFim = null)
    {
        try
        {
            var query = "";
            if (dataInicio.HasValue && dataFim.HasValue)
            {
                query = $"?dataInicio={dataInicio.Value:yyyy-MM-dd}&dataFim={dataFim.Value:yyyy-MM-dd}";
            }
            
            var endpoint = $"/api/manager/relatorio{query}";
            Console.WriteLine($"[ApiService] GET {endpoint}");
            Console.WriteLine($"[ApiService] Token: {(!string.IsNullOrEmpty(_token) ? "Configurado" : "NULL/Vazio")}");
            
            var response = await _httpClient.GetAsync(endpoint);
            Console.WriteLine($"[ApiService] Status: {(int)response.StatusCode} {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ApiService] Erro: {errorContent}");
                return null;
            }
            
            var relatorio = await response.Content.ReadFromJsonAsync<RelatorioDetalhadoDto>();
            Console.WriteLine($"[ApiService] Relatório recebido: {relatorio?.TotalChamados ?? 0} chamados");
            return relatorio;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] EXCEÇÃO ao buscar relatório: {ex.Message}");
            Console.WriteLine($"[ApiService] StackTrace: {ex.StackTrace}");
            return null;
        }
    }

    // ==================== CHATBOT ====================

    public async Task<ChatbotResponseDto?> SendChatbotMessageAsync(string message, List<ChatbotHistoryItemDto>? conversationHistory = null)
    {
        try
        {
            var request = new
            {
                Message = message,
                ConversationHistory = conversationHistory
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chatbot/message", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChatbotResponseDto>();
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem ao chatbot: {ex.Message}");
            return null;
        }
    }

    // ==================== FILA DE ATENDIMENTO ====================

    public async Task<List<Chamado>?> ObterChamadosEmFilaAsync()
    {
        try
        {
            Console.WriteLine("[ApiService] Buscando chamados da fila...");
            var chamados = await _httpClient.GetFromJsonAsync<List<ChamadoDto>>("/api/chamados");
            
            if (chamados == null)
            {
                Console.WriteLine("[ApiService] API retornou null");
                return null;
            }
            
            Console.WriteLine($"[ApiService] {chamados.Count} chamados retornados da API");
            foreach (var c in chamados)
            {
                Console.WriteLine($"  - ID: {c.Id}, Status: {c.Status}, Usuario: {c.UsuarioNome}");
            }
            
            // Filtrar apenas os que estão aguardando atendimento
            var filaFiltrada = chamados
                .Where(c => c.Status == "Aguardando Atendente" || c.Status == "Aberto")
                .Select(c => new Chamado
                {
                    Id = c.Id,
                    NomeCliente = c.UsuarioNome,
                    Assunto = c.Titulo,
                    Descricao = c.Descricao,
                    Status = c.Status,
                    DataAbertura = c.DataCriacao,
                    TecnicoId = c.TecnicoId
                })
                .ToList();
                
            Console.WriteLine($"[ApiService] {filaFiltrada.Count} chamados após filtro (Aberto ou Aguardando Atendente)");
            return filaFiltrada;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Erro ao obter fila: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AssumirChamadoAsync(int chamadoId, int tecnicoId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/chamados/{chamadoId}/assumir", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao assumir chamado: {ex.Message}");
            return false;
        }
    }

    public async Task<List<MensagemChat>?> ObterMensagensChatAsync(int chamadoId)
    {
        try
        {
            var mensagens = await _httpClient.GetFromJsonAsync<List<ChatMessageDto>>($"/api/chamados/{chamadoId}/chat");
            
            if (mensagens == null) return null;
            
            return mensagens.Select(m => new MensagemChat
            {
                Id = m.Id,
                ChamadoId = m.ChamadoId,
                Remetente = m.RemetenteNome,
                Mensagem = m.Mensagem,
                DataEnvio = m.DataEnvio,
                IsTecnico = !m.IsBot
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao obter mensagens: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> EnviarMensagemChatAsync(MensagemChat mensagem)
    {
        try
        {
            var dto = new
            {
                ChamadoId = mensagem.ChamadoId,
                Mensagem = mensagem.Mensagem,
                RemetenteNome = mensagem.Remetente
            };
            
            var response = await _httpClient.PostAsJsonAsync($"/api/chamados/{mensagem.ChamadoId}/chat", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar mensagem: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> FinalizarChamadoAsync(int chamadoId, int tecnicoId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/chamados/{chamadoId}/finalizar", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao finalizar: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> AvaliarChamadoAsync(AvaliacaoChamado avaliacao)
    {
        try
        {
            var dto = new
            {
                rating = avaliacao.Nota,
                comentario = avaliacao.Comentario
            };
            
            var response = await _httpClient.PostAsJsonAsync($"/api/chamados/{avaliacao.ChamadoId}/avaliar", dto);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao avaliar: {ex.Message}");
            return false;
        }
    }
}
