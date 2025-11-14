using System.Windows;

namespace GestaoChamados.Desktop;

/// <summary>
/// Janela principal de login do aplicativo Desktop WPF
/// Gerencia autenticação via API e redirecionamento baseado em roles
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Processa o login do usuário e redireciona para a tela apropriada
    /// Gerente -> Dashboard Gerencial
    /// Técnico/Admin -> Dashboard Técnico
    /// Usuário comum -> Meus Chamados
    /// </summary>
    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorTextBlock.Visibility = Visibility.Collapsed;

        var email = EmailTextBox.Text.Trim();
        var senha = SenhaPasswordBox.Password;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha))
        {
            ErrorTextBlock.Text = "Preencha todos os campos.";
            ErrorTextBlock.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var result = await App.ApiService.LoginAsync(email, senha);

            if (result != null)
            {
                App.CurrentUserName = result.Nome;
                App.CurrentUserEmail = result.Email;
                App.CurrentUserRole = result.Role;
                App.CurrentUserToken = result.Token;
                
                // ✅ CORREÇÃO: Configurar o token no ApiService para enviar nas requisições
                App.ApiService.Token = result.Token;

                // Redirecionar baseado no role
                // Gerente vai para Dashboard Gerencial
                // Técnico e Admin vão para Dashboard Técnico
                // Usuário vai para Meus Chamados
                if (result.Role == "Gerente")
                {
                    var gerenteDashboard = new GerenteDashboardWindow();
                    gerenteDashboard.Show();
                }
                else if (result.Role == "Tecnico" || result.Role == "Admin")
                {
                    var dashboardWindow = new DashboardWindow();
                    dashboardWindow.Show();
                }
                else
                {
                    var chamadosWindow = new ChamadosWindow();
                    chamadosWindow.Show();
                }
                
                this.Close();
            }
            else
            {
                ErrorTextBlock.Text = "Email ou senha inválidos.";
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            ErrorTextBlock.Text = $"Erro ao conectar com a API: {ex.Message}\n\nCertifique-se de que a API está rodando em http://localhost:5142";
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}