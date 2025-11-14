using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GestaoChamados.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace GestaoChamados.Desktop;

/// <summary>
/// Janela de chat em tempo real entre cliente e técnico
/// Usa SignalR para comunicação bidirecional instantânea
/// Permite que técnicos finalizem chamados e clientes avaliem o atendimento
/// </summary>
public partial class ChatAoVivoWindow : Window
{
    private int _chamadoId;
    private ChamadoDto? _chamado;
    private HubConnection? _hubConnection;
    private bool _chatInicializado = false;
    private bool _souTecnico = false; // Flag para identificar se sou técnico

    public ChatAoVivoWindow(int chamadoId)
    {
        InitializeComponent();
        _chamadoId = chamadoId;
        Loaded += async (s, e) => await InicializarChatSafe();
    }

    private async Task InicializarChatSafe()
    {
        try
        {
            await InicializarChat();
        }
        catch (Exception ex)
        {
            CustomMessageBox.ShowError(
                $"Erro ao inicializar chat:\n\n{ex.Message}\n\n{ex.StackTrace}",
                "Erro ao Abrir Chat");
            this.Close();
        }
    }

    private async Task InicializarChat()
    {
        // Carregar dados do chamado
        _chamado = await App.ApiService.GetChamadoAsync(_chamadoId);
        
        if (_chamado == null)
        {
            throw new Exception($"Chamado #{_chamadoId} não encontrado");
        }

        // Identificar se sou o técnico ou o cliente
        _souTecnico = _chamado.TecnicoNome == App.CurrentUserName || 
                      _chamado.TecnicoNome == App.CurrentUserEmail ||
                      App.CurrentUserRole == "Tecnico" || 
                      App.CurrentUserRole == "Gerente" || 
                      App.CurrentUserRole == "Admin";
        
        // Debug log
        System.Diagnostics.Debug.WriteLine($"[ChatAoVivo] Usuário: {App.CurrentUserName} ({App.CurrentUserEmail})");
        System.Diagnostics.Debug.WriteLine($"[ChatAoVivo] Role: {App.CurrentUserRole}");
        System.Diagnostics.Debug.WriteLine($"[ChatAoVivo] Técnico do chamado: {_chamado.TecnicoNome}");
        System.Diagnostics.Debug.WriteLine($"[ChatAoVivo] _souTecnico = {_souTecnico}");
        
        TitleTextBlock.Text = $"Chat ao Vivo - Chamado #{_chamado.Id:D7}";
        
        // Mostrar o outro participante
        if (_souTecnico)
        {
            ClienteTextBlock.Text = $"Cliente: {_chamado.UsuarioNome ?? _chamado.UsuarioEmail}";
        }
        else
        {
            ClienteTextBlock.Text = $"Técnico: {_chamado.TecnicoNome ?? "Não atribuído"}";
        }
        
        AssuntoTextBlock.Text = $"Assunto: {_chamado.Titulo}";
        
        if (!string.IsNullOrEmpty(_chamado.Descricao))
        {
            DescricaoTextBlock.Text = $"Descrição: {_chamado.Descricao.Split('\n')[0]}";
        }

        // ==================== CARREGAR HISTÓRICO DE MENSAGENS ====================
        try
        {
            var mensagens = await App.ApiService.GetMensagensChamadoAsync(_chamadoId);
            if (mensagens != null && mensagens.Count > 0)
            {
                AdicionarMensagemSistema($"📜 Carregando histórico ({mensagens.Count} mensagens)...");
                
                foreach (var msg in mensagens.OrderBy(m => m.DataEnvio))
                {
                    // Verificar se a mensagem é minha
                    var ehMinhaMsg = msg.RemetenteNome == App.CurrentUserName || 
                                    msg.RemetenteNome == App.CurrentUserEmail;
                    
                    if (ehMinhaMsg)
                    {
                        // Minha mensagem (sempre à direita, azul)
                        AdicionarMensagemTecnico(msg.Mensagem, msg.DataEnvio);
                    }
                    else
                    {
                        // Mensagem do outro participante (sempre à esquerda, cinza)
                        AdicionarMensagemCliente(msg.RemetenteNome, msg.Mensagem, msg.DataEnvio);
                    }
                }
                
                AdicionarMensagemSistema("✅ Histórico carregado. Chat ao vivo ativo!");
            }
            else
            {
                AdicionarMensagemSistema("💬 Atendimento iniciado. Você pode conversar agora.");
            }
        }
        catch (Exception ex)
        {
            AdicionarMensagemSistema($"⚠️ Não foi possível carregar histórico: {ex.Message}");
        }
        
        // ==================== INICIALIZAR SIGNALR ====================
        var apiBaseUrl = App.ApiService.GetBaseUrl().Replace("/api", "");
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/supportHub", options =>
            {
                options.AccessTokenProvider = () => System.Threading.Tasks.Task.FromResult(App.CurrentUserToken);
            })
            .WithAutomaticReconnect()
            .Build();

