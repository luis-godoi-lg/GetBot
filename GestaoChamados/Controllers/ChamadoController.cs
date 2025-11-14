using GestaoChamados.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using GestaoChamados.Hubs;
using GestaoChamados.Services;
using GestaoChamados.Shared.DTOs;
using System.Text.Json;

namespace GestaoChamados.Controllers
{
    [Authorize]
    public class ChamadoController : Controller
    {
        // --- PROPRIEDADES E DADOS EM MEMÓRIA ---

        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<SupportHub> _hubContext;
        private readonly IChatbotService _chatbotService;
        private readonly ApiService _apiService;
        private readonly ILogger<ChamadoController> _logger;

        
        // REMOVIDO: Lista estática _chamados - agora usa API (backend)
        // REMOVIDO: _protocoloCounter - agora o ID é auto-incrementado pelo banco de dados
        
        public static List<ChatMessageModel> _chatMessages = new List<ChatMessageModel>();
        
        // Armazena as conversas do chatbot por sessão
        private static Dictionary<string, List<ChatbotMessage>> _chatbotConversations = new Dictionary<string, List<ChatbotMessage>>();

        // --- CONSTRUTOR ---
        public ChamadoController(
            IWebHostEnvironment webHostEnvironment, 
            IHubContext<SupportHub> hubContext, 
            IChatbotService chatbotService,
            ApiService apiService,
            ILogger<ChamadoController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _hubContext = hubContext;
            _chatbotService = chatbotService;
            _apiService = apiService;
            _logger = logger;
        }

        // --- ACTIONS PARA VISUALIZAÇÃO DE PÁGINAS (VIEWS) ---

        public async Task<IActionResult> Index()
        {
            Console.WriteLine("=======================================================");
            Console.WriteLine("[ChamadoController] 📋 MÉTODO INDEX CHAMADO!");
            var userEmail = User.Identity.Name;
            Console.WriteLine($"[ChamadoController] Usuário: {userEmail}");
            
            try
            {
                // Busca chamados via API
                var response = await _apiService.GetAsync<List<ChamadoDto>>("/api/chamados");
                
                if (response == null || !response.Any())
                {
                    Console.WriteLine("[ChamadoController] Nenhum chamado retornado pela API");
                    return View(new List<ChamadoModel>());
                }
                
                // Filtrar chamados baseado no role
                var chamadosFiltrados = response;
                
                if (User.IsInRole("Tecnico"))
                {
                    // Técnico vê APENAS os chamados que ele atendeu
                    chamadosFiltrados = response
                        .Where(c => !string.IsNullOrEmpty(c.TecnicoNome) && 
                                   c.TecnicoNome.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    Console.WriteLine($"[ChamadoController] Técnico {userEmail} - Filtrando apenas chamados atendidos por ele");
                }
                // Gerente e Admin veem todos (sem filtro adicional)
                // Usuário comum já vem filtrado pela API
                
                // Converte DTOs para Models para a View
                var chamados = chamadosFiltrados.Select(dto => new ChamadoModel
                {
                    Protocolo = dto.Id,
                    Assunto = dto.Titulo,
                    Descricao = dto.Descricao,
                    Status = dto.Status,
                    DataAbertura = dto.DataCriacao,
                    UsuarioCriadorEmail = dto.UsuarioEmail,
                    TecnicoAtribuidoEmail = dto.TecnicoNome,
                    Rating = dto.Rating
                }).OrderByDescending(c => c.DataAbertura).ToList();
                
                Console.WriteLine($"[ChamadoController] Retornando {chamados.Count} chamados da API");
                foreach (var c in chamados)
                {
                    Console.WriteLine($"[ChamadoController]   - #{c.Protocolo}: {c.Assunto} ({c.Status})");
                }
                Console.WriteLine("=======================================================");
                
                return View(chamados);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChamadoController] ❌ Erro ao buscar chamados: {ex.Message}");
                Console.WriteLine("=======================================================");
                TempData["ErrorMessage"] = "Erro ao carregar chamados. Tente novamente.";
                return View(new List<ChamadoModel>());
            }
        }

