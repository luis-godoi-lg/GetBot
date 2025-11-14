using System.Collections.ObjectModel;
using System.Windows.Input;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;
using GestaoChamados.Mobile.Views;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Mobile.ViewModels;

/// <summary>
/// Extensão de ChamadoDto com propriedades calculadas para UI binding
/// Adiciona formatação de protocolo, cores de status e textos formatados
/// </summary>
public class ChamadoDtoExtended : ChamadoDto
{
    public string Protocolo => $"#{Id.ToString().PadLeft(4, '0')}";
    
    public string StatusTexto => Status switch
    {
        "Aberto" => "ABERTO",
        "Em Atendimento" => "EM ATENDIMENTO",
        "Resolvido" => "RESOLVIDO",
        _ => Status.ToUpper()
    };
    
    public string StatusColor => Status switch
    {
        "Aberto" => "#EF4444", // Vermelho
        "Em Atendimento" => "#F59E0B", // Laranja
        "Resolvido" => "#10B981", // Verde
        _ => "#6B7280" // Cinza padrão
    };
    
    public string DataCriacaoFormatada => DataCriacao.ToString("dd/MM/yyyy HH:mm");
    
    public string TecnicoNomeExibicao => string.IsNullOrEmpty(TecnicoNome) ? "Não atribuído" : TecnicoNome;
}

/// <summary>
/// ViewModel para a tela de listagem de chamados no Mobile
/// Gerencia carregamento, filtragem e navegação para detalhes dos chamados
/// </summary>
public class ChamadosViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private ObservableCollection<ChamadoDtoExtended> _chamados = new();
    private ChamadoDtoExtended? _selectedChamado;
    private string _userEmail = string.Empty;

    public ObservableCollection<ChamadoDtoExtended> Chamados
    {
        get => _chamados;
        set => SetProperty(ref _chamados, value);
    }

    public ChamadoDtoExtended? SelectedChamado
    {
        get => _selectedChamado;
        set
        {
            SetProperty(ref _selectedChamado, value);
            if (value != null)
            {
                NavigateToChamadoDetails(value);
            }
        }
    }

    public string UserEmail
    {
        get => _userEmail;
        set => SetProperty(ref _userEmail, value);
    }

    public bool IsNotBusy => !IsBusy;
    public bool NenhumChamado => !IsBusy && Chamados.Count == 0;

    public ICommand RefreshCommand { get; }
    public ICommand NovoChamadoCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand VerDetalhesCommand { get; }

    public ChamadosViewModel()
    {
        _authService = new AuthService();
        Title = "Meus Chamados";
        
        // Carregar email do usuário
        UserEmail = Preferences.Get("user_email", "Usuário");
        
        RefreshCommand = new Command(async () => await LoadChamados());
        NovoChamadoCommand = new Command(async () => await NavigateToNovoChamado());
        LogoutCommand = new Command(async () => await ExecuteLogout());
        VerDetalhesCommand = new Command<ChamadoDtoExtended>((chamado) => NavigateToChamadoDetails(chamado));
        
        Task.Run(async () => await LoadChamados());
    }

    private async Task LoadChamados()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            
            var api = _authService.GetApiService();
            var todosChamados = await api.GetChamadosAsync();

            if (todosChamados != null)
            {
                // Filtrar chamados baseado no role
                var role = Settings.UserRole;
                var userEmail = Settings.UserEmail ?? "";
                
                List<ChamadoDto> chamadosFiltrados;
                
                if (role == "Tecnico")
                {
                    // Técnico vê APENAS os chamados que ele atendeu
                    chamadosFiltrados = todosChamados
                        .Where(c => !string.IsNullOrEmpty(c.TecnicoNome) && 
                                   c.TecnicoNome.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else if (role == "Gerente" || role == "Admin")
                {
                    // Gerente/Admin vê todos
                    chamadosFiltrados = todosChamados;
                }
                else
                {
                    // Usuário comum vê apenas os seus
                    chamadosFiltrados = todosChamados
                        .Where(c => c.UsuarioEmail != null && 
                                   c.UsuarioEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Limpar lista existente
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Chamados.Clear();
                    
                    foreach (var chamado in chamadosFiltrados)
                    {
                        // Converter ChamadoDto para ChamadoDtoExtended
                        var extended = new ChamadoDtoExtended
                        {
                            Id = chamado.Id,
                            Titulo = chamado.Titulo,
                            Descricao = chamado.Descricao,
                            Status = chamado.Status,
                            Prioridade = chamado.Prioridade,
                            DataCriacao = chamado.DataCriacao,
                            DataFinalizacao = chamado.DataFinalizacao,
                            UsuarioId = chamado.UsuarioId,
                            UsuarioNome = chamado.UsuarioNome,
                            UsuarioEmail = chamado.UsuarioEmail,
                            TecnicoId = chamado.TecnicoId,
                            TecnicoNome = chamado.TecnicoNome,
                            Rating = chamado.Rating
                        };
                        Chamados.Add(extended);
                    }
                    
                    OnPropertyChanged(nameof(NenhumChamado));
                });
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar chamados: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(NenhumChamado));
        }
    }

    private async void NavigateToChamadoDetails(ChamadoDtoExtended? chamado)
    {
        if (chamado == null) return;
        await Shell.Current.GoToAsync($"{nameof(DetalhesChamadoPage)}?ChamadoId={chamado.Id}");
        SelectedChamado = null;
    }

    private async Task NavigateToNovoChamado()
    {
        await Shell.Current.GoToAsync(nameof(NovoChamadoPage));
    }

    private async Task ExecuteLogout()
    {
        _authService.Logout();
        await Shell.Current.GoToAsync($"//{nameof(LoginPage)}");
    }
}
