using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Shared.DTOs;
using GestaoChamados.Shared.Services;
using Microsoft.Maui.Controls.Shapes;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views;

public partial class NovoChamadoPage : ContentPage
{
    private readonly ApiService _apiService;
    private int? _chamadoId;
    private bool _aguardandoResposta = false;
    private List<ChatbotHistoryItemDto> _conversationHistory = new();

    public NovoChamadoPage()
    {
        InitializeComponent();
        _apiService = new ApiService(Settings.ApiBaseUrl) { Token = Settings.Token };
        UserInfoLabel.Text = Settings.UserEmail ?? "Usuário";
        AdicionarMensagemBot(" Olá! Sou o assistente de TI. Qual seu problema?");
        MessageEntry.Focus();
    }

    private void AdicionarMensagemBot(string mensagem)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#E9ECEF"),
            Padding = new Thickness(15, 10),
            Margin = new Thickness(0, 5, 80, 5),
            HorizontalOptions = LayoutOptions.Start,
            MaximumWidthRequest = 280,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18, 18, 18, 4) },
            Content = new Label { Text = mensagem, TextColor = Color.FromArgb("#212529"), FontSize = 14, LineBreakMode = LineBreakMode.WordWrap }
        };
        ChatMessagesLayout.Children.Add(border);
        MainThread.BeginInvokeOnMainThread(async () => { await Task.Delay(100); await ChatScrollView.ScrollToAsync(0, ChatMessagesLayout.Height, false); });
    }

    private void AdicionarMensagemUsuario(string mensagem)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#007BFF"),
            Padding = new Thickness(15, 10),
            Margin = new Thickness(80, 5, 0, 5),
            HorizontalOptions = LayoutOptions.End,
            MaximumWidthRequest = 280,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(18, 18, 4, 18) },
            Content = new Label { Text = mensagem, TextColor = Colors.White, FontSize = 14, LineBreakMode = LineBreakMode.WordWrap }
        };
        ChatMessagesLayout.Children.Add(border);
        MainThread.BeginInvokeOnMainThread(async () => { await Task.Delay(100); await ChatScrollView.ScrollToAsync(0, ChatMessagesLayout.Height, false); });
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        var mensagem = MessageEntry.Text?.Trim();
        if (string.IsNullOrEmpty(mensagem) || _aguardandoResposta) return;
        AdicionarMensagemUsuario(mensagem);
        MessageEntry.Text = string.Empty;
        _aguardandoResposta = true;
        MessageEntry.IsEnabled = false;
        try
        {
            _conversationHistory.Add(new ChatbotHistoryItemDto { Sender = "user", Message = mensagem });
            var response = await _apiService.SendChatbotMessageAsync(mensagem, _conversationHistory);
            if (response != null && !string.IsNullOrEmpty(response.Message))
            {
                _conversationHistory.Add(new ChatbotHistoryItemDto { Sender = "bot", Message = response.Message });
                AdicionarMensagemBot(response.Message);
                if (_chamadoId == null && _conversationHistory.Count(m => m.Sender == "user") == 1)
                    await CriarChamadoAutomaticamente(mensagem);
                ActionButtonsContainer.IsVisible = true;
            }
            else AdicionarMensagemBot(" Erro ao processar sua mensagem.");
        }
        catch (Exception ex) { AdicionarMensagemBot($" Erro: {ex.Message}"); }
        finally { _aguardandoResposta = false; MessageEntry.IsEnabled = true; MessageEntry.Focus(); }
    }

    private async Task CriarChamadoAutomaticamente(string primeiraMessage)
    {
        try
        {
            Console.WriteLine("[NovoChamado] Criando chamado automaticamente...");
            var novoChamado = new CriarChamadoDto
            {
                Titulo = primeiraMessage.Length > 100 ? primeiraMessage.Substring(0, 100) : primeiraMessage,
                Descricao = primeiraMessage,
                Prioridade = "Media"
            };
            var chamadoCriado = await _apiService.CriarChamadoAsync(novoChamado);
            if (chamadoCriado != null) 
            { 
                _chamadoId = chamadoCriado.Id; 
                Console.WriteLine($"[NovoChamado] Chamado #{_chamadoId} criado com sucesso. Status inicial: {chamadoCriado.Status}");
                AdicionarMensagemBot($" Chamado #{_chamadoId:D7} criado automaticamente."); 
            }
            else
            {
                Console.WriteLine("[NovoChamado] ERRO: CriarChamadoAsync retornou null!");
            }
        }
        catch { }
    }

    private async void OnFalarComHumanoClicked(object sender, EventArgs e)
    {
        bool answer = await CustomAlertService.ShowQuestionAsync("Deseja solicitar atendimento humano?");
        if (answer)
        {
            try
            {
                Console.WriteLine($"[NovoChamado] Solicitando atendimento humano. ChamadoId atual: {_chamadoId}");
                
                if (_chamadoId == null)
                {
                    Console.WriteLine("[NovoChamado] Chamado não existe, criando automaticamente...");
                    var msg = _conversationHistory.FirstOrDefault(m => m.Sender == "user")?.Message ?? "Solicitação";
                    await CriarChamadoAutomaticamente(msg);
                }
                
                if (_chamadoId != null)
                {
                    Console.WriteLine($"[NovoChamado] Chamando SolicitarAtendimentoAsync para chamado #{_chamadoId}...");
                    bool sucesso = await _apiService.SolicitarAtendimentoAsync(_chamadoId.Value);
                    Console.WriteLine($"[NovoChamado] SolicitarAtendimentoAsync retornou: {sucesso}");
                    
                    if (sucesso)
                    {
                        AdicionarMensagemBot("✅ Solicitação enviada! Redirecionando para fila de atendimento...");
                        await Task.Delay(1500);
                        Console.WriteLine($"[NovoChamado] Navegando para FilaAtendimentoPage com chamadoId={_chamadoId}");
                        await Navigation.PushAsync(new FilaAtendimentoPage(_chamadoId.Value));
                    }
                    else
                    {
                        Console.WriteLine("[NovoChamado] ERRO: SolicitarAtendimentoAsync falhou!");
                        AdicionarMensagemBot("❌ Erro ao solicitar atendimento. Tente novamente.");
                    }
                }
                else
                {
                    Console.WriteLine("[NovoChamado] ERRO: _chamadoId ainda é null após tentativa de criação!");
                    AdicionarMensagemBot("❌ Erro ao criar chamado. Tente novamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NovoChamado] EXCEÇÃO em OnFalarComHumanoClicked: {ex.Message}");
                await CustomAlertService.ShowErrorAsync($"Erro ao solicitar atendimento: {ex.Message}");
            }
        }
    }

    private async void OnProblemaResolvidoClicked(object sender, EventArgs e)
    {
        try
        {
            // Se não há chamado criado, criar agora com histórico do chatbot
            if (_chamadoId == null)
            {
                var msg = _conversationHistory.FirstOrDefault(m => m.Sender == "user")?.Message ?? "Resolvido";
                var desc = new System.Text.StringBuilder();
                desc.AppendLine("=== RESOLVIDO PELO CHATBOT ===");
                foreach (var m in _conversationHistory) 
                    desc.AppendLine($"[{(m.Sender == "user" ? "Usuário" : "Bot")}]: {m.Message}");
                
                var chamado = await _apiService.CriarChamadoAsync(new CriarChamadoDto 
                { 
                    Titulo = msg.Length > 100 ? msg.Substring(0, 100) : msg, 
                    Descricao = desc.ToString(), 
                    Prioridade = "Media" 
                });
                
                if (chamado != null) 
                {
                    _chamadoId = chamado.Id;
                    Console.WriteLine($"[NovoChamado] Chamado #{_chamadoId} criado para finalização via chatbot");
                }
                else
                {
                    await CustomAlertService.ShowErrorAsync("Não foi possível criar o chamado. Tente novamente.");
                    return;
                }
            }

            // Marcar chamado como "Resolvido"
            Console.WriteLine($"[NovoChamado] Finalizando chamado #{_chamadoId} (status → Resolvido)");
            bool sucesso = await _apiService.FinalizarChamadoAsync(_chamadoId.Value);
            
            if (sucesso)
            {
                AdicionarMensagemBot("✅ Chamado finalizado! Redirecionando para pesquisa de satisfação...");
                await Task.Delay(1000);
                
                // Buscar dados atualizados do chamado
                var chamadoDto = await _apiService.GetChamadoByIdAsync(_chamadoId.Value);
                
                if (chamadoDto != null)
                {
                    // Converter ChamadoDto para Chamado
                    var chamado = new Chamado
                    {
                        Id = chamadoDto.Id,
                        NomeCliente = chamadoDto.UsuarioEmail,
                        Assunto = chamadoDto.Titulo,
                        Descricao = chamadoDto.Descricao,
                        Status = chamadoDto.Status,
                        DataAbertura = chamadoDto.DataCriacao
                    };
                    
                    Console.WriteLine($"[NovoChamado] Navegando para PesquisaSatisfacaoView - Chamado #{chamado.Id}");
                    await Navigation.PushAsync(new PesquisaSatisfacaoView(chamado));
                }
                else
                {
                    await CustomAlertService.ShowSuccessAsync($"Chamado #{_chamadoId:D7} finalizado!");
                    await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                Console.WriteLine($"[NovoChamado] ERRO: FinalizarChamadoAsync retornou false para chamado #{_chamadoId}");
                await CustomAlertService.ShowErrorAsync("Não foi possível finalizar o chamado. Tente novamente.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NovoChamado] EXCEÇÃO em OnProblemaResolvidoClicked: {ex.Message}");
            await CustomAlertService.ShowErrorAsync($"Erro ao finalizar: {ex.Message}");
        }
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        if (_conversationHistory.Any())
        {
            bool answer = await CustomAlertService.ShowQuestionAsync("Você tem uma conversa em andamento. Deseja sair?");
            if (!answer) return;
        }
        await Shell.Current.GoToAsync("..");
    }
}
