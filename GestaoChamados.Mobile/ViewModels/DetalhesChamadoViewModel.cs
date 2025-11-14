using System.Collections.ObjectModel;
using System.Windows.Input;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Mobile.ViewModels;

[QueryProperty(nameof(ChamadoId), "ChamadoId")]
public class DetalhesChamadoViewModel : BaseViewModel
{
    private readonly AuthService _authService;
    private ChamadoDto? _chamado;
    private ObservableCollection<ChatMessageDto> _mensagens = new();
    private string _novaMensagem = string.Empty;
    private int _chamadoId;
    private string _tituloHeader = "Detalhes";
    private string _protocoloTexto = "";
    private string _tecnicoTexto = "";
    private string _statusColor = "#6B7280";
    private bool _chatVisivel = false;

    public int ChamadoId
    {
        get => _chamadoId;
        set
        {
            _chamadoId = value;
            Task.Run(async () => await LoadChamado());
        }
    }

    public ChamadoDto? Chamado
    {
        get => _chamado;
        set
        {
            SetProperty(ref _chamado, value);
            if (value != null)
            {
                TituloHeader = value.Titulo;
                ProtocoloTexto = $"Protocolo #{value.Id:D4}";
                TecnicoTexto = string.IsNullOrEmpty(value.TecnicoNome) ? "Não atribuído" : value.TecnicoNome;
                StatusColor = value.Status switch
                {
                    "Aberto" => "#EF4444",
                    "Em Atendimento" => "#F59E0B",
                    "Resolvido" => "#10B981",
                    _ => "#6B7280"
                };
                ChatVisivel = value.Status == "Em Atendimento";
            }
        }
    }

    public string TituloHeader
    {
        get => _tituloHeader;
        set => SetProperty(ref _tituloHeader, value);
    }

    public string ProtocoloTexto
    {
        get => _protocoloTexto;
        set => SetProperty(ref _protocoloTexto, value);
    }

    public string TecnicoTexto
    {
        get => _tecnicoTexto;
        set => SetProperty(ref _tecnicoTexto, value);
    }

    public string StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }

    public bool ChatVisivel
    {
        get => _chatVisivel;
        set => SetProperty(ref _chatVisivel, value);
    }

    public ObservableCollection<ChatMessageDto> Mensagens
    {
        get => _mensagens;
        set => SetProperty(ref _mensagens, value);
    }

    public string NovaMensagem
    {
        get => _novaMensagem;
        set => SetProperty(ref _novaMensagem, value);
    }

    public ICommand EnviarMensagemCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand SolicitarAtendimentoCommand { get; }
    public ICommand MarcarResolvidoCommand { get; }
    public ICommand AvaliarCommand { get; }
    public ICommand VoltarCommand { get; }

    public DetalhesChamadoViewModel()
    {
        _authService = new AuthService();
        Title = "Detalhes do Chamado";
        
        EnviarMensagemCommand = new Command(async () => await EnviarMensagem());
        RefreshCommand = new Command(async () => await LoadChamado());
        SolicitarAtendimentoCommand = new Command(async () => await SolicitarAtendimento());
        MarcarResolvidoCommand = new Command(async () => await MarcarResolvido());
        AvaliarCommand = new Command<string>(async (rating) => await AvaliarChamado(rating));
        VoltarCommand = new Command(async () => await Shell.Current.GoToAsync(".."));
    }

    private async Task LoadChamado()
    {
        if (IsBusy)
            return;

        try
        {
            IsBusy = true;
            var api = _authService.GetApiService();
            
            Chamado = await api.GetChamadoAsync(ChamadoId);
            
            if (Chamado != null)
            {
                Title = $"Chamado #{Chamado.Id}";
                await LoadMensagens();
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar chamado: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMensagens()
    {
        try
        {
            var api = _authService.GetApiService();
            var mensagens = await api.GetMensagensChamadoAsync(ChamadoId);

            if (mensagens != null)
            {
                Mensagens.Clear();
                foreach (var msg in mensagens.OrderBy(m => m.DataEnvio))
                {
                    Mensagens.Add(msg);
                }
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar mensagens: {ex.Message}");
        }
    }

    private async Task EnviarMensagem()
    {
        if (string.IsNullOrWhiteSpace(NovaMensagem))
            return;

        try
        {
            var api = _authService.GetApiService();
            var mensagem = await api.EnviarMensagemAsync(ChamadoId, NovaMensagem);

            if (mensagem != null)
            {
                Mensagens.Add(mensagem);
                NovaMensagem = string.Empty;
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao enviar mensagem: {ex.Message}");
        }
    }

    private async Task SolicitarAtendimento()
    {
        var confirm = await CustomAlertService.ShowQuestionAsync("Deseja solicitar atendimento humano?");

        if (!confirm)
            return;

        try
        {
            var api = _authService.GetApiService();
            var success = await api.SolicitarAtendimentoAsync(ChamadoId);

            if (success)
            {
                await CustomAlertService.ShowSuccessAsync("Atendimento solicitado com sucesso!");
                await LoadChamado();
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Não foi possível solicitar atendimento.");
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro: {ex.Message}");
        }
    }

    private async Task MarcarResolvido()
    {
        var confirm = await CustomAlertService.ShowQuestionAsync("Marcar este chamado como resolvido?");

        if (!confirm)
            return;

        try
        {
            var api = _authService.GetApiService();
            var success = await api.MarcarComoResolvidoAsync(ChamadoId);

            if (success)
            {
                await CustomAlertService.ShowSuccessAsync("Chamado marcado como resolvido!");
                await LoadChamado();
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Não foi possível marcar como resolvido.");
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro: {ex.Message}");
        }
    }

    private async Task AvaliarChamado(string ratingStr)
    {
        if (!int.TryParse(ratingStr, out int rating))
            return;

        try
        {
            var api = _authService.GetApiService();
            var success = await api.AvaliarChamadoAsync(ChamadoId, rating);

            if (success)
            {
                await CustomAlertService.ShowSuccessAsync("Avaliação enviada com sucesso!");
                await LoadChamado();
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Não foi possível enviar avaliação.");
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro: {ex.Message}");
        }
    }
}
