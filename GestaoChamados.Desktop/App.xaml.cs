using System.Windows;
using GestaoChamados.Shared.Services;

namespace GestaoChamados.Desktop;

/// <summary>
/// Classe Application principal do Desktop WPF
/// Configura o ApiService global, gerencia sessão do usuário e trata exceções não capturadas
/// </summary>
public partial class App : Application
{
    /// <summary>Instância global do serviço de comunicação com a API</summary>
    public static ApiService ApiService { get; private set; } = null!;
    
    /// <summary>Nome do usuário logado</summary>
    public static string? CurrentUserName { get; set; }
    
    /// <summary>Email do usuário logado</summary>
    public static string? CurrentUserEmail { get; set; }
    
    /// <summary>Role/Perfil do usuário: Usuario, Tecnico, Gerente ou Admin</summary>
    public static string? CurrentUserRole { get; set; }
    
    /// <summary>Token JWT para autenticação nas requisições</summary>
    public static string? CurrentUserToken { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Capturar exceções não tratadas
        DispatcherUnhandledException += (s, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"[APP] EXCEÇÃO NÃO TRATADA: {args.Exception.Message}");
            System.Diagnostics.Debug.WriteLine($"[APP] StackTrace: {args.Exception.StackTrace}");
            
            MessageBox.Show(
                $"Erro não tratado:\n\n{args.Exception.Message}\n\n" +
                $"Stack Trace:\n{args.Exception.StackTrace}\n\n" +
                $"Inner Exception:\n{args.Exception.InnerException?.Message}",
                "Erro Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            args.Handled = true; // Evitar crash imediato
        };
        
        // Inicializar serviço de API - usando HTTP pois HTTPS não está configurado
        ApiService = new ApiService("http://localhost:5142");
    }
}

                                        