        public async Task<IActionResult> Detalhes(int id)
        {
            Console.WriteLine("=======================================================");
            Console.WriteLine($"[ChamadoController] 🔍 MÉTODO DETALHES CHAMADO!");
            Console.WriteLine($"[ChamadoController] Buscando detalhes do chamado ID: {id}");
            Console.WriteLine($"[ChamadoController] Usuário atual: {User.Identity?.Name ?? "Não identificado"}");
            Console.WriteLine($"[ChamadoController] É Técnico? {User.IsInRole("Tecnico")}");
            Console.WriteLine($"[ChamadoController] É Usuário? {User.IsInRole("Usuario")}");
            
            try
            {
                // Busca chamado via API
                var dto = await _apiService.GetAsync<ChamadoDto>($"/api/chamados/{id}");
                
                if (dto == null)
                {
                    Console.WriteLine($"[ChamadoController] ❌ Chamado #{id} não encontrado!");
                    return NotFound();
                }
                
                // Converte DTO para Model
                var chamado = new ChamadoModel
                {
                    Protocolo = dto.Id,
                    Assunto = dto.Titulo,
                    Descricao = dto.Descricao,
                    Status = dto.Status,
                    DataAbertura = dto.DataCriacao,
                    UsuarioCriadorEmail = dto.UsuarioEmail,
                    TecnicoAtribuidoEmail = dto.TecnicoNome,
                    Rating = dto.Rating
                };
                
                Console.WriteLine($"[ChamadoController] ✅ Chamado #{id} encontrado:");
                Console.WriteLine($"[ChamadoController]   - Assunto: {chamado.Assunto}");
                Console.WriteLine($"[ChamadoController]   - Status: {chamado.Status}");
                Console.WriteLine($"[ChamadoController]   - Criador: {chamado.UsuarioCriadorEmail}");
                Console.WriteLine($"[ChamadoController]   - Técnico: {chamado.TecnicoAtribuidoEmail ?? "Não atribuído"}");
                Console.WriteLine($"[ChamadoController]   - Descrição (primeiros 50 chars): {(chamado.Descricao?.Length > 50 ? chamado.Descricao.Substring(0, 50) : chamado.Descricao)}");
                
                var chatHistoryList = _chatMessages.Where(m => m.TicketId == id).OrderBy(m => m.Timestamp).ToList();
                ViewBag.ChatHistory = chatHistoryList;
                ViewBag.IsTecnico = User.IsInRole("Tecnico");
                
                Console.WriteLine($"[ChamadoController] Histórico de chat: {chatHistoryList.Count} mensagens");
                Console.WriteLine($"[ChamadoController] ✅ Retornando View com o chamado");
                Console.WriteLine("=======================================================");
                
                return View(chamado);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChamadoController] ❌ Erro ao buscar detalhes: {ex.Message}");
                Console.WriteLine("=======================================================");
                return NotFound();
            }
        }

        [Authorize(Roles = "Usuario")]
        public IActionResult Novo()
        {
            return View();
        }

        // --- MÉTODO AUXILIAR ---
        private bool IsBotAskingConfirmation(string botMessage)
        {
            if (string.IsNullOrWhiteSpace(botMessage))
                return false;

            var normalizedMessage = botMessage.ToLower()
                .Normalize(System.Text.NormalizationForm.FormD)
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ã", "a")
                .Replace("õ", "o").Replace("ç", "c");

            // Verifica se o bot está fazendo uma pergunta de confirmação de resolução
            var confirmationPhrases = new[]
            {
                "resolveu o problema",
                "resolveu seu problema",
                "problema foi resolvido",
                "conseguiu resolver",
                "funcionou",
                "isso resolveu",
                "posso finalizar",
                "posso encerrar",
                "deseja finalizar",
                "deseja encerrar",
                "problema resolvido",
                "tudo resolvido",
                "tudo certo agora"
            };

            return confirmationPhrases.Any(phrase => 
                normalizedMessage.Contains(phrase) && normalizedMessage.Contains("?"));
        }

        // --- NOVA API PARA O CHATBOT COM IA ---

