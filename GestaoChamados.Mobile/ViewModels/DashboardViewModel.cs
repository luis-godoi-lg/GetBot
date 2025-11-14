using System.Windows.Input;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;
using GestaoChamados.Mobile.Views;

namespace GestaoChamados.Mobile.ViewModels;

/// <summary>
/// ViewModel para o Dashboard do aplicativo Mobile
/// Exibe estatísticas resumidas de chamados e métricas de desempenho
/// Atualiza dados em tempo real e permite navegação rápida
/// </summary>
public class DashboardViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private int _totalChamados;
    private int _chamadosAbertos;
    private int _chamadosEmAtendimento;
    private int _chamadosResolvidos;

    public int TotalChamados
    {
        get => _totalChamados;
        set => SetProperty(ref _totalChamados, value);
    }

    public int ChamadosAbertos
    {
        get => _chamadosAbertos;
        set => SetProperty(ref _chamadosAbertos, value);
    }

    public int ChamadosEmAtendimento
    {
        get => _chamadosEmAtendimento;
        set => SetProperty(ref _chamadosEmAtendimento, value);
    }

    public int ChamadosResolvidos
    {
        get => _chamadosResolvidos;
        set => SetProperty(ref _chamadosResolvidos, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand VerChamadosCommand { get; }
    public ICommand LogoutCommand { get; }

    public DashboardViewModel()
    {
        _authService = new AuthService();
        Title = "Dashboard";
        
        RefreshCommand = new Command(async () => await LoadStats());
        VerChamadosCommand = new Command(async () => await Shell.Current.GoToAsync($"//{nameof(ChamadosPage)}"));
        LogoutCommand = new Command(async () => await ExecuteLogout());
        
        Task.Run(async () => await LoadStats());
    }

    private async Task LoadStats()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var api = _authService.GetApiService();
            var stats = await api.GetDashboardStatsAsync();

            if (stats != null)
            {
                TotalChamados = stats.TotalChamados;
                ChamadosAbertos = stats.ChamadosAbertos;
                ChamadosEmAtendimento = stats.ChamadosEmAtendimento;
                ChamadosResolvidos = stats.ChamadosFinalizados;
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar estatísticas: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteLogout()
    {
        _authService.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
