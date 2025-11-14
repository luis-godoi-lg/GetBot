using System.Windows.Input;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;
using GestaoChamados.Mobile.Views;

namespace GestaoChamados.Mobile.ViewModels;

/// <summary>
/// ViewModel para a tela de login do aplicativo Mobile MAUI
/// Gerencia autenticação via API e redirecionamento baseado em roles (Admin, Técnico, Usuário)
/// </summary>
public class LoginViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private string _email = string.Empty;
    private string _senha = string.Empty;
    private string _mensagemErro = string.Empty;

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Senha
    {
        get => _senha;
        set => SetProperty(ref _senha, value);
    }

    public string MensagemErro
    {
        get => _mensagemErro;
        set => SetProperty(ref _mensagemErro, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel()
    {
        _authService = new AuthService();
        Title = "Login";
        LoginCommand = new Command(async () => await ExecuteLoginCommand(), () => !IsBusy);
    }

    private async Task ExecuteLoginCommand()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        {
            MensagemErro = "Por favor, preencha todos os campos.";
            return;
        }

        IsBusy = true;
        MensagemErro = string.Empty;

        try
        {
            var success = await _authService.LoginAsync(Email, Senha);

            if (success)
            {
                // Debug: Verificar role salvo
                Console.WriteLine($"[LOGIN] UserRole: {Settings.UserRole}");
                Console.WriteLine($"[LOGIN] IsAdmin: {Settings.IsAdmin}");
                Console.WriteLine($"[LOGIN] IsTecnico: {Settings.IsTecnico}");

                // Navegar para a tela principal baseado no role
                if (Settings.IsAdmin)
                {
                    Console.WriteLine("[LOGIN] Navegando para GerenteDashboardPage (Admin)");
                    await Shell.Current.GoToAsync($"//{nameof(GerenteDashboardPage)}");
                }
                else if (Settings.IsTecnico)
                {
                    Console.WriteLine("[LOGIN] Navegando para DashboardPage (Técnico)");
                    await Shell.Current.GoToAsync($"//{nameof(DashboardPage)}");
                }
                else
                {
                    Console.WriteLine("[LOGIN] Navegando para ChamadosPage");
                    await Shell.Current.GoToAsync($"//{nameof(ChamadosPage)}");
                }
            }
            else
            {
                MensagemErro = "Email ou senha inválidos.";
            }
        }
        catch (Exception ex)
        {
            MensagemErro = $"Erro ao fazer login: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
