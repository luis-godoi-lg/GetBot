using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Shared.Services;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views;

public partial class GerenteDashboardPage : ContentPage
{
    private readonly ApiService _apiService;

    public GerenteDashboardPage()
    {
        InitializeComponent();
        _apiService = new ApiService("http://localhost:5142");
        
        if (!string.IsNullOrEmpty(Settings.Token))
        {
            _apiService.Token = Settings.Token;
        }
        
        UserInfoLabel.Text = Settings.UserEmail ?? "Gerente";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarDashboard();
    }

    private async Task CarregarDashboard()
    {
        try
        {
            var dashboard = await _apiService.GetManagerDashboardAsync();

            if (dashboard != null)
            {
                ResolvidosLabel.Text = dashboard.ChamadosResolvidos.ToString();
                EmAtendimentoLabel.Text = dashboard.ChamadosEmAtendimento.ToString();
                NaoAtendidosLabel.Text = dashboard.ChamadosNaoAtendidos.ToString();

                UsuariosLabel.Text = dashboard.TotalUsuarios.ToString();
                TecnicosLabel.Text = dashboard.TotalTecnicos.ToString();
                TotalChamadosLabel.Text = dashboard.TotalChamados.ToString();
                TaxaResolucaoLabel.Text = $"{dashboard.TaxaResolucao:F1}%";
                AvaliacaoMediaLabel.Text = $"Media: {dashboard.AvaliaoMediaAtendimento:F1} ";

                AtualizarGraficoStatus(dashboard.ChamadosNaoAtendidos, dashboard.ChamadosEmAtendimento, dashboard.ChamadosResolvidos);

                TecnicosCollectionView.ItemsSource = dashboard.DesempenhoPorTecnico;
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Erro ao carregar dashboard gerencial.");
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar dashboard: {ex.Message}");
        }
    }

    private void AtualizarGraficoStatus(int aguardando, int emAtendimento, int resolvidos)
    {
        AguardandoCountLabel.Text = aguardando.ToString();
        EmAtendimentoCountLabel.Text = emAtendimento.ToString();
        ResolvidosCountLabel.Text = resolvidos.ToString();

        int total = aguardando + emAtendimento + resolvidos;

        if (total > 0)
        {
            double maxWidth = 300;
            BarraAguardando.WidthRequest = Math.Max(10, (aguardando / (double)total) * maxWidth);
            BarraEmAtendimento.WidthRequest = Math.Max(10, (emAtendimento / (double)total) * maxWidth);
            BarraResolvidos.WidthRequest = Math.Max(10, (resolvidos / (double)total) * maxWidth);
        }
        else
        {
            BarraAguardando.WidthRequest = 10;
            BarraEmAtendimento.WidthRequest = 10;
            BarraResolvidos.WidthRequest = 10;
        }
    }

    private async void GerenciarUsuarios_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GerenciarUsuariosPage));
    }

    private async void Relatorios_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RelatoriosPage));
    }

    private async void OnRelatoriosClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RelatoriosPage));
    }

    private async void OnGerenciarUsuariosClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GerenciarUsuariosPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        Settings.Token = string.Empty;
        Settings.UserEmail = string.Empty;
        Settings.UserRole = string.Empty;
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
