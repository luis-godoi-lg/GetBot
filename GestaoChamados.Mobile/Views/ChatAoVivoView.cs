using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using GestaoChamados.Shared.Services;
using GestaoChamados.Shared.DTOs;
using GestaoChamados.Mobile.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using GestaoChamados.Mobile.Helpers;

namespace GestaoChamados.Mobile.Views
{
    public class ChatAoVivoView : ContentPage
    {
        private readonly ApiService _apiService;
        private int _chamadoId;
        private ChamadoDto? _chamado;
        private HubConnection? _hubConnection;
        private VerticalStackLayout _messagesStack = new();
        private ScrollView _scrollView = new();
        private Entry _messageEntry = new();
        private Button _marcarResolvidoButton = new();
        private Button _sendButton = new();
        private bool _chatInicializado = false;
        private bool _souTecnico = false;
        private Label _subtitleLabel = new();
        private bool _finalizacaoEmProgresso = false;

        public ChatAoVivoView(int chamadoId)
        {
            _apiService = new ApiService(Settings.ApiBaseUrl) { Token = Settings.Token };
            _chamadoId = chamadoId;
            
            Shell.SetNavBarIsVisible(this, false);
            BackgroundColor = Color.FromArgb("#F5F6FA");
            
            CriarInterface();
        }

