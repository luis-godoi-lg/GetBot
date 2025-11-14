using System;
using System.Windows;
using System.Windows.Controls;
using GestaoChamados.Shared.DTOs;

namespace GestaoChamados.Desktop
{
    public partial class CriarEditarUsuarioDialog : Window
    {
        private readonly ListarUsuarioDto? _usuarioExistente;
        private readonly bool _isEdicao;

        public CriarEditarUsuarioDialog(ListarUsuarioDto? usuario = null)
        {
            InitializeComponent();
            _usuarioExistente = usuario;
            _isEdicao = usuario != null;

            if (_isEdicao && _usuarioExistente != null)
            {
                TituloTextBlock.Text = "✏️ Editar Usuário";
                SalvarButton.Content = "Atualizar";
                
                // Preencher campos
                NomeTextBox.Text = _usuarioExistente.Nome;
                EmailTextBox.Text = _usuarioExistente.Email;
                
                // Selecionar role
                foreach (ComboBoxItem item in RoleComboBox.Items)
                {
                    if (item.Content.ToString() == _usuarioExistente.Role)
                    {
                        item.IsSelected = true;
                        break;
                    }
                }
                
                // Senha opcional na edição
                SenhaLabel.Text = "Senha (opcional)";
                SenhaHint.Visibility = Visibility.Visible;
            }
        }

        private async void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Validações
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text))
            {
                MessageBox.Show("Nome é obrigatório.", "Validação", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NomeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Email é obrigatório.", "Validação", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                EmailTextBox.Focus();
                return;
            }

            if (!_isEdicao && string.IsNullOrWhiteSpace(SenhaPasswordBox.Password))
            {
                MessageBox.Show("Senha é obrigatória para novos usuários.", "Validação", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SenhaPasswordBox.Focus();
                return;
            }

            var role = (RoleComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Usuario";

            var dto = new CriarEditarUsuarioDto
            {
                Nome = NomeTextBox.Text.Trim(),
                Email = EmailTextBox.Text.Trim(),
                Senha = string.IsNullOrWhiteSpace(SenhaPasswordBox.Password) ? null : SenhaPasswordBox.Password,
                Role = role
            };

            bool sucesso;

            if (_isEdicao && _usuarioExistente != null)
            {
                dto.Id = _usuarioExistente.Id;
                sucesso = await App.ApiService.AtualizarUsuarioAsync(_usuarioExistente.Id, dto);
            }
            else
            {
                sucesso = await App.ApiService.CriarUsuarioAsync(dto);
            }

            if (sucesso)
            {
                MessageBox.Show(
                    _isEdicao ? "Usuário atualizado com sucesso!" : "Usuário criado com sucesso!",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    _isEdicao ? "Erro ao atualizar usuário." : "Erro ao criar usuário.",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
