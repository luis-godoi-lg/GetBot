using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Desktop;

public partial class ChamadosWindow : Window
{
    private ObservableCollection<ChamadoDisplayDto> _chamados = new();
    private List<ChamadoDto>? _todosOsChamados;

    public ChamadosWindow()
    {
        InitializeComponent();
        Loaded += ChamadosWindow_Loaded;
    }

    private async void ChamadosWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Atualizar informações do usuário
        UserInfoTextBlock.Text = App.CurrentUserEmail ?? App.CurrentUserName ?? "Usuário";
        
        // Ajustar interface baseado no role
        if (App.CurrentUserRole == "Tecnico" || App.CurrentUserRole == "Gerente" || App.CurrentUserRole == "Admin")
        {
            // Técnico vê "Chamados" e "Dashboard", mas NÃO vê o botão de abrir chamado
            ChamadosButton.Content = "Chamados";
            DashboardButton.Visibility = Visibility.Visible;
            NovoChamadoButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            // Usuário vê apenas "Meus Chamados" e o botão de abrir chamado
            ChamadosButton.Content = "Meus Chamados";
            DashboardButton.Visibility = Visibility.Collapsed;
            NovoChamadoButton.Visibility = Visibility.Visible;
        }
        
        // Carregar chamados
        await CarregarChamadosAsync();
    }

    private async Task CarregarChamadosAsync()
    {
        try
        {
            LoadingTextBlock.Visibility = Visibility.Visible;
            ChamadosItemsControl.Visibility = Visibility.Collapsed;
            NenhumChamadoTextBlock.Visibility = Visibility.Collapsed;

            // Buscar chamados
            var todosChamados = await App.ApiService.GetChamadosAsync();
            
            // Filtrar chamados baseado no role
            if (App.CurrentUserRole == "Tecnico")
            {
                // Técnico vê APENAS os chamados que ele atendeu (onde ele é o técnico)
                var emailTecnico = App.CurrentUserEmail ?? App.CurrentUserName ?? "";
                _todosOsChamados = todosChamados?
                    .Where(c => !string.IsNullOrEmpty(c.TecnicoNome) && 
                               (c.TecnicoNome.Equals(emailTecnico, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }
            else if (App.CurrentUserRole == "Gerente" || App.CurrentUserRole == "Admin")
            {
                // Gerente/Admin vê todos os chamados
                _todosOsChamados = todosChamados;
            }
            else
            {
                // Usuário comum vê apenas seus próprios chamados
                _todosOsChamados = await App.ApiService.GetMeusChamadosAsync();
            }

            LoadingTextBlock.Visibility = Visibility.Collapsed;

            if (_todosOsChamados == null || _todosOsChamados.Count == 0)
            {
                NenhumChamadoTextBlock.Visibility = Visibility.Visible;
                return;
            }

            AtualizarListaChamados();
        }
        catch (Exception ex)
        {
            LoadingTextBlock.Visibility = Visibility.Collapsed;
            MessageBox.Show($"Erro ao carregar chamados:\n{ex.Message}", 
                "Erro", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
    }

    private void AtualizarListaChamados()
    {
        if (_todosOsChamados == null) return;

        _chamados.Clear();
        foreach (var chamado in _todosOsChamados)
        {
            _chamados.Add(new ChamadoDisplayDto
            {
                Id = chamado.Id,
                Protocolo = chamado.Id.ToString("D7"), // Formato: 2025001
                Titulo = chamado.Titulo,
                Descricao = chamado.Descricao,
                Status = chamado.Status,
                StatusTexto = GetStatusTexto(chamado.Status),
                StatusColor = GetStatusColor(chamado.Status),
                DataCriacao = chamado.DataCriacao,
                DataCriacaoFormatada = chamado.DataCriacao.ToString("dd/MM/yyyy HH:mm"),
                UsuarioNome = chamado.UsuarioNome,
                UsuarioEmail = chamado.UsuarioEmail ?? chamado.UsuarioNome,
                TecnicoNome = string.IsNullOrEmpty(chamado.TecnicoNome) ? "Não atribuído" : chamado.TecnicoNome
            });
        }

        ChamadosItemsControl.ItemsSource = _chamados;

        if (_chamados.Count > 0)
        {
            ChamadosItemsControl.Visibility = Visibility.Visible;
            NenhumChamadoTextBlock.Visibility = Visibility.Collapsed;
        }
        else
        {
            ChamadosItemsControl.Visibility = Visibility.Collapsed;
            NenhumChamadoTextBlock.Visibility = Visibility.Visible;
        }
    }

    private string GetStatusTexto(string status)
    {
        return status switch
        {
            "Aberto" => "Aberto",
            "EmAndamento" => "Em Atendimento",
            "Resolvido" => "Resolvido",
            "Fechado" => "Fechado",
            _ => status
        };
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "Aberto" => "#DC3545",      // Vermelho
            "EmAndamento" => "#FFC107", // Amarelo
            "Resolvido" => "#28A745",   // Verde
            "Fechado" => "#6C757D",     // Cinza
            _ => "#6C757D"
        };
    }

    private void NovoChamadoButton_Click(object sender, RoutedEventArgs e)
    {
        var novoChamadoWindow = new NovoChamadoWindow();
        novoChamadoWindow.Show();
        this.Close();
    }

    private void VerDetalhesButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is int chamadoId)
        {
            // Buscar o chamado completo
            var chamado = _todosOsChamados?.FirstOrDefault(c => c.Id == chamadoId);
            
            if (chamado != null)
            {
                var detalhesWindow = new DetalhesWindow(chamado);
                var resultado = detalhesWindow.ShowDialog();
                
                // Se o chamado foi assumido, recarregar a lista
                if (resultado == true)
                {
                    _ = CarregarChamadosAsync();
                }
            }
            else
            {
                MessageBox.Show("Chamado não encontrado.", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Deseja realmente sair?", 
            "Confirmar", 
            MessageBoxButton.YesNo, 
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            App.ApiService.Logout();
            App.CurrentUserName = null;
            App.CurrentUserRole = null;

            var loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }
    }

    private void DashboardButton_Click(object sender, RoutedEventArgs e)
    {
        var dashboardWindow = new DashboardWindow();
        dashboardWindow.Show();
        this.Close();
    }
}

// DTO auxiliar para exibição
public class ChamadoDisplayDto
{
    public int Id { get; set; }
    public string Protocolo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusTexto { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public string DataCriacaoFormatada { get; set; } = string.Empty;
    public string UsuarioNome { get; set; } = string.Empty;
    public string UsuarioEmail { get; set; } = string.Empty;
    public string TecnicoNome { get; set; } = string.Empty;
}
