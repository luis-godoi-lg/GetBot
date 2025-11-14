using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Shared.Services;
using GestaoChamados.Shared.DTOs;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views;

public partial class GerenciarUsuariosPage : ContentPage
{
    private readonly ApiService _apiService;
    private List<UsuarioViewModel> _todosUsuarios = new();

    public GerenciarUsuariosPage()
    {
        InitializeComponent();
        _apiService = new ApiService("http://localhost:5142");
        
        if (!string.IsNullOrEmpty(Settings.Token))
        {
            _apiService.Token = Settings.Token;
        }

        FiltroPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarUsuarios();
    }

    private async Task CarregarUsuarios()
    {
        try
        {
            var usuarios = await _apiService.GetUsuariosAsync();
            
            if (usuarios != null)
            {
                _todosUsuarios = usuarios.Select(u => new UsuarioViewModel
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    Role = u.Role,
                    DataCriacao = u.DataCriacao,
                    DataCriacaoFormatada = u.DataCriacao.ToString("dd/MM/yyyy"),
                    RoleColor = GetRoleColor(u.Role),
                    RoleEmoji = GetRoleEmoji(u.Role)
                }).ToList();

                AplicarFiltro();
            }
        }
        catch (Exception ex)
        {
            await CustomAlertService.ShowErrorAsync($"Erro ao carregar usuarios: {ex.Message}");
        }
    }

    private void AplicarFiltro()
    {
        var filtro = FiltroPicker.SelectedItem?.ToString();
        
        if (filtro == "Todos" || string.IsNullOrEmpty(filtro))
        {
            UsuariosCollectionView.ItemsSource = _todosUsuarios;
        }
        else
        {
            UsuariosCollectionView.ItemsSource = _todosUsuarios.Where(u => u.Role == filtro).ToList();
        }
    }

    private string GetRoleColor(string role) => role switch
    {
        "Admin" or "Gerente" => "#f44336",
        "Tecnico" => "#2196F3",
        _ => "#757575"
    };

    private string GetRoleEmoji(string role) => role switch
    {
        "Admin" or "Gerente" => "",
        "Tecnico" => "",
        _ => ""
    };

    private void FiltroRole_Changed(object sender, EventArgs e)
    {
        AplicarFiltro();
    }

    private async void NovoUsuario_Clicked(object sender, EventArgs e)
    {
        var nome = await DisplayPromptAsync("Novo Usuario", "Nome:");
        if (string.IsNullOrWhiteSpace(nome)) return;

        var email = await DisplayPromptAsync("Novo Usuario", "Email:");
        if (string.IsNullOrWhiteSpace(email)) return;

        var senha = await DisplayPromptAsync("Novo Usuario", "Senha:");
        if (string.IsNullOrWhiteSpace(senha)) return;

        var role = await DisplayActionSheet("Selecione a Funcao", "Cancelar", null, "Usuario", "Tecnico", "Gerente", "Admin");
        if (role == "Cancelar" || string.IsNullOrEmpty(role)) return;

        var dto = new CriarEditarUsuarioDto
        {
            Nome = nome,
            Email = email,
            Senha = senha,
            Role = role
        };

        var sucesso = await _apiService.CriarUsuarioAsync(dto);
        
        if (sucesso)
        {
            await CustomAlertService.ShowSuccessAsync("Usuario criado com sucesso!");
            await CarregarUsuarios();
        }
        else
        {
            await CustomAlertService.ShowErrorAsync("Erro ao criar usuario.");
        }
    }

    private async void EditarUsuario_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            var nome = await DisplayPromptAsync("Editar Usuario", "Nome:", initialValue: usuario.Nome);
            if (string.IsNullOrWhiteSpace(nome)) return;

            var email = await DisplayPromptAsync("Editar Usuario", "Email:", initialValue: usuario.Email);
            if (string.IsNullOrWhiteSpace(email)) return;

            var alterarSenha = await CustomAlertService.ShowQuestionAsync("Deseja alterar a senha?", "Senha");
            string senha = null;
            if (alterarSenha)
            {
                senha = await DisplayPromptAsync("Editar Usuario", "Nova Senha:");
            }

            var role = await DisplayActionSheet("Selecione a Funcao", "Cancelar", null, "Usuario", "Tecnico", "Gerente", "Admin");
            if (role == "Cancelar" || string.IsNullOrEmpty(role)) return;

            var dto = new CriarEditarUsuarioDto
            {
                Id = usuario.Id,
                Nome = nome,
                Email = email,
                Senha = senha,
                Role = role
            };

            var sucesso = await _apiService.AtualizarUsuarioAsync(usuario.Id, dto);
            
            if (sucesso)
            {
                await CustomAlertService.ShowSuccessAsync("Usuario atualizado com sucesso!");
                await CarregarUsuarios();
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Erro ao atualizar usuario.");
            }
        }
    }

    private async void ExcluirUsuario_Clicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            var confirmar = await CustomAlertService.ShowQuestionAsync($"Deseja realmente excluir {usuario.Nome}?");
            
            if (confirmar)
            {
                var sucesso = await _apiService.DeletarUsuarioAsync(usuario.Id);
                
                if (sucesso)
                {
                    await CustomAlertService.ShowSuccessAsync("Usuario excluido com sucesso!");
                    await CarregarUsuarios();
                }
                else
                {
                    await CustomAlertService.ShowErrorAsync("Erro ao excluir usuario.");
                }
            }
        }
    }

    private async void VoltarDashboard_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnVoltarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnNovoUsuarioClicked(object sender, EventArgs e)
    {
        NovoUsuario_Clicked(sender, e);
    }

    private async void OnEditarClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            var nome = await DisplayPromptAsync("Editar Usuario", "Nome:", initialValue: usuario.Nome);
            if (string.IsNullOrWhiteSpace(nome)) return;

            var email = await DisplayPromptAsync("Editar Usuario", "Email:", initialValue: usuario.Email);
            if (string.IsNullOrWhiteSpace(email)) return;

            var senha = await DisplayPromptAsync("Editar Usuario", "Nova Senha (deixe vazio para manter):");

            var role = await DisplayActionSheet("Selecione a Funcao", "Cancelar", null, "Usuario", "Tecnico", "Gerente", "Admin");
            if (role == "Cancelar" || string.IsNullOrEmpty(role)) return;

            var dto = new CriarEditarUsuarioDto
            {
                Nome = nome,
                Email = email,
                Senha = string.IsNullOrWhiteSpace(senha) ? null : senha,
                Role = role
            };

            var sucesso = await _apiService.AtualizarUsuarioAsync(usuario.Id, dto);
            
            if (sucesso)
            {
                await CustomAlertService.ShowSuccessAsync("Usuario atualizado com sucesso!");
                await CarregarUsuarios();
            }
            else
            {
                await CustomAlertService.ShowErrorAsync("Erro ao atualizar usuario.");
            }
        }
    }

    private async void OnExcluirClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is UsuarioViewModel usuario)
        {
            var confirma = await CustomAlertService.ShowQuestionAsync($"Deseja realmente excluir o usuario {usuario.Nome}?");
            
            if (confirma)
            {
                var sucesso = await _apiService.DeletarUsuarioAsync(usuario.Id);
                
                if (sucesso)
                {
                    await CustomAlertService.ShowSuccessAsync("Usuario excluido com sucesso!");
                    await CarregarUsuarios();
                }
                else
                {
                    await CustomAlertService.ShowErrorAsync("Erro ao excluir usuario.");
                }
            }
        }
    }

    private void OnFiltroChanged(object sender, EventArgs e)
    {
        AplicarFiltro();
    }

    public class UsuarioViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime DataCriacao { get; set; }
        public string DataCriacaoFormatada { get; set; }
        public string RoleColor { get; set; }
        public string RoleEmoji { get; set; }
    }
}
