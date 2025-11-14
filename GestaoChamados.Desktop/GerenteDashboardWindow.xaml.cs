using System;
using System.Windows;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Desktop
{
    public partial class GerenteDashboardWindow : Window
    {
        public GerenteDashboardWindow()
        {
            InitializeComponent();
            
            // Garantir que o token está configurado no ApiService
            if (!string.IsNullOrEmpty(App.CurrentUserToken))
            {
                App.ApiService.Token = App.CurrentUserToken;
            }
            
            UserInfoTextBlock.Text = App.CurrentUserEmail;
            Loaded += async (s, e) => await CarregarDashboard();
        }

        private async System.Threading.Tasks.Task CarregarDashboard()
        {
            try
            {
                var dashboard = await App.ApiService.GetManagerDashboardAsync();
                
                if (dashboard != null)
                {
                    // Cards principais (3 status simplificados)
                    ResolvidosText.Text = dashboard.ChamadosResolvidos.ToString();
                    EmAtendimentoText.Text = dashboard.ChamadosEmAtendimento.ToString();
                    NaoAtendidosText.Text = dashboard.ChamadosNaoAtendidos.ToString();
                    
                    // Cards secundários
                    UsuariosText.Text = dashboard.TotalUsuarios.ToString();
                    TecnicosText.Text = dashboard.TotalTecnicos.ToString();
                    TotalChamadosText.Text = dashboard.TotalChamados.ToString();
                    TaxaResolucaoText.Text = $"{dashboard.TaxaResolucao:F1}%";
                    AvaliacaoMediaText.Text = $"Média: {dashboard.AvaliaoMediaAtendimento:F1} ⭐";
                    
                    // Atualizar gráfico de distribuição por status
                    AtualizarGraficoStatus(
                        dashboard.ChamadosNaoAtendidos,
                        dashboard.ChamadosEmAtendimento,
                        dashboard.ChamadosResolvidos
                    );
                    
                    // Lista de técnicos
                    TecnicosListBox.ItemsSource = dashboard.DesempenhoPorTecnico;
                }
                else
                {
                    MessageBox.Show("Erro ao carregar dashboard gerencial.", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dashboard: {ex.Message}", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AtualizarGraficoStatus(int aguardando, int emAtendimento, int resolvidos)
        {
            // Atualizar contadores
            AguardandoCountText.Text = aguardando.ToString();
            EmAtendimentoCountText.Text = emAtendimento.ToString();
            ResolvidosCountText.Text = resolvidos.ToString();
            
            // Calcular total para proporção
            int total = aguardando + emAtendimento + resolvidos;
            
            if (total > 0)
            {
                // Largura máxima do gráfico (em pixels)
                double maxWidth = GraficoGrid.ActualWidth > 0 ? GraficoGrid.ActualWidth : 300;
                
                // Calcular larguras proporcionais
                BarraAguardando.Width = Math.Max(5, (aguardando / (double)total) * maxWidth);
                BarraEmAtendimento.Width = Math.Max(5, (emAtendimento / (double)total) * maxWidth);
                BarraResolvidos.Width = Math.Max(5, (resolvidos / (double)total) * maxWidth);
            }
            else
            {
                // Se não houver chamados, mostrar barras mínimas
                BarraAguardando.Width = 5;
                BarraEmAtendimento.Width = 5;
                BarraResolvidos.Width = 5;
            }
        }

        private void GerenciarUsuarios_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gerenciarWindow = new GerenciarUsuariosWindow();
                gerenciarWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir Gerenciar Usuários:\n\n{ex.Message}\n\nDetalhes: {ex.InnerException?.Message}", 
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Relatorio_Click(object sender, RoutedEventArgs e)
        {
            var relatorioWindow = new RelatorioWindow();
            relatorioWindow.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Deseja realmente sair?", "Confirmar Logout", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUserToken = null;
                App.CurrentUserEmail = null;
                App.CurrentUserRole = null;

                var loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