        [HttpPost]
        public async Task<IActionResult> ProcessChatbotMessage([FromBody] ChatbotMessageRequest request)
        {
            try
            {
                Console.WriteLine($"[ChamadoController] Recebida mensagem do chatbot: {request.Message}");
                
                var sessionId = HttpContext.Session.Id;
                Console.WriteLine($"[ChamadoController] Session ID: {sessionId}");
                
                // Recupera ou cria a conversa da sessão
                if (!_chatbotConversations.ContainsKey(sessionId))
                {
                    _chatbotConversations[sessionId] = new List<ChatbotMessage>();
                    Console.WriteLine($"[ChamadoController] Nova conversa criada para sessão: {sessionId}");
                }

                var conversation = _chatbotConversations[sessionId];
                Console.WriteLine($"[ChamadoController] Conversa atual tem {conversation.Count} mensagens");
                
                // Adiciona a mensagem do usuário
                conversation.Add(new ChatbotMessage
                {
                    Sender = "user",
                    Message = request.Message,
                    Timestamp = DateTime.Now
                });

                Console.WriteLine($"[ChamadoController] Chamando serviço de IA...");
                
                // Processa a mensagem com IA
                var response = await _chatbotService.ProcessMessageAsync(request.Message, conversation);
                
                Console.WriteLine($"[ChamadoController] Resposta do serviço: {response.Message.Substring(0, Math.Min(100, response.Message.Length))}...");
                Console.WriteLine($"[ChamadoController] Relacionado a TI: {response.IsITRelated}");
                Console.WriteLine($"[ChamadoController] Sugerir criação de chamado: {response.SuggestTicketCreation}");
                
                // Adiciona a resposta do bot
                conversation.Add(new ChatbotMessage
                {
                    Sender = "bot",
                    Message = response.Message,
                    Timestamp = DateTime.Now
                });

                // Verifica se o problema foi resolvido
                var isProblemResolved = await _chatbotService.AnalyzeProblemResolutionAsync(conversation);
                Console.WriteLine($"[ChamadoController] Problema resolvido: {isProblemResolved}");

                // CORREÇÃO: Verifica se o bot está PERGUNTANDO sobre a resolução (não finalizando automaticamente)
                var isAskingConfirmation = IsBotAskingConfirmation(response.Message);
                Console.WriteLine($"[ChamadoController] Bot perguntando confirmação: {isAskingConfirmation}");

                // Se o bot sugeriu criar um chamado, prepara os dados
                if (response.SuggestTicketCreation)
                {
                    var ticketSuggestion = await _chatbotService.AnalyzeTicketRequirementAsync(conversation);
                    
                    return Json(new
                    {
                        success = true,
                        message = response.Message,
                        shouldCreateTicket = true,
                        isProblemResolved = isProblemResolved,
                        askingConfirmation = isAskingConfirmation,
                        ticketData = new
                        {
                            title = ticketSuggestion.SuggestedTitle,
                            description = ticketSuggestion.SuggestedDescription,
                            priority = ticketSuggestion.Priority,
                            category = ticketSuggestion.Category
                        },
                        endConversation = response.EndConversation
                    });
                }

                return Json(new
                {
                    success = true,
                    message = response.Message,
                    shouldCreateTicket = false,
                    isProblemResolved = isProblemResolved,
                    askingConfirmation = isAskingConfirmation,
                    isITRelated = response.IsITRelated,
                    endConversation = response.EndConversation
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChamadoController] Erro no processamento: {ex.Message}");
                Console.WriteLine($"[ChamadoController] Stack trace: {ex.StackTrace}");
                
                return Json(new
                {
                    success = false,
                    message = "Desculpe, ocorreu um erro. Por favor, tente novamente ou fale com um atendente.",
                    shouldCreateTicket = false
                });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Usuario")]
        public async Task<IActionResult> CreateTicketFromChatbot([FromBody] ChatbotTicketRequest request)
        {
            try
            {
                var criarDto = new CriarChamadoDto
                {
                    Titulo = request.Title,
                    Descricao = request.Description,
                    Prioridade = request.Priority ?? "Media"
                };
                
                var chamadoDto = await _apiService.PostAsync<CriarChamadoDto, ChamadoDto>("/api/chamados", criarDto);
                
                if (chamadoDto != null)
                {
                    // Notifica técnicos via SignalR
                    var chamado = new ChamadoModel
                    {
                        Protocolo = chamadoDto.Id,
                        Assunto = chamadoDto.Titulo,
                        Descricao = chamadoDto.Descricao,
                        Status = "Aguardando Atendente",
                        DataAbertura = chamadoDto.DataCriacao,
                        UsuarioCriadorEmail = User.Identity?.Name ?? ""
                    };
                    
                    await _hubContext.Clients.Group("Tecnicos").SendAsync("NovoUsuarioNaFila", chamado);
                    
                    // Limpa a conversa da sessão
                    var sessionId = HttpContext.Session.Id;
                    if (_chatbotConversations.ContainsKey(sessionId))
                    {
                        _chatbotConversations.Remove(sessionId);
                    }
                    
                    return Json(new { success = true, ticketId = chamadoDto.Id });
                }
                
                return Json(new { success = false, message = "Erro ao criar chamado na API." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChamadoController] Erro ao criar chamado: {ex.Message}");
                return Json(new { success = false, message = "Erro ao criar chamado." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Usuario")]
        public async Task<IActionResult> FinalizarProblemaResolvido()
        {
            try
            {
                Console.WriteLine($"[ChamadoController] Problema resolvido via chatbot - criando registro");
                
                var sessionId = HttpContext.Session.Id;
                var userEmail = User.Identity?.Name ?? "usuario@desconhecido.com";
                
                // Recupera a conversa para gerar o resumo
                string assunto = "Suporte via Chatbot";
                string descricao = "Problema resolvido automaticamente pelo chatbot.";
                
                if (_chatbotConversations.ContainsKey(sessionId))
                {
                    var conversation = _chatbotConversations[sessionId];
                    
                    // Pega a primeira mensagem do usuário como assunto
                    var firstUserMessage = conversation.FirstOrDefault(m => m.Sender == "user")?.Message;
                    if (!string.IsNullOrEmpty(firstUserMessage))
                    {
                        assunto = firstUserMessage.Length > 50 
                            ? firstUserMessage.Substring(0, 47) + "..." 
                            : firstUserMessage;
                    }
                    
                    // Monta a descrição com o histórico da conversa
                    var descricaoBuilder = new System.Text.StringBuilder();
                    descricaoBuilder.AppendLine("=== PROBLEMA RESOLVIDO PELO CHATBOT ===");
                    descricaoBuilder.AppendLine();
                    descricaoBuilder.AppendLine("Histórico da Conversa:");
                    descricaoBuilder.AppendLine("------------------------");
                    
                    foreach (var msg in conversation)
                    {
                        var sender = msg.Sender == "user" ? "Usuário" : "Chatbot";
                        descricaoBuilder.AppendLine($"[{sender}]: {msg.Message}");
                        descricaoBuilder.AppendLine();
                    }
                    
                    descricaoBuilder.AppendLine("------------------------");
                    descricaoBuilder.AppendLine($"Status: Resolvido automaticamente em {DateTime.Now:dd/MM/yyyy HH:mm}");
                    descricaoBuilder.AppendLine("O problema foi resolvido com sucesso através das orientações do chatbot.");
                    
                    descricao = descricaoBuilder.ToString();
                    
                    // Remove a conversa após criar o registro
                    _chatbotConversations.Remove(sessionId);
                    Console.WriteLine($"[ChamadoController] Conversa removida para sessão: {sessionId}");
                }
                
                // Cria o chamado via API com status "Resolvido"
                var criarDto = new CriarChamadoDto
                {
                    Titulo = assunto,
                    Descricao = descricao,
                    Prioridade = "Baixa"
                };
                
                var chamadoDto = await _apiService.PostAsync<CriarChamadoDto, ChamadoDto>("/api/chamados", criarDto);
                
                if (chamadoDto != null)
                {
                    // Marca imediatamente como resolvido via API
                    await _apiService.PostAsync<object>($"/api/chamados/{chamadoDto.Id}/fechar", new { });
                    
                    Console.WriteLine($"[ChamadoController] Chamado #{chamadoDto.Id} criado como 'Resolvido' para {userEmail}");
                    
                    return Json(new 
                    { 
                        success = true, 
                        message = "Problema resolvido com sucesso!",
                        ticketId = chamadoDto.Id 
                    });
                }
                
                return Json(new { success = false, message = "Erro ao criar registro na API." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChamadoController] Erro ao finalizar problema resolvido: {ex.Message}");
                return Json(new { success = false, message = "Erro ao finalizar." });
            }
        }

        // --- ACTIONS CHAMADAS POR FORMULÁRIOS E SCRIPTS (POST/GET) ---

        [HttpPost]
        [Authorize(Roles = "Usuario")]
        public async Task<IActionResult> EntrarNaFila([FromBody] NovoChamadoViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Criar chamado com status "Aguardando Atendente"
                    var criarDto = new CriarChamadoDto
                    {
                        Titulo = model.Assunto,
                        Descricao = model.Descricao,
                        Prioridade = "Media",
                        Status = "Aguardando Atendente"  // ✅ Define status correto
                    };
                    
                    var chamadoDto = await _apiService.PostAsync<CriarChamadoDto, ChamadoDto>("/api/chamados", criarDto);
                    
                    if (chamadoDto != null)
                    {
                        // ✅ Notificar técnicos via SignalR LOCAL (Web Hub)
                        try
                        {
                            await _hubContext.Clients.Group("Tecnicos").SendAsync("NovoUsuarioNaFila", new
                            {
                                Id = chamadoDto.Id,
                                Protocolo = chamadoDto.Id,
                                Titulo = chamadoDto.Titulo,
                                Assunto = chamadoDto.Titulo,
                                Usuario = User.Identity?.Name,
                                UsuarioCriadorEmail = User.Identity?.Name,
                                UsuarioEmail = User.Identity?.Name,
                                Status = chamadoDto.Status
                            });
                            _logger.LogInformation($"[EntrarNaFila] SignalR local notificado para chamado #{chamadoDto.Id}");
                        }
                        catch (Exception signalREx)
                        {
                            _logger.LogWarning($"[EntrarNaFila] Erro ao notificar SignalR: {signalREx.Message}");
                        }
                        
                        return Json(new { success = true, ticketId = chamadoDto.Id });
                    }
                    
                    return Json(new { success = false, message = "Erro ao criar chamado na API." });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChamadoController] Erro ao entrar na fila: {ex.Message}");
                    return Json(new { success = false, message = "Erro ao criar chamado." });
                }
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [Authorize(Roles = "Tecnico,Superior")]
        public async Task<IActionResult> AssumirChamado(int id)
        {
            _logger.LogInformation($"[AssumirChamado] Técnico {User.Identity?.Name} tentando assumir chamado {id}");
            
            try
            {
                _logger.LogInformation($"[AssumirChamado] Chamando API: POST /api/chamados/{id}/assumir");
                
                // Corrigido: Usar o método correto que retorna HttpResponseMessage
                var response = await _apiService.PostAsync($"/api/chamados/{id}/assumir", (object?)null);
                
                _logger.LogInformation($"[AssumirChamado] Status HTTP recebido: {response.StatusCode}");
                
                // Corrigido: Verificar o status HTTP corretamente
                if (response.IsSuccessStatusCode)
                {
                    // Corrigido: Ler o conteúdo JSON da resposta
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"[AssumirChamado] Resposta da API: {responseContent}");
                    
                    // Só dispara SignalR se a API retornou sucesso
                    await _hubContext.Clients.Group($"ticket-{id}").SendAsync("TechnicianJoined");
                    _logger.LogInformation($"[AssumirChamado] SignalR TechnicianJoined enviado para ticket-{id}");
                    
                    TempData["SuccessMessage"] = "Chamado assumido com sucesso.";
                }
                else
                {
                    // Corrigido: Tratar erros HTTP adequadamente
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"[AssumirChamado] Erro HTTP {response.StatusCode}: {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        TempData["ErrorMessage"] = "Chamado não encontrado.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        TempData["ErrorMessage"] = "Este chamado já está sendo atendido ou foi finalizado.";
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        TempData["ErrorMessage"] = "Você não tem permissão para assumir este chamado.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Erro ao assumir chamado. Status: {response.StatusCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AssumirChamado] Exceção ao assumir chamado {id}: {ex.Message}");
                _logger.LogError($"[AssumirChamado] StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"Erro ao comunicar com a API: {ex.Message}";
            }
            
            _logger.LogInformation($"[AssumirChamado] Redirecionando para Detalhes do chamado {id}");
            return RedirectToAction("Detalhes", new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Tecnico,Superior")]
        public async Task<IActionResult> MarcarComoResolvido(int id)
        {
            try
            {
                // Primeiro, busca os dados do chamado para pegar o email do criador
                var chamadoDto = await _apiService.GetAsync<ChamadoDto>($"/api/chamados/{id}");
                
                if (chamadoDto == null)
                {
                    _logger.LogWarning($"[MarcarComoResolvido] Chamado {id} não encontrado");
                    TempData["ErrorMessage"] = "Chamado não encontrado.";
                    return RedirectToAction("Index");
                }
                
                var response = await _apiService.PostAsync($"/api/chamados/{id}/resolver", (object?)null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[MarcarComoResolvido] Chamado #{id} marcado como resolvido pelo técnico {User.Identity?.Name}");
                    
                    // Dispara evento SignalR com o EMAIL DO USUÁRIO CRIADOR (não do técnico!)
                    await _hubContext.Clients.Group($"ticket-{id}").SendAsync("ShowSatisfactionSurvey", id.ToString(), chamadoDto.UsuarioEmail);
                    
                    _logger.LogInformation($"[MarcarComoResolvido] SignalR enviado para {chamadoDto.UsuarioEmail}");
                    
                    TempData["SuccessMessage"] = "Chamado marcado como resolvido. O usuário receberá uma pesquisa de satisfação.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"[MarcarComoResolvido] Erro HTTP {response.StatusCode}: {errorContent}");
                    TempData["ErrorMessage"] = "Erro ao marcar chamado como resolvido.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MarcarComoResolvido] Erro: {ex.Message}");
                _logger.LogError($"[MarcarComoResolvido] StackTrace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Erro ao marcar chamado como resolvido.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Avaliar(string id, int rating)
        {
            if (!int.TryParse(id, out int protocolo))
            {
                TempData["ErrorMessage"] = "Protocolo inválido.";
                return RedirectToAction("Index");
            }
            
            try
            {
                var avaliacaoDto = new { Rating = rating };
                var response = await _apiService.PostAsync<object, object>($"/api/chamados/{protocolo}/avaliar", avaliacaoDto);
                
                if (response != null)
                {
                    Console.WriteLine($"[ChamadoController] Avaliação registrada: Chamado #{protocolo} - Rating: {rating} estrelas");
                    TempData["SuccessMessage"] = $"Avaliação registrada com sucesso! Obrigado por avaliar nosso atendimento com {rating} estrela{(rating > 1 ? "s" : "")}!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Erro ao registrar avaliação.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChamadoController] Erro ao avaliar: {ex.Message}");
                TempData["ErrorMessage"] = "Erro ao registrar avaliação.";
            }
            
            // Sempre redireciona para a lista após avaliar
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Ação para fechar chamado via AJAX (chamado pelo chatbot quando problema foi resolvido)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> FecharChamado([FromBody] Dictionary<string, object> data)
        {
            try
            {
                if (data == null || !data.ContainsKey("id"))
                {
                    return Json(new { success = false, message = "ID do chamado não fornecido." });
                }

                if (!int.TryParse(data["id"].ToString(), out int id))
                {
                    return Json(new { success = false, message = "ID inválido." });
                }

                var response = await _apiService.PostAsync($"/api/chamados/{id}/fechar", (object?)null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"[FecharChamado] Chamado {id} fechado com sucesso pelo usuário {User.Identity?.Name}");
                    
                    // Não precisa enviar SignalR aqui porque o usuário já está vendo a página
                    // A pesquisa de satisfação será mostrada ao recarregar
                    
                    return Json(new { success = true, message = "Chamado fechado com sucesso!" });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"[FecharChamado] Erro HTTP {response.StatusCode}: {errorContent}");
                    return Json(new { success = false, message = "Erro ao fechar chamado na API." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[FecharChamado] Erro: {ex.Message}");
                return Json(new { success = false, message = $"Erro: {ex.Message}" });
            }
        }

        // Mantém compatibilidade com o sistema antigo
        [HttpGet]
        public IActionResult GetKnowledgeBase()
        {
            var keywords = new[] { "impressora", "senha", "computador", "sistema", "internet", "rede" };
            return Json(keywords);
        }
    }

    // Classes para as requisições do chatbot
    public class ChatbotMessageRequest
    {
        public string Message { get; set; }
    }

    public class ChatbotTicketRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Category { get; set; }
    }
}