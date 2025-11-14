using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Desktop
{
    public partial class GerenciarUsuariosWindow : Window
    {
        private List<ListarUsuarioDto> _todosUsuarios = new();

        public GerenciarUsuariosWindow()
        {
            try
            {
                InitializeComponent();
                
                // Garantir que o token está configurado no ApiService
                if (!string.IsNullOrEmpty(App.CurrentUserToken))
                {
                    App.ApiService.Token = App.CurrentUserToken;
                }
                
                Loaded += async (s, e) => await CarregarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar janela:\n\n{ex.Message}\n\nStack: {ex.StackTrace}", 
                    "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private async System.Threading.Tasks.Task CarregarUsuarios()
        {
            try
            {
                var usuarios = await App.ApiService.GetUsuariosAsync();
                
                if (usuarios != null)
                {
                    _todosUsuarios = usuarios;
                    AplicarFiltro();
                }
                else
                {
                    MessageBox.Show("Erro ao carregar usuários.", "Erro", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar usuários: {ex.Message}", "Erro", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltro()
        {
            if (FiltroRoleComboBox == null || UsuariosDataGrid == null)
                return;
                
            var filtroSelecionado = (FiltroRoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            
            if (filtroSelecionado == "Todos" || string.IsNullOrEmpty(filtroSelecionado))
            {
                UsuariosDataGrid.ItemsSource = _todosUsuarios;
            }
            else
            {
                UsuariosDataGrid.ItemsSource = _todosUsuarios.Where(u => u.Role == filtroSelecionado).ToList();
            }
        }

        private void FiltroRole_Changed(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltro();
        }

        private void NovoUsuario_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CriarEditarUsuarioDialog();
            if (dialog.ShowDialog() == true)
            {
                _ = CarregarUsuarios(); // Recarregar lista
            }
        }

        private void EditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.Tag as ListarUsuarioDto;
            
            if (usuario != null)
            {
                var dialog = new CriarEditarUsuarioDialog(usuario);
                if (dialog.ShowDialog() == true)
                {
                    _ = CarregarUsuarios(); // Recarregar lista
                }
            }
        }

        private async void ExcluirUsuario_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.Tag as ListarUsuarioDto;
            
            if (usuario != null)
            {
                var result = MessageBox.Show(
                    $"Tem certeza que deseja excluir o usuário '{usuario.Nome}'?\n\nEsta ação não pode ser desfeita.",
                    "Confirmar Exclusão",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var sucesso = await App.ApiService.DeletarUsuarioAsync(usuario.Id);
                    
                    if (sucesso)
                    {
                        MessageBox.Show("Usuário excluído com sucesso!", "Sucesso", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        await CarregarUsuarios();
                    }
                    else
                    {
                        MessageBox.Show("Erro ao excluir usuário.", "Erro", 
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void VoltarDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new GerenteDashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Deseja realmente sair?", "Confirmar Logout", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUserToken = null;
                App.CurrentUserEmail = null;
                App.CurrentUserRole = null;

                var loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