        // Configurar eventos SignalR
        _hubConnection.On<string, string, string>("ReceiveMessage", (senderName, senderEmail, message) =>
        {
            Dispatcher.Invoke(() =>
            {
                // Verificar se a mensagem é minha
                var ehMinhaMensagem = senderName == App.CurrentUserName || 
                                      senderName == App.CurrentUserEmail ||
                                      senderEmail == App.CurrentUserEmail;
                
                if (!ehMinhaMensagem)
                {
                    // Mensagem recebida do outro participante
                    AdicionarMensagemCliente(senderName, message, DateTime.Now);
                }
                else
                {
                    // Minha própria mensagem (eco do servidor)
                    // Não fazer nada, já adicionei localmente
                }
            });
        });

        _hubConnection.On<string>("TicketStatusChanged", (newStatus) =>
        {
            Dispatcher.Invoke(async () =>
            {
                if (_chamado != null)
                {
                        _chamado.Status = newStatus;
                        
                        // Se foi resolvido, redirecionar AMBOS (técnico E cliente)
                        if (newStatus == "Resolvido")
                        {
                            MessageTextBox.IsEnabled = false;
                            SendButton.IsEnabled = false;
                            MarcarResolvidoButton.IsEnabled = false;
                            
                            // Verificar se sou técnico pela ROLE
                            var isTecnico = App.CurrentUserRole == "Tecnico" || 
                                          App.CurrentUserRole == "Gerente" || 
                                          App.CurrentUserRole == "Admin";
                            
                            if (isTecnico)
                            {
                                // SOU TÉCNICO - voltar para fila automaticamente
                                AdicionarMensagemSistema("✅ Atendimento finalizado! Voltando para a fila...");
                                await System.Threading.Tasks.Task.Delay(1500);
                                
                                // Fechar conexão SignalR
                                if (_hubConnection != null)
                                {
                                    try
                                    {
                                        await _hubConnection.StopAsync();
                                        await _hubConnection.DisposeAsync();
                                    }
                                    catch { }
                                }
                                
                                var filaWindow = new FilaAtendimentoWindow();
                                filaWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                // SOU CLIENTE - abrir pesquisa
                                AdicionarMensagemSistema("✅ Atendimento finalizado! Abrindo pesquisa de satisfação...");
                                await System.Threading.Tasks.Task.Delay(1500);
                                
                                // Fechar conexão SignalR
                                if (_hubConnection != null)
                                {
                                    try
                                    {
                                        await _hubConnection.StopAsync();
                                        await _hubConnection.DisposeAsync();
                                    }
                                    catch { }
                                }
                                
                            // Abrir pesquisa de satisfação
                            var pesquisaWindow = new PesquisaSatisfacaoWindow(_chamadoId);
                            var resultado = pesquisaWindow.ShowDialog();

                            if (resultado == true && pesquisaWindow.Finalizado)
                            {
                                CustomMessageBox.ShowSuccess(
                                    $"Obrigado pela sua avaliação!\n\n" +
                                    $"Protocolo: #{_chamadoId:D7}\n" +
                                    $"Avaliação: {pesquisaWindow.AvaliacaoSelecionada}/5 estrelas",
                                    "Atendimento Concluído");
                            }
                            
                            // Redirecionar para "Meus Chamados"
                            var chamadosWindow = new ChamadosWindow();
                            chamadosWindow.Show();
                            this.Close();
                            }
                        }
                    }
                });
            });

        // Conectar ao SignalR
        await _hubConnection.StartAsync();
        await _hubConnection.InvokeAsync("JoinTicketGroup", _chamadoId.ToString());
        
        _chatInicializado = true;
        AdicionarMensagemSistema($"✅ Conectado ao chat ao vivo!");
        