        private void CriarInterface()
        {
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = GridLength.Auto }
                }
            };

            // Header
            var headerBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#0066CC"),
                Padding = new Thickness(20, 15)
            };

            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            var backButton = new Button
            {
                Text = "← Voltar",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#0052A3"),
                FontSize = 13,
                Padding = new Thickness(15, 8),
                CornerRadius = 4
            };
            backButton.Clicked += OnVoltarClicked;

            var titleLayout = new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            var titleLabel = new Label
            {
                Text = $"💬 Chat ao Vivo - #{_chamadoId:D7}",
                TextColor = Colors.White,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold
            };

            _subtitleLabel.Text = "Carregando...";
            _subtitleLabel.TextColor = Color.FromArgb("#E3F2FD");
            _subtitleLabel.FontSize = 13;

            titleLayout.Add(titleLabel);
            titleLayout.Add(_subtitleLabel);

            headerGrid.Add(backButton, 0, 0);
            headerGrid.Add(titleLayout, 1, 0);
            headerBorder.Content = headerGrid;
            mainGrid.Add(headerBorder, 0, 0);

            // Messages area
            _messagesStack.Spacing = 10;
            _messagesStack.Padding = new Thickness(15, 10);

            _scrollView.Content = _messagesStack;
            _scrollView.BackgroundColor = Color.FromArgb("#F5F6FA");
            mainGrid.Add(_scrollView, 0, 1);

            // Input area
            var inputFrame = new Border
            {
                BackgroundColor = Colors.White,
                Padding = new Thickness(15, 10),
                StrokeThickness = 0
            };

            var inputGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = GridLength.Auto }
                },
                ColumnSpacing = 10,
                RowSpacing = 10
            };

            _messageEntry.Placeholder = "Digite sua mensagem...";
            _messageEntry.FontSize = 15;
            _messageEntry.BackgroundColor = Color.FromArgb("#F5F5F5");
            _messageEntry.TextColor = Colors.Black;
            _messageEntry.Completed += (s, e) => EnviarMensagem();

            _sendButton.Text = "Enviar";
            _sendButton.BackgroundColor = Color.FromArgb("#0066CC");
            _sendButton.TextColor = Colors.White;
            _sendButton.FontSize = 14;
            _sendButton.Padding = new Thickness(20, 10);
            _sendButton.CornerRadius = 8;
            _sendButton.Clicked += (s, e) => EnviarMensagem();

            _marcarResolvidoButton.Text = "✓ Marcar como Resolvido";
            _marcarResolvidoButton.BackgroundColor = Color.FromArgb("#4CAF50");
            _marcarResolvidoButton.TextColor = Colors.White;
            _marcarResolvidoButton.FontSize = 14;
            _marcarResolvidoButton.CornerRadius = 8;
            _marcarResolvidoButton.Clicked += OnMarcarResolvidoClicked;

            inputGrid.Add(_messageEntry, 0, 0);
            inputGrid.Add(_sendButton, 1, 0);
            inputGrid.Add(_marcarResolvidoButton, 0, 1);
            Grid.SetColumnSpan(_marcarResolvidoButton, 2);

            inputFrame.Content = inputGrid;
            mainGrid.Add(inputFrame, 0, 2);

            Content = mainGrid;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InicializarChat();
        }

        private async Task InicializarChat()
        {
            try
            {
                AdicionarMensagemSistema("🔄 Carregando dados do chamado...");

                _chamado = await _apiService.GetChamadoAsync(_chamadoId);

                if (_chamado != null)
                {
                    var currentUserRole = Settings.UserRole ?? "";
                    _souTecnico = currentUserRole == "Tecnico" || currentUserRole == "Gerente" || currentUserRole == "Admin";
                    
                    Console.WriteLine($"[ChatAoVivo] Role: {currentUserRole}, _souTecnico = {_souTecnico}");
                    
                    if (_souTecnico)
                    {
                        _subtitleLabel.Text = $"Cliente: {_chamado.UsuarioNome ?? _chamado.UsuarioEmail}";
                        _marcarResolvidoButton.IsVisible = true;
                    }
                    else
                    {
                        _subtitleLabel.Text = $"Técnico: {_chamado.TecnicoNome ?? "Não atribuído"}";
                        _marcarResolvidoButton.IsVisible = false;
                    }
                    
                    AdicionarMensagemSistema($"📋 {_chamado.Titulo}");
                }

                // Carregar histórico
                var mensagens = await _apiService.GetMensagensChamadoAsync(_chamadoId);
                if (mensagens != null && mensagens.Count > 0)
                {
                    AdicionarMensagemSistema($"📜 Carregando {mensagens.Count} mensagens...");
                    
                    var currentUserEmail = Settings.UserEmail ?? "";
                    
                    foreach (var msg in mensagens.OrderBy(m => m.DataEnvio))
                    {
                        var ehMinhaMsg = msg.RemetenteNome == currentUserEmail;
                        
                        if (ehMinhaMsg)
                            AdicionarMensagemTecnico(msg.Mensagem, msg.DataEnvio);
                        else
                            AdicionarMensagemCliente(msg.RemetenteNome, msg.Mensagem, msg.DataEnvio);
                    }
                    
                    AdicionarMensagemSistema("✅ Chat ativo!");
                }
                else
                {
                    AdicionarMensagemSistema("💬 Você pode conversar agora.");
                }
                
                await ConectarSignalR();
                
                _chatInicializado = true;
                _messageEntry.Focus();
            }
            catch (Exception ex)
            {
                await CustomAlertService.ShowErrorAsync($"Erro ao inicializar: {ex.Message}");
            }
        }

        private async Task ConectarSignalR()
        {
            try
            {
                var apiBaseUrl = Settings.ApiBaseUrl.Replace("/api", "");
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{apiBaseUrl}/supportHub", options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult<string?>(Settings.Token);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string, string, string>("ReceiveMessage", (senderName, senderEmail, message) =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        var currentUserEmail = Settings.UserEmail ?? "";
                        
                        if (senderEmail != currentUserEmail)
                        {
                            AdicionarMensagemCliente(senderName, message, DateTime.Now);
                        }
                    });
                });

                _hubConnection.On<string>("TicketStatusChanged", async (newStatus) =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Prevenir processamento duplicado
                        if (_finalizacaoEmProgresso)
                            return;
                        
                        if (_chamado != null && newStatus == "Resolvido")
                        {
                            _finalizacaoEmProgresso = true;
                            
                            _messageEntry.IsEnabled = false;
                            _sendButton.IsEnabled = false;
                            _marcarResolvidoButton.IsEnabled = false;
                            
                            if (_souTecnico)
                            {
                                AdicionarMensagemSistema("✅ Atendimento finalizado! Voltando...");
                                Console.WriteLine($"[ChatAoVivo] Técnico - Iniciando retorno à fila. Stack count: {Navigation?.NavigationStack?.Count}");
                                
                                await Task.Delay(1000);
                                await DesconectarSignalR();
                                
                                // Tentar voltar para FilaAtendimentoView
                                await VoltarParaFila();
                            }
                            else
                            {
                                AdicionarMensagemSistema("✅ Finalizado! Abrindo pesquisa...");
                                await Task.Delay(1500);
                                await DesconectarSignalR();
                                
                                try
                                {
                                    await Navigation.PushAsync(new PesquisaSatisfacaoView(new Chamado
                                    {
                                        Id = _chamadoId,
                                        NomeCliente = _chamado.UsuarioNome,
                                        Assunto = _chamado.Titulo,
                                        Descricao = _chamado.Descricao,
                                        Status = _chamado.Status
                                    }));
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[ChatAoVivo] Erro ao abrir pesquisa: {ex.Message}");
                                    await CustomAlertService.ShowErrorAsync("Não foi possível abrir a pesquisa de satisfação.");
                                }
                            }
                        }
                    });
                });

                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("JoinTicketGroup", _chamadoId.ToString());
                
                AdicionarMensagemSistema("✅ Conectado!");
            }
            catch (Exception ex)
            {
                AdicionarMensagemSistema($"⚠️ Erro SignalR: {ex.Message}");
            }
        }

        private void AdicionarMensagemTecnico(string mensagem, DateTime? timestamp = null)
        {
            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#0066CC"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                Padding = new Thickness(12),
                Margin = new Thickness(50, 5, 10, 5),
                HorizontalOptions = LayoutOptions.End,
                MaximumWidthRequest = 300
            };

            var stack = new VerticalStackLayout { Spacing = 5 };
            stack.Add(new Label { Text = mensagem, TextColor = Colors.White, FontSize = 14, LineBreakMode = LineBreakMode.WordWrap });
            stack.Add(new Label { Text = (timestamp ?? DateTime.Now).ToString("HH:mm"), FontSize = 11, TextColor = Color.FromArgb("#E3F2FD"), HorizontalOptions = LayoutOptions.End });
            border.Content = stack;

            _messagesStack.Add(border);
            ScrollToBottom();
        }

        private void AdicionarMensagemCliente(string remetente, string mensagem, DateTime? timestamp = null)
        {
            var border = new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E0E0E0"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                Padding = new Thickness(12),
                Margin = new Thickness(10, 5, 50, 5),
                HorizontalOptions = LayoutOptions.Start,
                MaximumWidthRequest = 300
            };

            var stack = new VerticalStackLayout { Spacing = 5 };
            stack.Add(new Label { Text = remetente, FontAttributes = FontAttributes.Bold, FontSize = 12, TextColor = Color.FromArgb("#424242") });
            stack.Add(new Label { Text = mensagem, TextColor = Color.FromArgb("#212121"), FontSize = 14, LineBreakMode = LineBreakMode.WordWrap });
            stack.Add(new Label { Text = (timestamp ?? DateTime.Now).ToString("HH:mm"), FontSize = 11, TextColor = Color.FromArgb("#9E9E9E") });
            border.Content = stack;

            _messagesStack.Add(border);
            ScrollToBottom();
        }

        private void AdicionarMensagemSistema(string mensagem)
        {
            _messagesStack.Add(new Label
            {
                Text = mensagem,
                FontSize = 12,
                TextColor = Color.FromArgb("#757575"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10),
                FontAttributes = FontAttributes.Italic
            });
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await _scrollView.ScrollToAsync(0, _messagesStack.Height, false);
            });
        }

        private async void EnviarMensagem()
        {
            var mensagem = _messageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(mensagem) || !_chatInicializado)
                return;

            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                await CustomAlertService.ShowErrorAsync("Não conectado.");
                return;
            }

            try
            {
                AdicionarMensagemTecnico(mensagem);
                _messageEntry.Text = string.Empty;

                var currentUserName = Settings.UserName ?? Settings.UserEmail ?? "Usuário";
                var currentUserEmail = Settings.UserEmail ?? "";
                
                await _hubConnection.InvokeAsync("SendMessage", 
                    _chamadoId.ToString(), 
                    currentUserName, 
                    currentUserEmail, 
                    mensagem);
                
                _messageEntry.Focus();
            }
            catch (Exception ex)
            {
                await CustomAlertService.ShowErrorAsync($"Erro ao enviar: {ex.Message}");
            }
        }

        private async void OnMarcarResolvidoClicked(object? sender, EventArgs e)
        {
            var confirma = await CustomAlertService.ShowQuestionAsync(
                "Marcar como resolvido?\n\nO chat será encerrado para ambos.",
                "Finalizar",
                "Sim",
                "Não");

            if (!confirma)
                return;

            try
            {
                _marcarResolvidoButton.IsEnabled = false;
                _marcarResolvidoButton.Text = "⏳ Finalizando...";

                var sucesso = await _apiService.FinalizarChamadoAsync(_chamadoId);

                if (sucesso)
                {
                    AdicionarMensagemSistema("⏳ Finalizando...");
                }
                else
                {
                    await CustomAlertService.ShowErrorAsync("Erro ao finalizar.");
                    _marcarResolvidoButton.IsEnabled = true;
                    _marcarResolvidoButton.Text = "✓ Marcar como Resolvido";
                }
            }
            catch (Exception ex)
            {
                await CustomAlertService.ShowErrorAsync(ex.Message);
                _marcarResolvidoButton.IsEnabled = true;
                _marcarResolvidoButton.Text = "✓ Marcar como Resolvido";
            }
        }

        private async void OnVoltarClicked(object? sender, EventArgs e)
        {
            if (_chamado?.Status != "Resolvido")
            {
                var confirma = await CustomAlertService.ShowQuestionAsync(
                    "Fechar chat?\n\nAtendimento não foi finalizado.",
                    "Confirmar",
                    "Sim",
                    "Não");

                if (!confirma)
                    return;
            }

            await DesconectarSignalR();
            await Navigation.PopAsync();
        }

        private async Task DesconectarSignalR()
        {
            if (_hubConnection != null)
            {
                try
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
                catch { }
            }
        }

        private async Task VoltarParaFila()
        {
            try
            {
                Console.WriteLine($"[ChatAoVivo] VoltarParaFila - Navigation stack count: {Navigation?.NavigationStack?.Count}");
                
                if (Navigation == null)
                {
                    Console.WriteLine("[ChatAoVivo] Navigation é null!");
                    return;
                }

                // Método 1: PopAsync simples
                try
                {
                    Console.WriteLine("[ChatAoVivo] Tentando PopAsync()...");
                    await Navigation.PopAsync(animated: true);
                    Console.WriteLine("[ChatAoVivo] PopAsync() bem-sucedido!");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatAoVivo] PopAsync falhou: {ex.Message}");
                }

                // Método 2: PopToRootAsync se PopAsync falhar
                try
                {
                    Console.WriteLine("[ChatAoVivo] Tentando PopToRootAsync()...");
                    await Navigation.PopToRootAsync(animated: true);
                    Console.WriteLine("[ChatAoVivo] PopToRootAsync() bem-sucedido!");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatAoVivo] PopToRootAsync falhou: {ex.Message}");
                }

                // Método 3: Limpar stack manualmente
                try
                {
                    Console.WriteLine("[ChatAoVivo] Limpando stack manualmente...");
                    var pagesToRemove = Navigation.NavigationStack.ToList();
                    
                    // Remover todas exceto a root
                    foreach (var page in pagesToRemove.Skip(1))
                    {
                        Navigation.RemovePage(page);
                    }
                    
                    Console.WriteLine("[ChatAoVivo] Stack limpo!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ChatAoVivo] Limpeza de stack falhou: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatAoVivo] VoltarParaFila exception: {ex.Message}");
                Console.WriteLine($"[ChatAoVivo] StackTrace: {ex.StackTrace}");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            await DesconectarSignalR();
        }
    }
}
