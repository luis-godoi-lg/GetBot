using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Desktop;

public partial class NovoChamadoWindow : Window
{
    private int? _chamadoId;
    private bool _aguardandoResposta = false;
    private List<ChatbotHistoryItemDto> _conversationHistory = new();

    public NovoChamadoWindow()
    {
        InitializeComponent();
        Loaded += NovoChamadoWindow_Loaded;
    }

    private void NovoChamadoWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Atualizar informações do usuário
        UserInfoTextBlock.Text = App.CurrentUserEmail ?? App.CurrentUserName ?? "Usuário";
        
        // Adicionar mensagem inicial do bot
        AdicionarMensagemBot("👋 Olá! Sou o assistente de TI. Qual seu problema?");
        
        // Focar no input
        MessageTextBox.Focus();
    }

    private void AdicionarMensagemBot(string mensagem)
    {
        var border = new Border
        {
            Style = (Style)FindResource("BotMessageStyle")
        };

        var textBlock = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
            FontSize = 14
        };

        border.Child = textBlock;
        ChatMessagesPanel.Children.Add(border);
        
        ScrollToBottom();
    }

    private void AdicionarMensagemUsuario(string mensagem)
    {
        var border = new Border
        {
            Style = (Style)FindResource("UserMessageStyle")
        };

        var textBlock = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontSize = 14
        };

        border.Child = textBlock;
        ChatMessagesPanel.Children.Add(border);
        
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToBottom();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await EnviarMensagem();
    }

    private async void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !_aguardandoResposta)
        {
            await EnviarMensagem();
        }
    }

    private async Task EnviarMensagem()
    {
        var mensagem = MessageTextBox.Text.Trim();
        
        if (string.IsNullOrEmpty(mensagem) || _aguardandoResposta)
            return;

        // Adicionar mensagem do usuário
        AdicionarMensagemUsuario(mensagem);
        MessageTextBox.Clear();
        
        _aguardandoResposta = true;
        SendButton.IsEnabled = false;
        MessageTextBox.IsEnabled = false;

        try
        {
            // Adicionar ao histórico
            _conversationHistory.Add(new ChatbotHistoryItemDto
            {
                Sender = "user",
                Message = mensagem
            });

            // Enviar para chatbot com IA
            var response = await App.ApiService.SendChatbotMessageAsync(mensagem, _conversationHistory);
            
            if (response != null && !string.IsNullOrEmpty(response.Message))
            {
                // Adicionar resposta ao histórico
                _conversationHistory.Add(new ChatbotHistoryItemDto
                {
                    Sender = "bot",
                    Message = response.Message
                });

                // Mostrar resposta
                AdicionarMensagemBot(response.Message);

                // ✅ CORREÇÃO: Criar chamado SEMPRE na primeira mensagem do usuário
                if (_chamadoId == null && _conversationHistory.Count(m => m.Sender == "user") == 1)
                {
                    await CriarChamadoAutomaticamente(mensagem);
                }

                // Mostrar botões de ação
                FalarComHumanoButton.Visibility = Visibility.Visible;
                ProblemaResolvidoButton.Visibility = Visibility.Visible;
            }
            else
            {
                AdicionarMensagemBot("❌ Erro ao processar sua mensagem. Por favor, tente novamente.");
            }
        }
        catch (Exception ex)
        {
            AdicionarMensagemBot($"❌ Erro: {ex.Message}\n\nPor favor, tente novamente ou solicite atendimento humano.");
        }
        finally
        {
            _aguardandoResposta = false;
            SendButton.IsEnabled = true;
            MessageTextBox.IsEnabled = true;
            MessageTextBox.Focus();
        }
    }

    private async Task CriarChamadoAutomaticamente(string primeiraMessage)
    {
        try
        {
            var novoChamado = new CriarChamadoDto
            {
                Titulo = primeiraMessage.Length > 100 ? primeiraMessage.Substring(0, 100) : primeiraMessage,
                Descricao = primeiraMessage,
                Prioridade = "Media"
            };

            var chamadoCriado = await App.ApiService.CriarChamadoAsync(novoChamado);
            
            if (chamadoCriado != null)
            {
                _chamadoId = chamadoCriado.Id;
                AdicionarMensagemBot($"📋 Chamado #{_chamadoId:D7} criado automaticamente para registro.");
            }
        }
        catch (Exception ex)
        {
            // Falha silenciosa - o chatbot continua funcionando sem chamado
            System.Diagnostics.Debug.WriteLine($"Erro ao criar chamado: {ex.Message}");
        }
    }

    private async Task CriarChamadoComHistorico(string titulo)
    {
        try
        {
            // Montar descrição com histórico da conversa
            var descricaoBuilder = new System.Text.StringBuilder();
            descricaoBuilder.AppendLine("=== PROBLEMA RESOLVIDO PELO CHATBOT ===");
            descricaoBuilder.AppendLine();
            descricaoBuilder.AppendLine("Histórico da Conversa:");
            descricaoBuilder.AppendLine("------------------------");
            
            foreach (var msg in _conversationHistory)
            {
                var sender = msg.Sender == "user" ? "Usuário" : "Chatbot";
                descricaoBuilder.AppendLine($"[{sender}]: {msg.Message}");
            }
            
            descricaoBuilder.AppendLine("------------------------");
            descricaoBuilder.AppendLine($"Status: Resolvido automaticamente em {DateTime.Now:dd/MM/yyyy HH:mm}");
            descricaoBuilder.AppendLine("O problema foi resolvido com sucesso através das orientações do chatbot.");
            
            var novoChamado = new CriarChamadoDto
            {
                Titulo = titulo.Length > 100 ? titulo.Substring(0, 100) : titulo,
                Descricao = descricaoBuilder.ToString(),
                Prioridade = "Media"
            };
            
            var chamadoCriado = await App.ApiService.CriarChamadoAsync(novoChamado);
            
            if (chamadoCriado != null)
            {
                _chamadoId = chamadoCriado.Id;
                System.Diagnostics.Debug.WriteLine($"✅ Chamado #{_chamadoId:D7} criado com histórico do chatbot");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Erro ao criar chamado com histórico: {ex.Message}");
        }
    }

    private async void FalarComHumanoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CustomMessageBox.ShowQuestion(
            "Deseja realmente solicitar atendimento humano?\n\nUm técnico será notificado e entrará em contato.",
            "Confirmar Atendimento Humano"))
            return;

        try
        {
            // Criar chamado se ainda não existir
            if (_chamadoId == null)
            {
                var primeiraMsg = _conversationHistory.FirstOrDefault(m => m.Sender == "user")?.Message ?? "Solicitação de atendimento";
                await CriarChamadoAutomaticamente(primeiraMsg);
            }

            if (_chamadoId != null)
            {
                // ✅ Solicitar atendimento humano (muda status para "Aguardando Atendente" e notifica técnicos)
                var sucesso = await App.ApiService.SolicitarAtendimentoAsync(_chamadoId.Value);
                
                if (sucesso)
                {
                    // Mostrar mensagem de sucesso
                    AdicionarMensagemBot("✅ Solicitação enviada com sucesso!");
                    AdicionarMensagemBot("⏳ Redirecionando para tela de espera...");
                    
                    await Task.Delay(1500);
                    
                    // Buscar dados atualizados do chamado e abrir tela de espera
                    var chamado = await App.ApiService.GetChamadoAsync(_chamadoId.Value);
                    if (chamado != null)
                    {
                        var aguardandoWindow = new AguardandoAtendimentoWindow(chamado);
                        aguardandoWindow.Show();
                        this.Close();
                    }
                return;
            }
        }
        
        CustomMessageBox.ShowError(
            "Erro ao solicitar atendimento. Tente novamente.",
            "Erro");
            
        this.Close();
    }
    catch (Exception ex)
    {
        CustomMessageBox.ShowError($"Erro ao solicitar atendimento: {ex.Message}", "Erro");
    }
}

    private async void ProblemaResolvidoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // ✅ CORREÇÃO: Criar chamado com histórico se não existir
            if (_chamadoId == null)
            {
                var primeiraMsg = _conversationHistory.FirstOrDefault(m => m.Sender == "user")?.Message 
                    ?? "Problema resolvido pelo chatbot";
                
                AdicionarMensagemBot("📋 Criando registro do atendimento...");
                await CriarChamadoComHistorico(primeiraMsg);
                
                if (_chamadoId == null)
                {
                    CustomMessageBox.ShowError(
                        "Erro ao criar registro do chamado.\n\nPor favor, tente novamente.",
                        "Erro");
                    return;
                }
            }
            
            // Abrir janela de pesquisa de satisfação
            var pesquisaWindow = new PesquisaSatisfacaoWindow(_chamadoId.Value);
            var resultado = pesquisaWindow.ShowDialog();

            if (resultado == true && pesquisaWindow.Finalizado)
            {
                // Voltar para tela de chamados (mensagem de sucesso já exibida em PesquisaSatisfacaoWindow)
                var chamadosWindow = new ChamadosWindow();
                chamadosWindow.Show();
                this.Close();
            }
            // Se resultado == false, o usuário clicou em "Continuar Conversando" e a janela apenas fecha
        }
        catch (Exception ex)
        {
            CustomMessageBox.ShowError($"Erro: {ex.Message}", "Erro");
        }
    }

    private void VoltarButton_Click(object sender, RoutedEventArgs e)
    {
        if (_conversationHistory.Any())
        {
            var result = MessageBox.Show(
                "Você tem uma conversa em andamento.\n\nDeseja realmente sair?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;
        }

        var chamadosWindow = new ChamadosWindow();
        chamadosWindow.Show();
        this.Close();
    }
}
