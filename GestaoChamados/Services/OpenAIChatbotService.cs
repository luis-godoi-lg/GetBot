using GestaoChamados.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GestaoChamados.Services
{
    public class OpenAIChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _apiUrl; // sempre termina com /v1
        private readonly int _maxTokens;
        private readonly double _temperature;

        // Palavras neutras/continua��o comuns em conversas
        private static readonly HashSet<string> _neutralFollowUps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sim","n�o","nao","ok","certo","beleza","blz","valeu","obrigado","obg","entendi","isso","positivo","confirmo","pode ser","vamos","tudo bem","t� bom","ta bom","n�o sei","nao sei",
            // Adiciona sauda��es comuns
            "ol�","oi","bom dia","boa tarde","boa noite","sauda��es","hello","hi"
        };

        // Lista de palavras-chave relacionadas a TI (ampliada)
        private readonly HashSet<string> _itKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Hardware - Computadores e Periféricos
            "computador", "pc", "laptop", "notebook", "desktop", "maquina", "máquina",
            "impressora", "scanner", "monitor", "teclado", "mouse", "webcam", "camera", "câmera",
            "headset", "fone", "microfone", "caixa de som", "alto-falante",
            "pendrive", "usb", "hd externo", "disco", "teclado", "mousepad",
            
            // Componentes de Hardware
            "hardware", "placa", "placa-mãe", "placa mae", "processador", "cpu",
            "memoria", "memória", "ram", "hd", "ssd", "disco rigido", "fonte",
            "gabinete", "cooler", "ventoinha", "bateria", "carregador", "cabo", "fio",
            
            // Tela e Vídeo
            "tela", "monitor", "display", "video", "vídeo", "imagem", "resolucao", "resolução",
            "tela azul", "tela preta", "bsod", "screen", "dual monitor", "segundo monitor",
            "ligar", "desligar", "acender", "apagar", "piscar", "piscando", "tremendo", "tremendo",
            
            // Rede e Conectividade
            "internet", "wifi", "wi-fi", "rede", "conexao", "conexão", "cabo de rede",
            "roteador", "modem", "switch", "lan", "wan", "ethernet", "ip",
            "dns", "ping", "velocidade", "sinal", "conectar", "desconectar",
            
            // Sistema Operacional
            "sistema", "windows", "linux", "mac", "so", "sistema operacional",
            "boot", "iniciar", "ligar", "desligar", "reiniciar", "restart", "reboot",
            "atualização", "atualizacao", "update", "upgrade", "instalação", "instalacao",
            "driver", "drivers", "configuração", "configuracao", "painel de controle",
            
            // Software e Aplicativos
            "software", "programa", "aplicativo", "app", "aplicação", "aplicacao",
            "excel", "word", "powerpoint", "outlook", "teams", "office", "365",
            "navegador", "browser", "chrome", "edge", "firefox", "safari",
            "pdf", "leitor", "adobe", "zip", "winrar", "compactador",
            
            // Problemas Comuns
            "erro", "bug", "falha", "problema", "defeito", "nao funciona", "não funciona",
            "lento", "travando", "travou", "congelou", "parou", "crashou", "crash",
            "quebrado", "quebrou", "corrompido", "danificado", "perdido",
            "demora", "demorado", "devagar", "lentidão", "lentidao", "engasgando",
            
            // Segurança
            "senha", "login", "acesso", "usuario", "usuário", "conta", "perfil",
            "virus", "vírus", "malware", "antivirus", "antivírus", "seguranca", "segurança",
            "firewall", "bloqueado", "bloqueio", "hackeado", "invadido", "spam",
            
            // Dados e Arquivos
            "backup", "arquivo", "pasta", "documento", "planilha", "dados",
            "salvar", "recuperar", "perdeu", "deletou", "apagou", "sumiu",
            "corrupto", "corrompido", "nao abre", "não abre",
            
            // Email e Comunicação
            "email", "e-mail", "outlook", "gmail", "mensagem", "enviar", "receber",
            "anexo", "spam", "caixa de entrada", "remetente", "destinatario", "destinatário",
            
            // Servidor e Banco de Dados
            "servidor", "server", "banco de dados", "bd", "database", "sql",
            "backup", "restore", "query", "tabela", "registro",
            
            // Impressão
            "imprimir", "impressao", "impressão", "papel", "tinta", "toner",
            "scanner", "digitalizar", "escanear", "copiadora", "xerox",
            
            // Outros termos técnicos
            "licenca", "licença", "ativação", "ativacao", "serial", "chave",
            "permissao", "permissão", "bloqueado", "travado", "congelado",
            "formatacao", "formatação", "formatar", "instalar", "desinstalar"
        };

        // Categorias de problemas e suas prioridades
        private readonly Dictionary<string, (string Priority, string Category)> _problemCategories = new()
        {
            { "senha", ("M�dia", "Acesso e Seguran�a") },
            { "login", ("M�dia", "Acesso e Seguran�a") },
            { "internet", ("Alta", "Conectividade") },
            { "rede", ("Alta", "Conectividade") },
            { "servidor", ("Cr�tica", "Infraestrutura") },
            { "sistema", ("Alta", "Software") },
            { "impressora", ("Baixa", "Hardware") },
            { "computador", ("M�dia", "Hardware") },
            { "v�rus", ("Cr�tica", "Seguran�a") },
            { "virus", ("Cr�tica", "Seguran�a") },
            { "backup", ("Alta", "Dados") },
            { "tela azul", ("Alta", "Sistema Operacional") }
        };

        public OpenAIChatbotService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            // Carrega configurações do appsettings.json
            _apiKey = _configuration["OpenAI:ApiKey"] ?? "sk-proj-demo-key-placeholder";
            _model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var rawBase = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
            var baseTrim = rawBase.TrimEnd('/');
            _apiUrl = baseTrim.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? baseTrim : baseTrim + "/v1"; // garante /v1
            _maxTokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "500");
            _temperature = double.Parse(_configuration["OpenAI:Temperature"] ?? "0.9", CultureInfo.InvariantCulture);
            
            // Configura HttpClient
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GestaoChamados/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // Timeout de 30 segundos
            
            Console.WriteLine($"[OpenAIChatbotService] INICIALIZADO");
            Console.WriteLine($"[OpenAIChatbotService] Modelo: {_model}");
            Console.WriteLine($"[OpenAIChatbotService] URL: {_apiUrl}");
            Console.WriteLine($"[OpenAIChatbotService] MaxTokens: {_maxTokens}");
            Console.WriteLine($"[OpenAIChatbotService] Temperature: {_temperature}");
        }

        private static string Normalize(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        }

        public async Task<ChatbotResponse> ProcessMessageAsync(string userMessage, List<ChatbotMessage> conversationHistory)
        {
            try
            {
                Console.WriteLine($"[OpenAIChatbotService] BaseUrl efetiva: {_apiUrl}");
                Console.WriteLine($"[OpenAIChatbotService] Processando mensagem: {userMessage}");
                Console.WriteLine($"[OpenAIChatbotService] Histórico de conversa: {conversationHistory.Count} mensagens");
                Console.WriteLine($"[OpenAIChatbotService] API Key configurada: {!string.IsNullOrEmpty(_apiKey) && _apiKey != "sua-api-key-aqui"}");
                Console.WriteLine($"[OpenAIChatbotService] Modelo configurado: {_model}");

                // SEMPRE aceita como contexto de TI - deixa a IA decidir
                // A própria IA vai filtrar se não for relacionado a TI
                Console.WriteLine("[OpenAIChatbotService] ✅ Aceitando como contexto técnico - IA decidirá");

                // SEMPRE usa IA
                Console.WriteLine("[OpenAIChatbotService] Chamando OpenAI API...");
                var aiResponse = await CallOpenAIAsync(userMessage, conversationHistory);
                
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    Console.WriteLine("[OpenAIChatbotService] IA respondeu com sucesso!");
                    
                    // Analisa se devemos sugerir criar um chamado (apenas após conversa prolongada)
                    var userMessages = conversationHistory.Where(m => m.Sender == "user").ToList();
                    Console.WriteLine($"[OpenAIChatbotService] Total de mensagens do usuário: {userMessages.Count}");
                    
                    // Só analisa sugestão de chamado se a conversa tiver pelo menos 5 trocas (para evitar sugestão precoce)
                    if (userMessages.Count >= 5)
                    {
                        var ticketSuggestion = await AnalyzeTicketRequirementAsync(conversationHistory);
                        Console.WriteLine($"[OpenAIChatbotService] Análise de chamado: ShouldCreate={ticketSuggestion.ShouldCreateTicket}, Reason={ticketSuggestion.Reason}");
                        
                        if (ticketSuggestion.ShouldCreateTicket)
                        {
                            Console.WriteLine($"[OpenAIChatbotService] ✅ Sugerindo criação de chamado");
                            return new ChatbotResponse
                            {
                                Message = aiResponse,
                                IsITRelated = true,
                                SuggestTicketCreation = true,
                                Priority = ticketSuggestion.Priority,
                                Category = ticketSuggestion.Category,
                                EndConversation = false
                            };
                        }
                    }
                    
                    return new ChatbotResponse
                    {
                        Message = aiResponse,
                        IsITRelated = true,
                        SuggestTicketCreation = false,
                        EndConversation = false
                    };
                }

                // Se a IA falhou, retorna erro genérico (não deve acontecer frequentemente)
                Console.WriteLine("[OpenAIChatbotService] ERRO: IA não respondeu!");
                return new ChatbotResponse
                {
                    Message = "Desculpe, estou com dificuldades técnicas no momento. Por favor, tente novamente em alguns instantes ou descreva seu problema de outra forma.",
                    IsITRelated = true,
                    SuggestTicketCreation = false,
                    EndConversation = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenAIChatbotService] Erro no processamento: {ex.Message}");
                Console.WriteLine($"[OpenAIChatbotService] Stack trace: {ex.StackTrace}");
                return GetFallbackResponse(userMessage, conversationHistory);
            }
        }

        private bool IsNeutralFollowUp(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return false;
            var text = Normalize(msg);
            if (_neutralFollowUps.Contains(text)) return true;
            // Considera mensagens muito curtas como continua��o
            if (text.Length <= 3) return true;
            return false;
        }

        private bool HasRecentITContext(List<ChatbotMessage> conversationHistory)
        {
            // Agora verifica as �ltimas 5 mensagens de AMBOS (usu�rio e bot)
            foreach (var message in conversationHistory.AsEnumerable().Reverse().Take(5))
            {
                var normalizedMessage = Normalize(message.Message ?? string.Empty);
                if (_itKeywords.Any(keyword => normalizedMessage.Contains(Normalize(keyword))))
                {
                    // Se encontrar qualquer palavra de TI no hist�rico recente, confirma o contexto.
                    return true;
                }
            }
            return false;
        }

        private bool IsITRelated(string message, List<ChatbotMessage> conversationHistory)
        {
            var normalizedMessage = Normalize(message ?? string.Empty);

            // 1. Verifica se a mensagem atual cont�m uma palavra-chave de TI
            bool containsITKeyword = _itKeywords.Any(keyword => normalizedMessage.Contains(Normalize(keyword)));
            if (containsITKeyword)
            {
                return true;
            }

            // 2. Se a mensagem atual for curta/neutra (como "sim", "n�o", "ok"),
            //    verifica se o hist�rico recente da conversa J� ERA sobre TI.
            if (IsNeutralFollowUp(normalizedMessage) && HasRecentITContext(conversationHistory))
            {
                return true;
            }

            // Se nenhuma das condi��es acima for atendida, n�o � relacionado a TI.
            return false;
        }

        private async Task<string?> CallOpenAIAsync(string userMessage, List<ChatbotMessage> conversationHistory)
        {
            try
            {
                Console.WriteLine($"[OpenAI] ==================== INICIANDO CHAMADA ====================");
                Console.WriteLine($"[OpenAI] API Key presente: {!string.IsNullOrEmpty(_apiKey)}");
                Console.WriteLine($"[OpenAI] API Key (20 chars): {_apiKey?.Substring(0, Math.Min(20, _apiKey.Length))}...");
                Console.WriteLine($"[OpenAI] Modelo: {_model}");
                Console.WriteLine($"[OpenAI] URL: {_apiUrl}");
                Console.WriteLine($"[OpenAI] MaxTokens: {_maxTokens}");
                Console.WriteLine($"[OpenAI] Temperature: {_temperature}");
                Console.WriteLine($"[OpenAI] Mensagem do usuário: {userMessage}");
                Console.WriteLine($"[OpenAI] Histórico: {conversationHistory.Count} mensagens");

                // Prompt de sistema otimizado para respostas curtas e objetivas
                var systemPrompt = "Voce e um assistente de TI. REGRAS: 1) Maximo 3 linhas por resposta. 2) NUNCA use markdown, asteriscos ou formatacao. 3) Use apenas texto simples. 4) Seja direto, sem introducoes. 5) Para saudacoes: cumprimente e pergunte o problema. 6) Para problemas: de 1-2 passos praticos. Portugues brasileiro informal.";

                // Monta mensagens
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                foreach (var msg in conversationHistory.TakeLast(8))
                {
                    messages.Add(new { role = msg.Sender == "user" ? "user" : "assistant", content = msg.Message });
                }
                messages.Add(new { role = "user", content = userMessage });

                // Decide endpoint conforme modelo
                if (_model.Contains("4.1"))
                {
                    // Usa /responses para modelos 4.1
                    var input = new List<object>();
                    input.Add(new { role = "system", content = new[] { new { type = "input_text", text = systemPrompt } } });
                    foreach (var msg in conversationHistory.TakeLast(8))
                    {
                        input.Add(new { role = msg.Sender == "user" ? "user" : "assistant", content = new[] { new { type = "input_text", text = msg.Message } } });
                    }
                    input.Add(new { role = "user", content = new[] { new {type = "input_text", text = userMessage } } });

                    var req = new
                    {
                        model = _model,
                        input = input,
                        temperature = _temperature,
                        max_output_tokens = _maxTokens
                    };

                    var json = JsonSerializer.Serialize(req, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    Console.WriteLine($"[OpenAI] Payload (/responses): {json.Substring(0, Math.Min(200, json.Length))}...");
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    // Simple retry/backoff for transient failures (429, 5xx)
                    HttpResponseMessage? response = null;
                    int maxRetries = 3;
                        for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        response = await _httpClient.PostAsync($"{_apiUrl}/responses", content);
                        Console.WriteLine($"[OpenAI] Tentativa {attempt} - Status da resposta: {response.StatusCode}");
                            if (response != null && response.IsSuccessStatusCode) break;

                        // On 401/403 provide a clear hint
                        if (response != null && (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden))
                        {
                            Console.WriteLine("[OpenAI] ERRO: Autorização falhou (401/403). Verifique a chave de API OpenAI (OpenAI:ApiKey) e se ela tem permissão para o endpoint.");
                            break; // don't retry on auth errors
                        }

                        // Retry on rate-limit or server errors
                        if (response != null && ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500))
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                            Console.WriteLine($"[OpenAI] Resposta transitória (status {(int)response.StatusCode}). Fazendo retry em {delay.TotalSeconds}s...");
                            await Task.Delay(delay);
                            continue;
                        }

                        // Non-retriable error
                        break;
                    }

                    var responseContent = response != null ? await response.Content.ReadAsStringAsync() : string.Empty;
                    Console.WriteLine($"[OpenAI] Resposta bruta: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...");

                    var result = JsonSerializer.Deserialize<OpenAIResponsesApiResponse>(responseContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (result != null)
                    {
                        if (!string.IsNullOrEmpty(result.OutputText)) return result.OutputText;
                        if (result.Output != null && result.Output.Length > 0)
                        {
                            var first = result.Output[0];
                            if (first.Content != null && first.Content.Length > 0)
                            {
                                var textPart = first.Content.FirstOrDefault(c => c.Type == "output_text" || c.Type == "text");
                                if (textPart != null && !string.IsNullOrEmpty(textPart.Text)) return textPart.Text;
                            }
                        }
                    }
                }
                else
                {
                    // Usa chat/completions para demais modelos
                    var requestBody = new
                    {
                        model = _model,
                        messages = messages,
                        max_tokens = _maxTokens,
                        temperature = _temperature,
                        presence_penalty = 0.1,
                        frequency_penalty = 0.1
                    };

                    var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    Console.WriteLine($"[OpenAI] 📤 Payload COMPLETO: {jsonContent}");
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    Console.WriteLine($"[OpenAI] 🌐 URL: {_apiUrl}/chat/completions");
                    Console.WriteLine($"[OpenAI] 🔑 API Key (primeiros 20 chars): {(string.IsNullOrEmpty(_apiKey) ? "(null)" : _apiKey.Substring(0, Math.Min(20, _apiKey.Length)))}...");
                    
                    // Simple retry/backoff for transient failures (429, 5xx)
                    HttpResponseMessage? response = null;
                    int maxRetries = 3;
                        for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        response = await _httpClient.PostAsync($"{_apiUrl}/chat/completions", content);
                        Console.WriteLine($"[OpenAI] Tentativa {attempt} - Status HTTP: {(int)response.StatusCode} {response.StatusCode}");
                            if (response != null && response.IsSuccessStatusCode) break;

                        if (response != null && (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden))
                        {
                            Console.WriteLine("[OpenAI] ERRO: Autorização falhou (401/403). Verifique a chave de API OpenAI (OpenAI:ApiKey) e se ela tem permissão para o endpoint.");
                            break; // don't retry on auth errors
                        }

                        if (response != null && ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500))
                        {
                            var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                            Console.WriteLine($"[OpenAI] Resposta transitória (status {(int)response.StatusCode}). Fazendo retry em {delay.TotalSeconds}s...");
                            await Task.Delay(delay);
                            continue;
                        }

                        // Non-retriable error - break and log below
                        break;
                    }

                    if (response != null)
                    {
                        Console.WriteLine($"[OpenAI] 📊 Status final: {(int)response.StatusCode} {response.StatusCode}");
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[OpenAI] ✅ SUCESSO! Status: {response.StatusCode}");
                        Console.WriteLine($"[OpenAI] Resposta completa (primeiros 800 chars): {responseContent.Substring(0, Math.Min(800, responseContent.Length))}...");

                        var result = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });

                        if (result?.Choices?.Length > 0)
                        {
                            var aiMessage = result.Choices.FirstOrDefault()?.Message?.Content;
                            Console.WriteLine($"[OpenAI] ✅ MENSAGEM EXTRAÍDA: {aiMessage}");
                            Console.WriteLine($"[OpenAI] Tokens usados: {result.Usage?.TotalTokens ?? 0}");
                            if (!string.IsNullOrEmpty(aiMessage)) return aiMessage;
                        }
                        else
                        {
                            Console.WriteLine("[OpenAI] ❌ ERRO: result.Choices está vazio ou nulo!");
                            Console.WriteLine($"[OpenAI] result is null? {result == null}");
                            Console.WriteLine($"[OpenAI] result.Choices is null? {result?.Choices == null}");
                            Console.WriteLine($"[OpenAI] result.Choices length: {result?.Choices?.Length ?? -1}");
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[OpenAI] ❌ ERRO na API: Status {(int)response.StatusCode} {response.StatusCode}");
                        Console.WriteLine($"[OpenAI] ❌ Detalhes COMPLETOS do erro: {errorContent}");
                        Console.WriteLine($"[OpenAI] ❌ Headers da resposta: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
                    }
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[OpenAI] ❌ EXCEÇÃO HTTP: {httpEx.Message}");
                Console.WriteLine($"[OpenAI] ❌ Stack trace HTTP: {httpEx.StackTrace}");
                Console.WriteLine($"[OpenAI] ❌ Inner exception: {httpEx.InnerException?.Message}");
            }
            catch (TaskCanceledException timeoutEx)
            {
                Console.WriteLine($"[OpenAI] ❌ TIMEOUT: A requisição excedeu 30 segundos");
                Console.WriteLine($"[OpenAI] ❌ Detalhes: {timeoutEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenAI] ❌ EXCEÇÃO GERAL: {ex.GetType().Name}");
                Console.WriteLine($"[OpenAI] ❌ Mensagem: {ex.Message}");
                Console.WriteLine($"[OpenAI] ❌ Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("[OpenAI] ⚠️ Retornando NULL - IA não conseguiu responder");
            return null; // Retorna null para usar fallback
        }

        public async Task<bool> ShouldCreateTicketAsync(List<ChatbotMessage> conversationHistory)
        {
            var suggestion = await AnalyzeTicketRequirementAsync(conversationHistory);
            return suggestion.ShouldCreateTicket;
        }

        public Task<TicketSuggestion> AnalyzeTicketRequirementAsync(List<ChatbotMessage> conversationHistory)
        {
            var userMessages = conversationHistory.Where(m => m.Sender == "user").ToList();
            
            Console.WriteLine($"[OpenAIChatbotService] AnalyzeTicketRequirement: {userMessages.Count} mensagens do usuário");
            
            // Apenas considere criar ticket se houver MUITAS mensagens (indicando problema persistente)
            if (userMessages.Count < 5)
            {
                Console.WriteLine($"[OpenAIChatbotService] Número insuficiente de mensagens ({userMessages.Count} < 5)");
                return Task.FromResult(new TicketSuggestion
                {
                    ShouldCreateTicket = false,
                    Reason = "Conversa ainda muito curta para criar chamado"
                });
            }

            var allUserText = Normalize(string.Join(" ", userMessages.Select(m => m.Message)));

            // Palavras que indicam problema NÃO resolvido
            var unsolvedKeywords = new[] 
            { 
                "ainda", "continua", "persiste", "nao funciona", "nao resolve", 
                "erro", "falha", "quebrado", "parou", "tela azul", "nao resolveu",
                "mesmo problema", "mesma coisa", "continua acontecendo"
            };
            bool hasUnsolvedIndicators = unsolvedKeywords.Any(keyword => allUserText.Contains(Normalize(keyword)));

            Console.WriteLine($"[OpenAIChatbotService] Indicadores de problema não resolvido: {hasUnsolvedIndicators}");

            var (priority, category) = DeterminePriorityAndCategory(allUserText);

            // SÓ sugere ticket se REALMENTE houver muitas mensagens E indicadores de que o problema persiste
            if (userMessages.Count >= 7 && hasUnsolvedIndicators)
            {
                Console.WriteLine($"[OpenAIChatbotService] ✅ Critério atendido: {userMessages.Count} mensagens + problema persiste");
                return Task.FromResult(new TicketSuggestion
                {
                    ShouldCreateTicket = true,
                    SuggestedTitle = GenerateTicketTitle(userMessages.First().Message),
                    SuggestedDescription = GenerateTicketDescription(conversationHistory),
                    Priority = priority,
                    Category = category,
                    Reason = "O problema persiste após várias tentativas. Requer atenção especializada."
                });
            }

            Console.WriteLine($"[OpenAIChatbotService] Critério não atendido para criar ticket");
            return Task.FromResult(new TicketSuggestion
            {
                ShouldCreateTicket = false,
                Reason = "Conversa não apresenta problema persistente o suficiente"
            });
        }

        private string GetBasicSolution(string userMessage)
        {
            string norm = Normalize(userMessage ?? string.Empty);

            // Problemas de Tela/Monitor
            if (norm.Contains(Normalize("tela")) || norm.Contains(Normalize("monitor")) || norm.Contains(Normalize("display")))
            {
                if (norm.Contains("nao liga") || norm.Contains("não liga") || norm.Contains("nao acende") || norm.Contains("não acende") || norm.Contains("preta") || norm.Contains("escura"))
                    return "**Tela não liga:**\n• Confira se o monitor está ligado (tomada + botão)\n• Cheque o cabo de vídeo (HDMI/VGA)\n• Teste apertar teclas do teclado\n\nLigou?";
                
                if (norm.Contains("azul") || norm.Contains("bsod"))
                    return "**Tela azul:**\n• Anote o código de erro\n• Reinicie o PC\n• Se repetir: F8 ao ligar → Modo Segurança\n\nResolveu?";
                    
                if (norm.Contains("tremendo") || norm.Contains("piscando") || norm.Contains("piscar"))
                    return "**Tela tremendo:**\n• Reconecte o cabo de vídeo\n• Clique direito na área de trabalho → Configurações de exibição → Taxa de atualização\n• Atualize driver da placa de vídeo\n\nMelhorou?";
            }

            // Problemas de Impressora
            if (norm.Contains(Normalize("impressora")) || norm.Contains(Normalize("imprimir")) || norm.Contains(Normalize("impressao")))
                return "**Impressora:**\n• Confira se está ligada e conectada\n• Desligue por 30s e ligue novamente\n• Verifique papel e tinta\n• Configurações → Impressoras → Imprimir teste\n\nImprimiu?";

            // Problemas de Internet/Rede
            if (norm.Contains(Normalize("internet")) || norm.Contains(Normalize("rede")) || norm.Contains("wifi") || norm.Contains("wi-fi"))
                return "**Sem internet:**\n• Reinicie o roteador (30s desligado)\n• Confira se WiFi está ativado ou cabo conectado\n• Configurações → Rede → Solução de problemas\n• Teste acessar google.com\n\nVoltou?";

            // Problemas de Computador/PC Geral
            if (norm.Contains(Normalize("computador")) || norm.Contains("pc") || norm.Contains(Normalize("laptop")) || norm.Contains(Normalize("notebook")))
            {
                if (norm.Contains("nao liga") || norm.Contains("não liga") || norm.Contains("nao inicia") || norm.Contains("não inicia"))
                    return "**PC não liga:**\n• Confira tomada (teste com outro aparelho)\n• Notebook: conecte carregador, aguarde 5min\n• Pressione botão ligar por 5s\n• Desktop: verifique botão da fonte\n\nLigou algo?";
                
                if (norm.Contains("lento") || norm.Contains("travando") || norm.Contains("travou") || norm.Contains("devagar") || norm.Contains("demora"))
                    return "**PC lento:**\n• Ctrl+Shift+Esc → veja CPU/Memória\n• Feche programas abertos\n• Reinicie o PC\n\nMelhorou?";
            }

            // Problemas de Senha/Login
            if (norm.Contains(Normalize("senha")) || norm.Contains("login") || norm.Contains("acesso") || norm.Contains("bloqueado"))
                return "**Senha/Login:**\n• Caps Lock está desligado?\n• Use 'Esqueci minha senha'\n• Limpe cache: Ctrl+Shift+Delete\n• Teste outro navegador\n\nConseguiu?";

            // Problemas de Sistema/Software
            if (norm.Contains(Normalize("sistema")) || norm.Contains(Normalize("software")) || norm.Contains(Normalize("programa")) || norm.Contains(Normalize("aplicativo")))
            {
                if (norm.Contains("nao abre") || norm.Contains("não abre") || norm.Contains("nao inicia") || norm.Contains("não inicia"))
                    return "**Programa não abre:**\n• Botão direito → Executar como Administrador\n• Reinicie o PC e tente\n• Gerenciador de Tarefas: já está aberto?\n\nAbriu?";
                
                return "**Problema no sistema:**\n• Feche e abra o programa\n• Reinicie o PC\n• Windows Update → Verificar atualizações\n\nFuncionou?";
            }

            // Problemas de Email
            if (norm.Contains("email") || norm.Contains("e-mail") || norm.Contains("outlook") || norm.Contains("mensagem"))
                return "**Email:**\n• Confira internet\n• Verifique pasta Spam\n• Feche e abra o Outlook\n\nResolveu?";

            // Resposta para follow-ups neutros
            if (IsNeutralFollowUp(norm))
                return "Me conte: o que aconteceu? Funcionou?";

            // Resposta genérica para problemas técnicos não específicos
            return "Conte mais:\n• O que está acontecendo?\n• Desde quando?\n• Aparece erro?\n• Já tentou reiniciar?";
        }

        private ChatbotResponse GetFallbackResponse(string userMessage, List<ChatbotMessage> conversationHistory)
        {
            if (!IsITRelated(userMessage, conversationHistory))
            {
                return new ChatbotResponse
                {
                    Message = "Desculpe, mas sou especializado apenas em quest�es de TI. Para outros assuntos, por favor, entre em contato com o departamento apropriado.",
                    IsITRelated = false,
                    SuggestTicketCreation = false,
                    EndConversation = false
                };
            }

            if (conversationHistory.Count <= 2)
            {
                return new ChatbotResponse
                {
                    Message = GetBasicSolution(userMessage),
                    IsITRelated = true,
                    SuggestTicketCreation = false,
                    EndConversation = false
                };
            }

            return new ChatbotResponse
            {
                Message = "Vejo que o problema persiste. Vou encaminhar para nosso suporte t�cnico para uma an�lise mais detalhada.",
                IsITRelated = true,
                SuggestTicketCreation = true,
                Priority = "M�dia",
                Category = "Suporte T�cnico",
                EndConversation = true
            };
        }

        private (string Priority, string Category) DeterminePriorityAndCategory(string message)
        {
            foreach (var category in _problemCategories)
            {
                if (message.Contains(Normalize(category.Key)))
                {
                    return (category.Value.Priority, category.Value.Category);
                }
            }
            return ("M�dia", "Suporte Geral");
        }

        private string GenerateTicketTitle(string firstMessage)
        {
            return firstMessage.Length > 50 ? firstMessage.Substring(0, 47) + "..." : firstMessage;
        }

        private string GenerateTicketDescription(List<ChatbotMessage> conversationHistory)
        {
            var description = new StringBuilder();
            description.AppendLine("=== Hist�rico da conversa com o ChatBot ===");
            
            foreach (var message in conversationHistory)
            {
                string sender = message.Sender == "user" ? "Usu�rio" : "ChatBot";
                description.AppendLine($"{sender}: {message.Message}");
            }
            
            description.AppendLine("\n=== An�lise do ChatBot ===");
            description.AppendLine("O problema n�o foi resolvido com as solu��es b�sicas sugeridas pelo chatbot.");
            description.AppendLine("Requer aten��o do suporte t�cnico especializado.");
            
            return description.ToString();
        }

        public Task<bool> AnalyzeProblemResolutionAsync(List<ChatbotMessage> conversationHistory)
        {
            Console.WriteLine($"[OpenAIChatbotService] Analisando se problema foi resolvido...");

            if (conversationHistory == null || conversationHistory.Count < 2)
            {
                Console.WriteLine($"[OpenAIChatbotService] Histórico insuficiente para análise");
                return Task.FromResult(false);
            }

            // Pegar as últimas 4 mensagens para análise (2 do usuário e 2 do bot)
            var recentMessages = conversationHistory.TakeLast(4).ToList();
            var lastUserMessage = recentMessages.LastOrDefault(m => m.Sender == "user")?.Message ?? string.Empty;
            var lastBotMessage = recentMessages.LastOrDefault(m => m.Sender == "bot")?.Message ?? string.Empty;

            Console.WriteLine($"[OpenAIChatbotService] Última mensagem do usuário: {lastUserMessage.Substring(0, Math.Min(50, lastUserMessage.Length))}...");
            Console.WriteLine($"[OpenAIChatbotService] Última mensagem do bot: {lastBotMessage.Substring(0, Math.Min(50, lastBotMessage.Length))}...");

            // Palavras-chave que indicam problema RESOLVIDO
            var resolvedKeywords = new[] 
            { 
                "resolveu", "resolvido", "funcionou", "funciona", "consegui", "deu certo",
                "obrigado", "obrigada", "valeu", "ajudou", "perfeito", "�timo", "otimo",
                "excelente", "foi", "j� est�", "ja esta", "est� funcionando", "esta funcionando",
                "consertou", "arrumou", "voltou", "normalizou", "beleza", "top", "show",
                "problema resolvido", "tudo certo", "tudo bem", "est� ok", "esta ok", "ok agora"
            };

            // Palavras-chave que indicam problema N�O RESOLVIDO
            var unresolvedKeywords = new[] 
            { 
                "ainda", "continua", "persiste", "n�o funciona", "nao funciona", 
                "n�o resolve", "nao resolve", "n�o resolveu", "nao resolveu",
                "erro", "falha", "quebrado", "parou", "tela azul", "problema",
                "mesmo problema", "mesma coisa", "continua acontecendo", "n�o consegui", 
                "nao consegui", "n�o deu", "nao deu", "piorou", "ainda n�o", "ainda nao"
            };

            var normalizedUser = Normalize(lastUserMessage);
            var normalizedBot = Normalize(lastBotMessage);

            // Verifica se o usu�rio indicou que resolveu
            bool userIndicatesResolved = resolvedKeywords.Any(keyword => 
                normalizedUser.Contains(Normalize(keyword)));

            // Verifica se o usu�rio indicou que N�O resolveu
            bool userIndicatesUnresolved = unresolvedKeywords.Any(keyword => 
                normalizedUser.Contains(Normalize(keyword)));

            // Verifica se o bot fez uma pergunta confirmando resolu��o
            bool botAskedConfirmation = normalizedBot.Contains("resolveu") || 
                                       normalizedBot.Contains("funcionou") ||
                                       normalizedBot.Contains("conseguiu") ||
                                       normalizedBot.Contains("deu certo") ||
                                       normalizedBot.Contains("melhorou");

            Console.WriteLine($"[OpenAIChatbotService] Usuário indica resolução: {userIndicatesResolved}");
            Console.WriteLine($"[OpenAIChatbotService] Usuário indica problema persiste: {userIndicatesUnresolved}");
            Console.WriteLine($"[OpenAIChatbotService] Bot perguntou confirmação: {botAskedConfirmation}");

            // CORREÇÃO: Só considera resolvido se o usuário usou palavras MUITO explícitas de confirmação
            // Palavras fracas como "ok", "beleza" não devem contar sozinhas
            var strongConfirmationKeywords = new[] 
            { 
                "resolveu", "resolvido", "funcionou", "funciona", "consegui", "deu certo",
                "perfeito", "excelente", "problema resolvido", "esta ok", "está ok"
            };
            
            bool userStronglyConfirms = strongConfirmationKeywords.Any(keyword => 
                normalizedUser.Contains(Normalize(keyword)));

            // Considera resolvido APENAS se:
            // 1. Usuário usou palavras FORTES de confirmação
            // 2. E NÃO usou palavras de problema
            bool isProblemResolved = userStronglyConfirms && !userIndicatesUnresolved;

            Console.WriteLine($"[OpenAIChatbotService] Confirmação forte do usuário: {userStronglyConfirms}");
            Console.WriteLine($"[OpenAIChatbotService] Resultado da análise: {(isProblemResolved ? "RESOLVIDO" : "NÃO RESOLVIDO")}");

            return Task.FromResult(isProblemResolved);
        }
    }

    // Chat Completions
    public class OpenAIResponse
    {
        public OpenAIChoice[]? Choices { get; set; }
        public OpenAIUsage? Usage { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage? Message { get; set; }
        public string? FinishReason { get; set; }
    }

    public class OpenAIMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    public class OpenAIUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    // Responses API
    public class OpenAIResponsesApiResponse
    {
        public string? OutputText { get; set; }
        public OpenAIResponsesApiOutput[]? Output { get; set; }
    }

    public class OpenAIResponsesApiOutput
    {
        public OpenAIResponsesApiContent[]? Content { get; set; }
    }

    public class OpenAIResponsesApiContent
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }
}