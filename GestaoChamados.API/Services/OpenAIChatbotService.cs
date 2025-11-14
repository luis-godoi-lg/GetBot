    using GestaoChamados.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace GestaoChamados.Services
{
    /// <summary>
    /// Serviço de chatbot inteligente usando OpenAI API
    /// Identifica problemas de TI, sugere soluções e determina quando escalar para atendimento humano
    /// </summary>
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
            "computador", "pc", "laptop", "notebook", "desktop",
            "impressora", "scanner", "monitor", "teclado", "mouse",
            "internet", "wifi", "rede", "conex�o", "conexao", "cabo",
            "sistema", "software", "programa", "aplicativo", "app",
            "senha", "login", "acesso", "usu�rio", "usuario", "conta",
            "email", "outlook", "teams", "office", "windows",
            "erro", "bug", "falha", "problema", "lento", "travando", "travou",
            "v�rus", "virus", "malware", "antiv�rus", "antivirus", "seguran�a", "seguranca",
            "backup", "arquivo", "pasta", "documento", "excel",
            "servidor", "banco de dados", "sql", "bd",
            "hardware", "placa", "mem�ria", "memoria", "hd", "ssd",
            "tela azul", "bsod", "driver", "boot", "iniciar", "ligar"
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
            
            // Carrega configura��es do appsettings.json
            _apiKey = _configuration["OpenAI:ApiKey"] ?? "sk-proj-demo-key-placeholder";
            _model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
            var rawBase = _configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1";
            var baseTrim = rawBase.TrimEnd('/');
            _apiUrl = baseTrim.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) ? baseTrim : baseTrim + "/v1"; // garante /v1
            _maxTokens = int.Parse(_configuration["OpenAI:MaxTokens"] ?? "1000");
            _temperature = double.Parse(_configuration["OpenAI:Temperature"] ?? "0.7");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GestaoChamados/1.0");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
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
                Console.WriteLine($"[OpenAIChatbotService] Hist�rico de conversa: {conversationHistory.Count} mensagens");
                Console.WriteLine($"[OpenAIChatbotService] API Key configurada: {!string.IsNullOrEmpty(_apiKey) && _apiKey != "sua-api-key-aqui"}");
                Console.WriteLine($"[OpenAIChatbotService] Modelo configurado: {_model}");

                // Verifica se � TI ou continua��o de contexto de TI
                bool isITContext = IsITRelated(userMessage, conversationHistory);
                Console.WriteLine($"[OpenAIChatbotService] � contexto de TI: {isITContext}");

                if (!isITContext)
                {
                    return new ChatbotResponse
                    {
                        Message = "Desculpe, mas sou especializado apenas em quest�es de TI. Para outros assuntos, por favor, entre em contato com o departamento apropriado. Posso ajud�-lo com problemas de computador, internet, sistemas, impressoras e outras quest�es t�cnicas.",
                        IsITRelated = false,
                        SuggestTicketCreation = false,
                        EndConversation = false
                    };
                }

                // Sempre tenta usar IA primeiro em contexto de TI
                Console.WriteLine("[OpenAIChatbotService] Tentando usar IA...");
                var aiResponse = await CallOpenAIAsync(userMessage, conversationHistory);
                
                if (!string.IsNullOrEmpty(aiResponse))
                {
                    Console.WriteLine("[OpenAIChatbotService] IA respondeu com sucesso!");
                    
                    // Verifica se o problema foi resolvido
                    bool problemResolved = IsProblemResolved(userMessage);
                    
                    if (problemResolved)
                    {
                        return new ChatbotResponse
                        {
                            Message = aiResponse,
                            IsITRelated = true,
                            SuggestTicketCreation = false,
                            EndConversation = true,
                            ProblemResolved = true,
                            ActionButtons = new List<ActionButton>
                            {
                                new ActionButton { Label = "✅ Fechar Chamado", Action = "close_ticket", CssClass = "btn-success" },
                                new ActionButton { Label = "🔄 Continuar Chamado", Action = "continue_ticket", CssClass = "btn-warning" }
                            }
                        };
                    }
                    
                    return new ChatbotResponse
                    {
                        Message = aiResponse,
                        IsITRelated = true,
                        SuggestTicketCreation = false,
                        EndConversation = false,
                        ProblemResolved = false
                    };
                }

                Console.WriteLine("[OpenAIChatbotService] IA n�o respondeu, usando fallback...");

                // Se j� tentou solu��es b�sicas v�rias vezes, analisa se precisa criar chamado
                var userMessages = conversationHistory.Where(m => m.Sender == "user").ToList();
                if (userMessages.Count >= 3)
                {
                    var ticketSuggestion = await AnalyzeTicketRequirementAsync(conversationHistory);
                    
                    if (ticketSuggestion.ShouldCreateTicket)
                    {
                        return new ChatbotResponse
                        {
                            Message = $"Entendo que o problema persiste. {ticketSuggestion.Reason} Vou criar um chamado para nosso suporte t�cnico. Prioridade: {ticketSuggestion.Priority}",
                            IsITRelated = true,
                            SuggestTicketCreation = true,
                            Priority = ticketSuggestion.Priority,
                            Category = ticketSuggestion.Category,
                            EndConversation = true
                        };
                    }
                }

                // Fallback para solu��es b�sicas
                var basicSolution = GetBasicSolution(userMessage);
                return new ChatbotResponse
                {
                    Message = basicSolution,
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

        /// <summary>
        /// Detecta se o usu�rio afirmou que o problema foi resolvido
        /// </summary>
        private bool IsProblemResolved(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            
            var normalizedMessage = Normalize(message);
            
            // Palavras-chave positivas que indicam resolu��o
            var resolutionKeywords = new[] 
            { 
                "sim", "pronto", "resolvido", "funcionou", "funcionando", "problema resolvido",
                "ok", "certo", "beleza", "blz", "valeu", "obrigado", "obg", "muito obrigado",
                "ta bom", "t� bom", "ta ok", "t� ok", "foi resolvido", "estava resolvido",
                "consegui", "funcionou", "consigo", "funcionando agora", "agora funciona",
                "pronto, resolvido", "perfeito", "maravilhoso", "excelente", "top",
                "consegui resolver", "ja resolveu", "já resolveu", "voltou a funcionar",
                "ta funcionando", "t� funcionando", "voltou", "ja funciona", "já funciona"
            };

            return resolutionKeywords.Any(keyword => normalizedMessage.Contains(Normalize(keyword)));
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

        private async Task<string> CallOpenAIAsync(string userMessage, List<ChatbotMessage> conversationHistory)
        {
            try
            {
                Console.WriteLine($"[OpenAI] Iniciando chamada para API...");
                Console.WriteLine($"[OpenAI] API Key: {_apiKey.Substring(0, Math.Min(20,_apiKey.Length))}...");
                Console.WriteLine($"[OpenAI] Modelo: {_model}");
                
                if (_apiKey == "sua-api-key-aqui" || string.IsNullOrEmpty(_apiKey))
                {
                    Console.WriteLine("[OpenAI] API Key n�o configurada, usando fallback");
                    return null; // Usa fallback
                }

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
                    var response = await _httpClient.PostAsync($"{_apiUrl}/responses", content);
                    Console.WriteLine($"[OpenAI] Status da resposta: {response.StatusCode}");
                    var responseContent = await response.Content.ReadAsStringAsync();
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

                    Console.WriteLine($"[OpenAI] Payload: {jsonContent.Substring(0, Math.Min(200, jsonContent.Length))}...");
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    Console.WriteLine($"[OpenAI] URL: {_apiUrl}/chat/completions");
                    var response = await _httpClient.PostAsync($"{_apiUrl}/chat/completions", content);
                    Console.WriteLine($"[OpenAI] Status da resposta: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[OpenAI] Resposta bruta: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}...");
                        var result = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                        if (result?.Choices?.Length > 0)
                        {
                            var aiMessage = result.Choices[0].Message.Content;
                            Console.WriteLine($"[OpenAI] Resposta processada: {aiMessage.Substring(0, Math.Min(100, aiMessage.Length))}...");
                            Console.WriteLine($"[OpenAI] Tokens usados: {result.Usage?.TotalTokens ?? 0}");
                            return aiMessage;
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"[OpenAI] Erro na API: {response.StatusCode} - {errorContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OpenAI] Exce��o na chamada da API: {ex.Message}");
                Console.WriteLine($"[OpenAI] Stack trace: {ex.StackTrace}");
            }

            return null; // Retorna null para usar fallback
        }

        public async Task<bool> ShouldCreateTicketAsync(List<ChatbotMessage> conversationHistory)
        {
            var suggestion = await AnalyzeTicketRequirementAsync(conversationHistory);
            return suggestion.ShouldCreateTicket;
        }

        public async Task<TicketSuggestion> AnalyzeTicketRequirementAsync(List<ChatbotMessage> conversationHistory)
        {
            var userMessages = conversationHistory.Where(m => m.Sender == "user").ToList();
            
            if (userMessages.Count < 2)
            {
                return new TicketSuggestion
                {
                    ShouldCreateTicket = false,
                    Reason = "Ainda tentando solu��es b�sicas"
                };
            }

            var allUserText = Normalize(string.Join(" ", userMessages.Select(m => m.Message)));

            var complexKeywords = new[] { "ainda", "continua", "persiste", "nao funciona", "n�o funciona", "nao resolve", "n�o resolve", "erro", "falha", "quebrado", "parou", "tela azul" };
            bool hasComplexIndicators = complexKeywords.Any(keyword => allUserText.Contains(Normalize(keyword)));

            var (priority, category) = DeterminePriorityAndCategory(allUserText);

            if (userMessages.Count >= 3 || hasComplexIndicators)
            {
                return new TicketSuggestion
                {
                    ShouldCreateTicket = true,
                    SuggestedTitle = GenerateTicketTitle(userMessages.First().Message),
                    SuggestedDescription = GenerateTicketDescription(conversationHistory),
                    Priority = priority,
                    Category = category,
                    Reason = hasComplexIndicators ? "O problema parece complexo e requer aten��o especializada." : "Solu��es b�sicas n�o resolveram o problema."
                };
            }

            return new TicketSuggestion
            {
                ShouldCreateTicket = false,
                Reason = "Ainda h� solu��es para tentar"
            };
        }

        private string GetBasicSolution(string userMessage)
        {
            string norm = Normalize(userMessage ?? string.Empty);

            if (norm.Contains(Normalize("impressora")))
                return "Para problemas com impressora, tente estas solu��es: 1) Verifique se est� ligada e conectada; 2) Reinicie a impressora; 3) Verifique se h� papel e tinta; 4) Tente imprimir um p�gina de teste. Isso resolveu o problema?";

            if (norm.Contains(Normalize("internet")) || norm.Contains(Normalize("rede")) || norm.Contains("wifi"))
                return "Para problemas de conex�o: 1) Verifique se o cabo de rede est� conectado (se usar cabo); 2) Reinicie o roteador (desligando por 30 segundos); 3) Desconecte e reconecte o WiFi; 4) Teste acessar um site simples. A conex�o voltou?";

            if (norm.Contains(Normalize("computador")) || norm.Contains("pc") || norm.Contains(Normalize("laptop")) || norm.Contains(Normalize("tela azul")))
                return "Para problemas no computador: 1) Salve o trabalho e reinicie o computador; 2) Verifique se todos os cabos est�o conectados; 3) Se n�o ligar, teste em outra tomada; 4) Aguarde completar a inicializa��o. Se aparecer 'tela azul', informe o c�digo de erro que aparece na tela. O problema foi resolvido?";

            if (norm.Contains(Normalize("senha")) || norm.Contains("login"))
                return "Para problemas de senha: 1) Verifique se o Caps Lock n�o est� ativado; 2) Tente usar a op��o 'Esqueci minha senha'; 3) Certifique-se de estar digitando o usu�rio correto; 4) Limpe o cache do navegador. Conseguiu fazer login?";

            if (norm.Contains(Normalize("sistema")) || norm.Contains(Normalize("software")) || norm.Contains(Normalize("programa")))
                return "Para problemas no sistema: 1) Feche e abra o programa novamente; 2) Reinicie o computador; 3) Verifique se h� atualiza��es pendentes; 4) Tente usar como administrador. O sistema voltou a funcionar?";

            if (IsNeutralFollowUp(norm))
                return "Perfeito. Pode me dizer o resultado do �ltimo passo que sugeri? Se n�o resolveu, descreva o comportamento atual para avan�armos.";

            return "Entendi que voc� tem um problema t�cnico. Para que eu possa ajudar melhor, voc� poderia dar mais detalhes sobre o que exatamente est� acontecendo?";
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

        public async Task<bool> AnalyzeProblemResolutionAsync(List<ChatbotMessage> conversationHistory)
        {
            Console.WriteLine($"[OpenAIChatbotService] Analisando se problema foi resolvido...");

            if (conversationHistory == null || conversationHistory.Count < 2)
            {
                Console.WriteLine($"[OpenAIChatbotService] Hist�rico insuficiente para an�lise");
                return false;
            }

            // Pegar as �ltimas 4 mensagens para an�lise (2 do usu�rio e 2 do bot)
            var recentMessages = conversationHistory.TakeLast(4).ToList();
            var lastUserMessage = recentMessages.LastOrDefault(m => m.Sender == "user")?.Message ?? string.Empty;
            var lastBotMessage = recentMessages.LastOrDefault(m => m.Sender == "bot")?.Message ?? string.Empty;

            Console.WriteLine($"[OpenAIChatbotService] �ltima mensagem do usu�rio: {lastUserMessage.Substring(0, Math.Min(50, lastUserMessage.Length))}...");
            Console.WriteLine($"[OpenAIChatbotService] �ltima mensagem do bot: {lastBotMessage.Substring(0, Math.Min(50, lastBotMessage.Length))}...");

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

            Console.WriteLine($"[OpenAIChatbotService] Usu�rio indica resolu��o: {userIndicatesResolved}");
            Console.WriteLine($"[OpenAIChatbotService] Usu�rio indica problema persiste: {userIndicatesUnresolved}");
            Console.WriteLine($"[OpenAIChatbotService] Bot perguntou confirma��o: {botAskedConfirmation}");

            // Considera resolvido se:
            // 1. Usu�rio usou palavras de resolu��o E n�o usou palavras de problema
            // 2. Bot perguntou e usu�rio confirmou positivamente
            bool isProblemResolved = userIndicatesResolved && !userIndicatesUnresolved;

            Console.WriteLine($"[OpenAIChatbotService] Resultado da an�lise: {(isProblemResolved ? "RESOLVIDO" : "N�O RESOLVIDO")}");

            return isProblemResolved;
        }
    }

    // Chat Completions
    public class OpenAIResponse
    {
        public OpenAIChoice[] Choices { get; set; }
        public OpenAIUsage Usage { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage Message { get; set; }
        public string FinishReason { get; set; }
    }

    public class OpenAIMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
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
        public string OutputText { get; set; }
        public OpenAIResponsesApiOutput[] Output { get; set; }
    }

    public class OpenAIResponsesApiOutput
    {
        public OpenAIResponsesApiContent[] Content { get; set; }
    }

    public class OpenAIResponsesApiContent
    {
        public string Type { get; set; }
        public string Text { get; set; }
    }
}