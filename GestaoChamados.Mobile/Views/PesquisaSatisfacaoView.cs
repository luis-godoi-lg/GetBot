using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using GestaoChamados.Shared.Services;
using GestaoChamados.Shared.DTOs;
using System;
using System.Threading.Tasks;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views
{
    public class PesquisaSatisfacaoView : ContentPage
    {
        private readonly ApiService _apiService;
        private readonly Chamado _chamado;
        private int? _avaliacaoSelecionada;
        private readonly Label[] _emojiLabels;
        private Label _avaliacaoTextoLabel;
        private Button _finalizarButton;
        private Entry _comentarioEntry;

        public PesquisaSatisfacaoView(Chamado chamado)
        {
            _apiService = new ApiService(Settings.ApiBaseUrl) { Token = Settings.Token };
            _chamado = chamado;
            _emojiLabels = new Label[5];

            Shell.SetNavBarIsVisible(this, false);
            BackgroundColor = Color.FromArgb("#F5F5F5");

            var mainLayout = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                }
            };

            // Header azul
            var header = new Border
            {
                BackgroundColor = Color.FromArgb("#0066CC"),
                Padding = new Thickness(20, 15)
            };

            var headerStack = new VerticalStackLayout
            {
                Spacing = 5
            };

            var titleLabel = new Label
            {
                Text = "📊 Pesquisa de Satisfação",
                TextColor = Colors.White,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            };

            var subtitleLabel = new Label
            {
                Text = $"Chamado #{_chamado.Id:D7}",
                TextColor = Color.FromArgb("#E3F2FD"),
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center
            };

            headerStack.Add(titleLabel);
            headerStack.Add(subtitleLabel);
            header.Content = headerStack;

            mainLayout.Add(header, 0, 0);

            // Conteúdo scrollável
            var scrollView = new ScrollView
            {
                Content = CriarConteudo()
            };
            mainLayout.Add(scrollView, 0, 1);

            Content = mainLayout;
        }

        private VerticalStackLayout CriarConteudo()
        {
            var layout = new VerticalStackLayout
            {
                Spacing = 20,
                Padding = new Thickness(20)
            };

            // Card principal
            var card = new Frame
            {
                BackgroundColor = Colors.White,
                CornerRadius = 16,
                Padding = new Thickness(15),
                HasShadow = true,
                HorizontalOptions = LayoutOptions.Fill
            };

            var cardLayout = new VerticalStackLayout
            {
                Spacing = 15
            };

            // Ícone de sucesso
            var iconLabel = new Label
            {
                Text = "✓",
                FontSize = 50,
                TextColor = Color.FromArgb("#4CAF50"),
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            };

            // Título
            var titleLabel = new Label
            {
                Text = "Atendimento Finalizado!",
                FontSize = 22,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#212121"),
                HorizontalTextAlignment = TextAlignment.Center
            };

            // Subtítulo
            var subtitleLabel = new Label
            {
                Text = "Como você avalia nosso atendimento?",
                FontSize = 15,
                TextColor = Color.FromArgb("#757575"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Grade de emojis - Grid responsivo com proporções iguais
            var emojiGrid = new Grid
            {
                ColumnSpacing = 5,
                Margin = new Thickness(0, 10),
                HorizontalOptions = LayoutOptions.Fill
            };

            // Criar 5 colunas com proporção igual (1*)
            for (int i = 0; i < 5; i++)
            {
                emojiGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            var emojis = new[] { "😞", "😕", "😐", "😊", "😍" };
            var labels = new[] { "Muito\nRuim", "Ruim", "Regular", "Bom", "Excelente" };

            for (int i = 0; i < 5; i++)
            {
                var emojiFrame = CriarEmojiButton(emojis[i], labels[i], i + 1);
                emojiGrid.Add(emojiFrame, i, 0);
            }

            // Label de avaliação selecionada
            _avaliacaoTextoLabel = new Label
            {
                Text = "Selecione uma avaliação",
                FontSize = 13,
                TextColor = Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            // Separador
            var separator = new BoxView
            {
                HeightRequest = 1,
                BackgroundColor = Color.FromArgb("#E0E0E0"),
                Margin = new Thickness(0, 10)
            };

            // Campo de comentário
            var comentarioLabel = new Label
            {
                Text = "Comentário (opcional):",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#424242")
            };

            _comentarioEntry = new Entry
            {
                Placeholder = "Deixe seu comentário sobre o atendimento...",
                HeightRequest = 80,
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                TextColor = Color.FromArgb("#212121")
            };

            // Botão finalizar
            _finalizarButton = new Button
            {
                Text = "Enviar Avaliação",
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                CornerRadius = 8,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 50,
                Margin = new Thickness(0, 10, 0, 0),
                IsEnabled = false
            };
            _finalizarButton.Clicked += OnFinalizarClicked;

            cardLayout.Add(iconLabel);
            cardLayout.Add(titleLabel);
            cardLayout.Add(subtitleLabel);
            cardLayout.Add(emojiGrid);
            cardLayout.Add(_avaliacaoTextoLabel);
            cardLayout.Add(separator);
            cardLayout.Add(comentarioLabel);
            cardLayout.Add(_comentarioEntry);
            cardLayout.Add(_finalizarButton);

            card.Content = cardLayout;
            layout.Add(card);

            return layout;
        }

        private Frame CriarEmojiButton(string emoji, string label, int valor)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                BorderColor = Color.FromArgb("#E0E0E0"),
                CornerRadius = 12,
                Padding = new Thickness(5, 8),
                HasShadow = false,
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = 75,
                Margin = new Thickness(2, 0)
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnEmojiTapped(valor);
            frame.GestureRecognizers.Add(tapGesture);

            var stackLayout = new VerticalStackLayout
            {
                Spacing = 3,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var emojiLabel = new Label
            {
                Text = emoji,
                FontSize = 30,
                HorizontalOptions = LayoutOptions.Center
            };

            var textLabel = new Label
            {
                Text = label,
                FontSize = 9,
                TextColor = Color.FromArgb("#757575"),
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap,
                MaxLines = 2
            };

            _emojiLabels[valor - 1] = emojiLabel;

            stackLayout.Add(emojiLabel);
            stackLayout.Add(textLabel);

            frame.Content = stackLayout;
            return frame;
        }

        private void OnEmojiTapped(int valor)
        {
            _avaliacaoSelecionada = valor;

            // Reset todos os emojis
            for (int i = 0; i < 5; i++)
            {
                _emojiLabels[i].Opacity = 0.3;
            }

            // Destacar o selecionado
            _emojiLabels[valor - 1].Opacity = 1.0;

            // Atualizar texto
            var textos = new[] { "Muito Ruim", "Ruim", "Regular", "Bom", "Excelente" };
            _avaliacaoTextoLabel.Text = $"Avaliação: {textos[valor - 1]}";
            _avaliacaoTextoLabel.TextColor = Color.FromArgb("#4CAF50");

            // Habilitar botão
            _finalizarButton.IsEnabled = true;
        }

        private async void OnFinalizarClicked(object? sender, EventArgs e)
        {
            if (!_avaliacaoSelecionada.HasValue)
            {
                await CustomAlertService.ShowWarningAsync("Por favor, selecione uma avaliação");
                return;
            }

            try
            {
                _finalizarButton.IsEnabled = false;
                _finalizarButton.Text = "Enviando...";

                var avaliacao = new AvaliacaoChamado
                {
                    ChamadoId = _chamado.Id,
                    Nota = _avaliacaoSelecionada.Value,
                    Comentario = _comentarioEntry.Text,
                    DataAvaliacao = DateTime.Now
                };

                var sucesso = await _apiService.AvaliarChamadoAsync(avaliacao);

                if (sucesso)
                {
                    await CustomAlertService.ShowSuccessAsync("Sua avaliação foi registrada com sucesso!", "Obrigado!");
                    
                    // Voltar para a tela de chamados
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await CustomAlertService.ShowErrorAsync("Não foi possível registrar a avaliação");
                    _finalizarButton.IsEnabled = true;
                    _finalizarButton.Text = "Enviar Avaliação";
                }
            }
            catch (Exception ex)
            {
                await CustomAlertService.ShowErrorAsync($"Erro ao enviar avaliação: {ex.Message}");
                _finalizarButton.IsEnabled = true;
                _finalizarButton.Text = "Enviar Avaliação";
            }
        }
    }
}
