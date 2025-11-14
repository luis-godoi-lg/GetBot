using System.Collections.ObjectModel;
using System.Windows.Input;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Mobile.ViewModels;

public class ChatMessage
{
    public string Text { get; set; } = string.Empty;
    public bool IsUserMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class NovoChamadoViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private string _messageText = string.Empty;
    private string _userEmail = string.Empty;
    private ObservableCollection<ChatMessage> _messages = new();
    private List<ChatbotHistoryItemDto> _conversationHistory = new();

    public string MessageText
    {
        get => _messageText;
        set => SetProperty(ref _messageText, value);
    }

    public string UserEmail
    {
        get => _userEmail;
        set => SetProperty(ref _userEmail, value);
    }

    public ObservableCollection<ChatMessage> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    public ICommand SendMessageCommand { get; }
    public ICommand VoltarCommand { get; }

    public NovoChamadoViewModel()
    {
        _authService = new AuthService();
        Title = "ChatBot";
        
        // Carregar email do usuário
        UserEmail = Preferences.Get("user_email", "usuário");
        
        SendMessageCommand = new Command(async () => await SendMessage(), () => !string.IsNullOrWhiteSpace(MessageText) && !IsBusy);
        VoltarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
        
        // Adicionar mensagem inicial do bot
        Messages.Add(new ChatMessage
        {
            Text = "👋 Olá! Sou o assistente virtual.\nComo posso ajudar você hoje?",
            IsUserMessage = false,
            Timestamp = DateTime.Now
        });
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText) || IsBusy)
            return;

        var userMessage = MessageText.Trim();
        MessageText = string.Empty;

        // Adicionar mensagem do usuário
        Messages.Add(new ChatMessage
        {
            Text = userMessage,
            IsUserMessage = true,
            Timestamp = DateTime.Now
        });

        // Adicionar ao histórico
        _conversationHistory.Add(new ChatbotHistoryItemDto
        {
            Sender = "user",
            Message = userMessage
        });

        IsBusy = true;

        try
        {
            var api = _authService.GetApiService();
            
            var response = await api.SendChatbotMessageAsync(userMessage, _conversationHistory);

            if (response != null)
            {
                // Adicionar resposta do bot
                Messages.Add(new ChatMessage
                {
                    Text = response.Message,
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });

                // Adicionar resposta ao histórico
                _conversationHistory.Add(new ChatbotHistoryItemDto
                {
                    Sender = "bot",
                    Message = response.Message
                });

                // Se o bot sugerir criar chamado e for relacionado a TI
                if (response.IsITRelated && response.SuggestTicketCreation)
                {
                    var criarChamado = await CustomAlertService.ShowQuestionAsync(
                        "Deseja criar um chamado técnico com base nesta conversa?",
                        "Criar Chamado?",
                        "Sim",
                        "Não");

                    if (criarChamado)
                    {
                        await CriarChamadoAutomatico(userMessage);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Text = $"❌ Erro ao enviar mensagem: {ex.Message}",
                IsUserMessage = false,
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsBusy = false;
            ((Command)SendMessageCommand).ChangeCanExecute();
        }
    }

    private async Task CriarChamadoAutomatico(string descricaoProblema)
    {
        try
        {
            IsBusy = true;

            var api = _authService.GetApiService();
            var novoChamado = new CriarChamadoDto
            {
                Titulo = descricaoProblema.Length > 100 
                    ? descricaoProblema.Substring(0, 97) + "..." 
                    : descricaoProblema,
                Descricao = $"Chamado criado via ChatBot:\n\n{descricaoProblema}\n\n--- Histórico da Conversa ---\n" +
                           string.Join("\n", _conversationHistory.Select(h => 
                               $"[{h.Sender.ToUpper()}]: {h.Message}")),
                Prioridade = "Media"
            };

            var result = await api.CriarChamadoAsync(novoChamado);

            if (result != null)
            {
                Messages.Add(new ChatMessage
                {
                    Text = $"✅ Chamado #{result.Id} criado com sucesso! Um técnico irá atendê-lo em breve.",
                    IsUserMessage = false,
                    Timestamp = DateTime.Now
                });

                await Task.Delay(2000);
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatMessage
            {
                Text = $"❌ Erro ao criar chamado: {ex.Message}",
                IsUserMessage = false,
                Timestamp = DateTime.Now
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