        MessageTextBox.Focus();
    }

    private void AdicionarMensagemTecnico(string mensagem, DateTime? timestamp = null)
    {
        var border = new Border
        {
            Style = (Style)FindResource("TecnicoMessageStyle")
        };

        var stackPanel = new StackPanel();

        var textBlock = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontSize = 14
        };

        var timeBlock = new TextBlock
        {
            Text = (timestamp ?? DateTime.Now).ToString("HH:mm"),
            FontSize = 11,
            Foreground = Brushes.White,
            Opacity = 0.8,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 5, 0, 0)
        };

        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(timeBlock);
        border.Child = stackPanel;

        ChatMessagesPanel.Children.Add(border);
        ScrollToBottom();
    }

    private void AdicionarMensagemCliente(string remetente, string mensagem, DateTime? timestamp = null)
    {
        var border = new Border
        {
            Style = (Style)FindResource("ClienteMessageStyle")
        };

        var stackPanel = new StackPanel();

        var nomeBlock = new TextBlock
        {
            Text = remetente,
            FontWeight = FontWeights.Bold,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var textBlock = new TextBlock
        {
            Text = mensagem,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(33, 37, 41)),
            FontSize = 14
        };

        var timeBlock = new TextBlock
        {
            Text = (timestamp ?? DateTime.Now).ToString("HH:mm"),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 5, 0, 0)
        };

        stackPanel.Children.Add(nomeBlock);
        stackPanel.Children.Add(textBlock);
        stackPanel.Children.Add(timeBlock);
        border.Child = stackPanel;

        ChatMessagesPanel.Children.Add(border);
        ScrollToBottom();
    }

    private void AdicionarMensagemSistema(string mensagem)
    {
        var textBlock = new TextBlock
        {
            Text = mensagem,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 10),
            FontStyle = FontStyles.Italic
        };

        ChatMessagesPanel.Children.Add(textBlock);
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer.ScrollToEnd();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        EnviarMensagem();
    }

    private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            EnviarMensagem();
        }
    }

    private async void EnviarMensagem()
    {
        var mensagem = MessageTextBox.Text.Trim();
        if (string.IsNullOrEmpty(mensagem) || !_chatInicializado)
            return;

        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            MessageBox.Show("Não conectado ao chat. Tente novamente.", "Erro", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Adicionar mensagem localmente (para feedback imediato)
            AdicionarMensagemTecnico(mensagem);
            MessageTextBox.Clear();

            // Enviar via SignalR
            await _hubConnection.InvokeAsync("SendMessage", 
                _chamadoId.ToString(), 
                App.CurrentUserName ?? App.CurrentUserEmail ?? "Técnico", 
                App.CurrentUserEmail ?? "", 
                mensagem);
            
            MessageTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao enviar mensagem: {ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MarcarResolvidoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CustomMessageBox.ShowQuestion(
            "Deseja marcar este chamado como resolvido?\n\n" +
            "O chat será encerrado para ambos os participantes.",
            "Finalizar Atendimento"))
            return;

        try
        {
            MarcarResolvidoButton.IsEnabled = false;
            MarcarResolvidoButton.Content = "⏳ Finalizando...";

            // Chamar a API para finalizar
            var sucesso = await App.ApiService.FinalizarChamadoAsync(_chamadoId);

            if (sucesso)
            {
                // A API enviará notificação SignalR para AMBOS
                // O evento TicketStatusChanged redirecionará cada um automaticamente
                AdicionarMensagemSistema("⏳ Finalizando atendimento...");
            }
            else
            {
                CustomMessageBox.ShowError("Erro ao finalizar chamado.", "Erro");

                MarcarResolvidoButton.IsEnabled = true;
                MarcarResolvidoButton.Content = "✓ Marcar como Resolvido";
            }
        }
        catch (Exception ex)
        {
            CustomMessageBox.ShowError($"Erro: {ex.Message}", "Erro");

            MarcarResolvidoButton.IsEnabled = true;
            MarcarResolvidoButton.Content = "✓ Marcar como Resolvido";
        }
    }

    private void FecharButton_Click(object sender, RoutedEventArgs e)
    {
        var resultado = MessageBox.Show(
            "Deseja realmente fechar o chat?\n\n" +
            "O atendimento ainda não foi finalizado.",
            "Confirmar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (resultado == MessageBoxResult.Yes)
        {
            this.Close();
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        // Desconectar SignalR
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch { }
        }

        base.OnClosed(e);
    }
}
