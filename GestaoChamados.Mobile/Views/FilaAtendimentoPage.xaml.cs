using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Shared.Services;
using GestaoChamados.Mobile.Services;
using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;

namespace GestaoChamados.Mobile.Views;

public partial class FilaAtendimentoPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly int _chamadoId;
    private System.Timers.Timer? _pollingTimer;
    private HubConnection? _hubConnection;
    private bool _jaAbriuChat = false;

    public FilaAtendimentoPage(int chamadoId)
    {
        InitializeComponent();
        _chamadoId = chamadoId;
        _apiService = new ApiService(Settings.ApiBaseUrl) { Token = Settings.Token };
        UserInfoLabel.Text = Settings.UserEmail ?? "Usuario";
        ProtocoloLabel.Text = $"Protocolo: #{chamadoId:D7}";
        
        // Conectar ao SignalR para receber notificações em tempo real
        Task.Run(async () => await ConectarSignalR());
        
        // Iniciar polling como backup (caso SignalR falhe)
        IniciarPolling();
    }

    private void IniciarPolling()
    {
        _pollingTimer = new System.Timers.Timer(3000); // Verifica a cada 3 segundos
        _pollingTimer.Elapsed += async (s, e) => await VerificarStatusChamado();
        _pollingTimer.AutoReset = true;
        _pollingTimer.Start();
        
        // Primeira verificacao imediata
        Task.Run(async () => await VerificarStatusChamado());
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

            // Evento: Quando técnico assume o chamado
            _hubConnection.On<int>("ChamadoAssumido", async (chamadoId) =>
            {
                if (chamadoId == _chamadoId && !_jaAbriuChat)
                {
                    _jaAbriuChat = true;
                    _pollingTimer?.Stop();
                    
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            // Buscar dados do chamado para pegar o nome do técnico
                            var chamado = await _apiService.GetChamadoByIdAsync(_chamadoId);
                            
                            await CustomAlertService.ShowSuccessAsync(
                                $"O técnico {chamado?.TecnicoNome ?? "atendente"} assumiu seu chamado!\n\nVocê será redirecionado para o chat.",
                                "Atendimento Iniciado"
                            );
                            
                            // Desconectar SignalR antes de navegar (será reconectado no ChatAoVivoView)
                            await DesconectarSignalR();
                            
                            // Redirecionar para o chat ao vivo
                            await Navigation.PushAsync(new ChatAoVivoView(_chamadoId));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] Erro ao abrir chat: {ex.Message}");
                            // Fallback: polling detectará a mudança
                            _jaAbriuChat = false;
                        }
                    });
                }
            });

            await _hubConnection.StartAsync();
            await _hubConnection.InvokeAsync("JoinTicketGroup", _chamadoId.ToString());
            
            Console.WriteLine($"[FilaAtendimento] Conectado ao SignalR, grupo: ticket_{_chamadoId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FilaAtendimento] Erro SignalR: {ex.Message}");
            // Polling continua funcionando como fallback
        }
    }

    private async Task VerificarStatusChamado()
    {
        try
        {
            // Se já foi redirecionado, não verificar mais
            if (_jaAbriuChat)
                return;
            
            var chamado = await _apiService.GetChamadoByIdAsync(_chamadoId);
            
            if (chamado != null)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // Se o chamado foi assumido (Em Atendimento), navegar para o chat
                    if (chamado.Status == "Em Atendimento" && chamado.TecnicoId != null && !_jaAbriuChat)
                    {
                        _jaAbriuChat = true;
                        _pollingTimer?.Stop();
                        
                        await CustomAlertService.ShowSuccessAsync(
                            $"O técnico {chamado.TecnicoNome ?? "atendente"} assumiu seu chamado!",
                            "Atendimento Iniciado"
                        );
                        
                        await DesconectarSignalR();
                        
                        // Navegar para o chat ao vivo
                        await Navigation.PushAsync(new ChatAoVivoView(_chamadoId));
                    }
                    // Se foi cancelado ou finalizado
                    else if (chamado.Status == "Cancelado" || chamado.Status == "Fechado")
                    {
                        _pollingTimer?.Stop();
                        await CustomAlertService.ShowInfoAsync("Este chamado foi encerrado.", "Chamado Finalizado");
                        await DesconectarSignalR();
                        await Navigation.PopAsync();
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Erro de conexao - nao precisa exibir, vai tentar novamente
            System.Diagnostics.Debug.WriteLine($"Erro ao verificar status: {ex.Message}");
        }
    }

    private async void OnAtualizarClicked(object sender, EventArgs e)
    {
        try
        {
            var button = sender as Button;
            if (button != null)
            {
                button.IsEnabled = false;
                button.Text = "Atualizando...";
            }

            await VerificarStatusChamado();
            
            // Verificar posicao na fila
            var todosChamados = await _apiService.GetChamadosAsync();
            var chamadosNaFila = todosChamados?
                .Where(c => (c.Status == "Aberto" || c.Status == "Aguardando Atendente") && c.Id <= _chamadoId)
                .OrderBy(c => c.DataCriacao)
                .ToList();

            if (chamadosNaFila != null && chamadosNaFila.Any())
            {
                var posicao = chamadosNaFila.FindIndex(c => c.Id == _chamadoId) + 1;
                PosicaoFilaLabel.Text = $"Posicao na fila: {posicao} de {chamadosNaFila.Count}";
                PosicaoFilaLabel.IsVisible = true;
            }

            if (button != null)
            {
                button.IsEnabled = true;
                button.Text = " Atualizar Status";
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao atualizar: {ex.Message}");
        }
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        bool confirm = await CustomAlertService.ShowQuestionAsync(
            "Deseja realmente cancelar a solicitacao de atendimento humano e voltar ao ChatBot?",
            "Cancelar Atendimento",
            "Sim",
            "Não"
        );

        if (confirm)
        {
            try
            {
                // Mudar status de volta para "Aberto" (como se estivesse com o chatbot)
                // ou pode deixar como "Aguardando Atendente" caso o usuario mude de ideia
                _pollingTimer?.Stop();
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await CustomAlertService.ShowErrorAsync($"Erro ao cancelar: {ex.Message}");
            }
        }
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        OnCancelarClicked(sender, e);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _pollingTimer?.Stop();
        _pollingTimer?.Dispose();
        
        // Desconectar SignalR
        Task.Run(async () => await DesconectarSignalR());
    }

    private async Task DesconectarSignalR()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                Console.WriteLine("[FilaAtendimento] SignalR desconectado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] Erro ao desconectar: {ex.Message}");
            }
        }
    }
}
