using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using GestaoChamados.Shared.DTOs;
using Microsoft.Win32;

namespace GestaoChamados.Desktop
{
    public partial class RelatorioWindow : Window
    {
        private RelatorioDetalhadoDto? _relatorioAtual;

        public RelatorioWindow()
        {
            InitializeComponent();
            
            // Garantir que o token está configurado no ApiService
            if (!string.IsNullOrEmpty(App.CurrentUserToken))
            {
                App.ApiService.Token = App.CurrentUserToken;
            }
            
            // Definir período padrão (últimos 30 dias)
            DataFimDatePicker.SelectedDate = DateTime.Today;
            DataInicioDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
            
            Loaded += async (s, e) => await CarregarRelatorio();
        }

        private async System.Threading.Tasks.Task CarregarRelatorio()
        {
            try
            {
                var dataInicio = DataInicioDatePicker.SelectedDate;
                var dataFim = DataFimDatePicker.SelectedDate;

                System.Diagnostics.Debug.WriteLine($"[RelatorioWindow] Carregando relatório - Início: {dataInicio}, Fim: {dataFim}");
                System.Diagnostics.Debug.WriteLine($"[RelatorioWindow] Token configurado: {!string.IsNullOrEmpty(App.ApiService.Token)}");

                _relatorioAtual = await App.ApiService.GetRelatorioDetalhadoAsync(dataInicio, dataFim);
                
                System.Diagnostics.Debug.WriteLine($"[RelatorioWindow] Relatório recebido: {(_relatorioAtual != null ? "SIM" : "NULL")}");
                
                if (_relatorioAtual != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[RelatorioWindow] Total: {_relatorioAtual.TotalChamados}, Resolvidos: {_relatorioAtual.Resolvidos}, Técnicos: {_relatorioAtual.ChamadosPorTecnico.Count}");
                
                    // Atualizar cards (4 status simplificados)
                    TotalText.Text = _relatorioAtual.TotalChamados.ToString();
                    NaoAtendidosText.Text = _relatorioAtual.NaoAtendidos.ToString();
                    EmAtendimentoText.Text = _relatorioAtual.EmAtendimento.ToString();
                    ResolvidosText.Text = _relatorioAtual.Resolvidos.ToString();
                    
                    // Preparar dados dos técnicos com taxa de resolução
                    var tecnicosComTaxa = _relatorioAtual.ChamadosPorTecnico.Select(t => new
                    {
                        Tecnico = t.Tecnico,
                        Total = t.Total,
                        Resolvidos = t.Resolvidos,
                        TaxaResolucao = t.Total > 0 ? (double)t.Resolvidos / t.Total * 100 : 0,
                        TaxaResolucaoText = t.Total > 0 ? $"{(double)t.Resolvidos / t.Total * 100:F1}%" : "0%",
                        NotaMedia = t.NotaMedia,
                        NotaMediaText = t.NotaMedia > 0 ? $"{t.NotaMedia:F1} ⭐" : "--"
                    }).ToList();
                    
                    TecnicosDataGrid.ItemsSource = tecnicosComTaxa;
                }
                else
                {
                    MessageBox.Show("Erro ao carregar relatório.", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RelatorioWindow] EXCEÇÃO ao carregar relatório: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RelatorioWindow] StackTrace: {ex.StackTrace}");
                MessageBox.Show($"Erro ao carregar relatório: {ex.Message}\n\nDetalhes: {ex.InnerException?.Message}", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Filtrar_Click(object sender, RoutedEventArgs e)
        {
            if (!DataInicioDatePicker.SelectedDate.HasValue || !DataFimDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Selecione o período para o relatório.", "Validação", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (DataInicioDatePicker.SelectedDate > DataFimDatePicker.SelectedDate)
            {
                MessageBox.Show("Data inicial não pode ser maior que data final.", "Validação", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await CarregarRelatorio();
        }

        private void ExportarCSV_Click(object sender, RoutedEventArgs e)
        {
            if (_relatorioAtual == null)
            {
                MessageBox.Show("Não há dados para exportar.", "Aviso", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Arquivo CSV (*.csv)|*.csv",
                FileName = $"Relatorio_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                Title = "Salvar Relatório"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var csv = new StringBuilder();
                    
                    // Header
                    csv.AppendLine("RELATÓRIO DE CHAMADOS");
                    csv.AppendLine($"Período: {DataInicioDatePicker.SelectedDate:dd/MM/yyyy} a {DataFimDatePicker.SelectedDate:dd/MM/yyyy}");
                    csv.AppendLine($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
                    csv.AppendLine();
                    
                    // Resumo
                    csv.AppendLine("RESUMO GERAL");
                    csv.AppendLine($"Total de Chamados,{_relatorioAtual.TotalChamados}");
                    csv.AppendLine($"Abertos,{_relatorioAtual.Abertos}");
                    csv.AppendLine($"Em Atendimento,{_relatorioAtual.EmAtendimento}");
                    csv.AppendLine($"Resolvidos,{_relatorioAtual.Resolvidos}");
                    csv.AppendLine($"Não Atendidos,{_relatorioAtual.NaoAtendidos}");
                    csv.AppendLine();
                    
                    // Desempenho por Técnico
                    csv.AppendLine("DESEMPENHO POR TÉCNICO");
                    csv.AppendLine("Técnico,Total de Chamados,Resolvidos,Nota Média,Taxa de Resolução");
                    
                    foreach (var tecnico in _relatorioAtual.ChamadosPorTecnico)
                    {
                        var taxa = tecnico.Total > 0 ? (double)tecnico.Resolvidos / tecnico.Total * 100 : 0;
                        var notaMedia = tecnico.NotaMedia > 0 ? $"{tecnico.NotaMedia:F1}" : "--";
                        csv.AppendLine($"{tecnico.Tecnico},{tecnico.Total},{tecnico.Resolvidos},{notaMedia},{taxa:F1}%");
                    }
                    
                    File.WriteAllText(saveFileDialog.FileName, csv.ToString(), Encoding.UTF8);
                    
                    MessageBox.Show($"Relatório exportado com sucesso!\n\n{saveFileDialog.FileName}", 
                        "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao exportar relatório: {ex.Message}", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void VoltarDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new GerenteDashboardWindow();
            dashboard.Show();
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
