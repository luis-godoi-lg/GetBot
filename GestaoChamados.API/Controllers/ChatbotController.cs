using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.API.Controllers
{
    /// <summary>
    /// Controller responsável pela integração com o chatbot de IA (OpenAI)
    /// Processa mensagens, mantém contexto da conversa e determina quando escalar para atendimento humano
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatbotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotController> _logger;
        private readonly HttpClient _httpClient;

        public ChatbotController(
            IConfiguration configuration,
            ILogger<ChatbotController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Processa uma mensagem do usuário com o chatbot de IA
        /// Usa OpenAI API para gerar respostas contextuais e inteligentes
        /// </summary>
        /// <param name="request">Mensagem do usuário e histórico de conversa</param>
        /// <returns>Resposta do chatbot com indicadores de relevância e sugestões</returns>
        [HttpPost("message")]
        public async Task<ActionResult<ChatbotResponseDto>> ProcessMessage([FromBody] ChatbotMessageRequestDto request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
                _logger.LogInformation($"Processando mensagem do chatbot para {userEmail}: {request.Message}");

                // Configurações OpenAI
                var apiKey = _configuration["OpenAI:ApiKey"];
                var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
                var baseUrl = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";

                if (string.IsNullOrEmpty(apiKey) || apiKey == "sua-api-key-aqui")
                {
                    _logger.LogWarning("OpenAI API Key não configurada");
                    return Ok(new ChatbotResponseDto
                    {
                        Message = "E aí! Vamos resolver isso. 1. Verifique se o mouse está conectado direito. Se for USB, tenta em outra porta. 2. Troque as pilhas, se for sem fio. 3. Reinicie o computador. Funcionou?",
                        IsITRelated = true,
                        SuggestTicketCreation = false
                    });
                }

                // Construir contexto da conversa com prompt otimizado para respostas curtas
                var messages = new List<object>
                {
                    new
                    {
                        role = "system",
                        content = "Voce e um assistente de TI. REGRAS: 1) Maximo 3 linhas por resposta. 2) NUNCA use markdown, asteriscos ou formatacao. 3) Use apenas texto simples. 4) Seja direto, sem introducoes. 5) Para saudacoes: cumprimente e pergunte o problema. 6) Para problemas: de 1-2 passos praticos. 7) Se nao for problema de TI, diga que so ajuda com TI. 8) Depois de dar solucao, pergunte se funcionou. Portugues brasileiro informal."
                    }
                };

                // Adicionar histórico de conversa (se houver)
                if (request.ConversationHistory != null && request.ConversationHistory.Any())
                {
                    foreach (var msg in request.ConversationHistory)
                    {
                        messages.Add(new
                        {
                            role = msg.Sender == "user" ? "user" : "assistant",
                            content = msg.Message
                        });
                    }
                }

                // Adicionar mensagem atual do usuário
                messages.Add(new
                {
                    role = "user",
                    content = request.Message
                });

                // Chamar OpenAI API
                var requestBody = new
                {
                    model = model,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 500
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync($"{baseUrl}/chat/completions", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Erro na chamada OpenAI: {responseContent}");
                    return Ok(new ChatbotResponseDto
                    {
                        Message = "Desculpe, tive um problema técnico. Pode tentar novamente? Se o problema persistir, posso encaminhar para um atendente humano.",
                        IsITRelated = true,
                        SuggestTicketCreation = false
                    });
                }

                var openAiResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var assistantMessage = openAiResponse
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "Desculpe, não entendi. Pode reformular?";

                // Analisar se deve criar chamado
                var shouldCreateTicket = assistantMessage.ToLower().Contains("chamado") ||
                                        assistantMessage.ToLower().Contains("atendente") ||
                                        assistantMessage.ToLower().Contains("técnico");

                return Ok(new ChatbotResponseDto
                {
                    Message = assistantMessage,
                    IsITRelated = true,
                    SuggestTicketCreation = shouldCreateTicket
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem do chatbot");
                return Ok(new ChatbotResponseDto
                {
                    Message = "Ops, tive um problema. Pode tentar novamente? Se não funcionar, posso chamar um atendente humano para você.",
                    IsITRelated = true,
                    SuggestTicketCreation = false
                });
            }
        }
    }

    public class ChatbotMessageRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatbotHistoryItemDto>? ConversationHistory { get; set; }
    }
}
