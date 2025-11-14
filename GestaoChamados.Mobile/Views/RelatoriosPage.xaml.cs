using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Shared.Services;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views;

public partial class RelatoriosPage : ContentPage
{
    private readonly ApiService _apiService;

    public RelatoriosPage()
    {
        InitializeComponent();
        _apiService = new ApiService("http://localhost:5142");
        
        if (!string.IsNullOrEmpty(Settings.Token))
        {
            _apiService.Token = Settings.Token;
        }

        DataFimPicker.Date = DateTime.Today;
        DataInicioPicker.Date = DateTime.Today.AddDays(-30);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DataFimPicker.Date = DateTime.Now;
        DataInicioPicker.Date = DateTime.Now.AddMonths(-1);
        await CarregarRelatorio();
    }

    private async Task CarregarRelatorio()
    {
        try
        {
            var dataInicio = DataInicioPicker.Date;
            var dataFim = DataFimPicker.Date;

            System.Diagnostics.Debug.WriteLine($"[RelatoriosPage] Carregando relatório - Início: {dataInicio:yyyy-MM-dd}, Fim: {dataFim:yyyy-MM-dd}");
            System.Diagnostics.Debug.WriteLine($"[RelatoriosPage] Token configurado: {!string.IsNullOrEmpty(_apiService.Token)}");

            var relatorio = await _apiService.GetRelatorioDetalhadoAsync(dataInicio, dataFim);
            
            System.Diagnostics.Debug.WriteLine($"[RelatoriosPage] Relatório recebido: {(relatorio != null ? "SIM" : "NULL")}");

            if (relatorio != null)
            {
                System.Diagnostics.Debug.WriteLine($"[RelatoriosPage] Total: {relatorio.TotalChamados}, Resolvidos: {relatorio.Resolvidos}, Técnicos: {relatorio.ChamadosPorTecnico.Count}");
                
                TotalLabel.Text = relatorio.TotalChamados.ToString();
                AguardandoLabel.Text = relatorio.NaoAtendidos.ToString();
                EmAtendimentoLabel.Text = relatorio.EmAtendimento.ToString();
                ResolvidosLabel.Text = relatorio.Resolvidos.ToString();

                var tecnicosComTaxa = relatorio.ChamadosPorTecnico.Select(t => new
                {
                    Tecnico = t.Tecnico,
                    Total = t.Total,
                    Resolvidos = t.Resolvidos,
                    TaxaResolucaoDecimal = t.Total > 0 ? (double)t.Resolvidos / t.Total : 0,
                    TaxaResolucaoText = t.Total > 0 ? $"{(double)t.Resolvidos / t.Total * 100:F1}%" : "0%",
                    NotaMedia = t.NotaMedia
                }).ToList();

                TecnicosCollectionView.ItemsSource = tecnicosComTaxa;
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Erro ao carregar relatorio.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[RelatoriosPage] EXCEÇÃO: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[RelatoriosPage] StackTrace: {ex.StackTrace}");
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar relatorio: {ex.Message}");
        }
    }

    private async void Filtrar_Clicked(object sender, EventArgs e)
    {
        if (DataInicioPicker.Date > DataFimPicker.Date)
        {
            await CustomAlertService.ShowWarningAsync("Data inicial nao pode ser maior que data final.", "Validacao");
            return;
        }

        await CarregarRelatorio();
    }

    private async void VoltarDashboard_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBuscarClicked(object sender, EventArgs e)
    {
        if (DataInicioPicker.Date > DataFimPicker.Date)
        {
            await CustomAlertService.ShowWarningAsync("Data inicial não pode ser maior que data final.", "Validação");
            return;
        }

        await CarregarRelatorio();
    }
}
