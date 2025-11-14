using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GestaoChamados.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace GestaoChamados.Desktop
{
    public partial class AguardandoAtendimentoWindow : Window
    {
        private readonly int _chamadoId;
        private HubConnection? _hubConnection;
        private DispatcherTimer? _pollingTimer;
        private bool _jaAbriuChat = false; // ✅ Flag para evitar abrir chat duas vezes

        public AguardandoAtendimentoWindow(ChamadoDto chamado)
        {
            InitializeComponent();
            _chamadoId = chamado.Id;

            // Preencher informações
            ProtocoloText.Text = $"Protocolo: #{chamado.Id:D7}";
            AssuntoText.Text = chamado.Titulo; // ChamadoDto usa "Titulo" não "Assunto"
            DataCriacaoText.Text = chamado.DataCriacao.ToString("dd/MM/yyyy HH:mm");

            // Iniciar animação
            IniciarAnimacao();

            // Iniciar monitoramento do status
            IniciarMonitoramento();
        }

        private void IniciarAnimacao()
        {
            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 1.2,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            CircleScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
            CircleScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
        }

        private async void IniciarMonitoramento()
        {
            try
            {
                // ✅ CORREÇÃO: Usar HTTP ao invés de HTTPS (API roda em HTTP)
                var apiBaseUrl = App.ApiService.GetBaseUrl().Replace("/api", "");
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{apiBaseUrl}/supportHub", options =>
                    {
                        options.AccessTokenProvider = () => System.Threading.Tasks.Task.FromResult(App.CurrentUserToken);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                // Evento quando técnico assume o chamado
                _hubConnection.On<int>("ChamadoAssumido", (chamadoId) =>
                {
                    if (chamadoId == _chamadoId && !_jaAbriuChat)
                    {
                        _jaAbriuChat = true; // ✅ Marcar como já processado
                        _pollingTimer?.Stop(); // ✅ Parar polling
                        
                        Dispatcher.Invoke(async () =>
                        {
                            StatusText.Text = "✅ Técnico assumiu seu chamado! Abrindo chat...";
                            await System.Threading.Tasks.Task.Delay(1000);

                            // Abrir janela de chat ao vivo
                            var chatWindow = new ChatAoVivoWindow(_chamadoId);
                            chatWindow.Show();
                            this.Close();
                        });
                    }
                });

                await _hubConnection.StartAsync();
                
                // ✅ CORREÇÃO: Conectar ao grupo do ticket para receber notificações
                await _hubConnection.InvokeAsync("JoinTicketGroup", _chamadoId.ToString());
                
                System.Diagnostics.Debug.WriteLine($"[AguardandoAtendimento] Conectado ao SignalR e ao grupo do ticket {_chamadoId}");

                // Polling de backup (a cada 5 segundos)
                _pollingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(5)
                };
                _pollingTimer.Tick += async (s, e) => await VerificarStatus();
                _pollingTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao sistema de notificações: {ex.Message}\n\nVocê será notificado por polling.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async System.Threading.Tasks.Task VerificarStatus()
        {
            // ✅ Se já abriu o chat via SignalR, não fazer nada
            if (_jaAbriuChat)
                return;
                
            try
            {
                var chamado = await App.ApiService.GetChamadoAsync(_chamadoId);
                // ✅ CORREÇÃO: Verificar status "Em Atendimento" ou se técnico assumiu
                if (chamado != null && (chamado.Status == "Em Atendimento" || 
                                       !string.IsNullOrEmpty(chamado.TecnicoNome)))
                {
                    _jaAbriuChat = true; // ✅ Marcar como já processado
                    _pollingTimer?.Stop();
                    StatusText.Text = "✅ Técnico assumiu seu chamado! Abrindo chat...";
                    await System.Threading.Tasks.Task.Delay(1000);

                    // Abrir janela de chat ao vivo
                    var chatWindow = new ChatAoVivoWindow(_chamadoId);
                    chatWindow.Show();
                    this.Close();
                }
            }
            catch
            {
                // Ignorar erros de polling
            }
        }

        private void VerChamados_Click(object sender, RoutedEventArgs e)
        {
            var chamadosWindow = new ChamadosWindow();
            chamadosWindow.Show();
            this.Close();
        }

        private void Voltar_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        protected override async void OnClosed(EventArgs e)
        {
            _pollingTimer?.Stop();
            
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
}
