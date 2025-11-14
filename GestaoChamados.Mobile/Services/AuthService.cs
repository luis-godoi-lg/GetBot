using GestaoChamados.Shared.Services;
using GestaoChamados.Mobile.Helpers;

namespace GestaoChamados.Mobile.Services;

/// <summary>
/// Serviço de autenticação do aplicativo Mobile
/// Gerencia login, logout, persistência de token JWT e configurações do usuário
/// Usa Preferences do MAUI para armazenamento seguro de credenciais
/// </summary>
public class AuthService
{
    private readonly ApiService _apiService;

    public AuthService()
    {
        _apiService = new ApiService(Settings.ApiBaseUrl);
    }

    public ApiService GetApiService()
    {
        if (!string.IsNullOrEmpty(Settings.Token))
        {
            _apiService.Token = Settings.Token;
        }
        return _apiService;
    }

    public async Task<bool> LoginAsync(string email, string senha)
    {
        try
        {
            Console.WriteLine($"[AUTH] Tentando login com: {email}");
            var result = await _apiService.LoginAsync(email, senha);
            
            if (result != null)
            {
                Console.WriteLine($"[AUTH] Login bem-sucedido!");
                Console.WriteLine($"[AUTH] Token: {result.Token.Substring(0, 20)}...");
                Console.WriteLine($"[AUTH] Email: {result.Email}");
                Console.WriteLine($"[AUTH] Nome: {result.Nome}");
                Console.WriteLine($"[AUTH] Role: {result.Role}");
                
                Settings.Token = result.Token;
                Settings.UserEmail = result.Email;
                Settings.UserName = result.Nome;
                Settings.UserRole = result.Role;
                
                Console.WriteLine($"[AUTH] Settings.UserRole salvo: {Settings.UserRole}");
                Console.WriteLine($"[AUTH] Settings.IsAdmin: {Settings.IsAdmin}");
                
                return true;
            }
            
            Console.WriteLine("[AUTH] Login falhou - result null");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH] Erro no login: {ex.Message}");
            return false;
        }
    }

    public void Logout()
    {
        _apiService.Logout();
        Settings.ClearAll();
    }

    public bool IsLoggedIn()
    {
        return Settings.IsLoggedIn;
    }
}
