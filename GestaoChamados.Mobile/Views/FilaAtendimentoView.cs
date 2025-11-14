using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using GestaoChamados.Shared.Services;
using GestaoChamados.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Services;

namespace GestaoChamados.Mobile.Views
{
    public class FilaAtendimentoView : ContentPage
    {
        private readonly ApiService _apiService;
        private HubConnection? _hubConnection;
        private ScrollView _scrollView;
        private VerticalStackLayout _chamadosStack;
        private Label _loadingLabel;
        private Label _nenhumChamadoLabel;
        private System.Threading.Timer? _timer;

        public FilaAtendimentoView()
        {
            _apiService = new ApiService(Settings.ApiBaseUrl) { Token = Settings.Token };

            // Remove título da barra de navegação
            Shell.SetNavBarIsVisible(this, false);
            BackgroundColor = Color.FromArgb("#F5F6FA");

            // Layout principal com Grid
            var mainGrid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition { Height = GridLength.Auto }, // Header
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) } // Content
                }
            };

            // Header igual ao Dashboard
            var headerBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#0066CC"),
                Padding = new Thickness(20, 15)
            };

            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var backButton = new Button
            {
                Text = "← Voltar",
                TextColor = Colors.White,
                BackgroundColor = Color.FromArgb("#0052A3"),
                FontSize = 13,
                Padding = new Thickness(15, 8),
                CornerRadius = 4
            };
            backButton.Clicked += async (s, e) => await Navigation.PopAsync();

            var titleLabel = new Label
            {
                Text = "📋 Fila de Atendimento",
                TextColor = Colors.White,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            headerGrid.Add(backButton, 0, 0);
            headerGrid.Add(titleLabel, 1, 0);
            headerBorder.Content = headerGrid;
            mainGrid.Add(headerBorder, 0, 0);

            // Loading label
            _loadingLabel = new Label
            {
                Text = "Carregando fila...",
                FontSize = 16,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 30, 0, 0)
            };

            // Nenhum chamado label
            _nenhumChamadoLabel = new Label
            {
                Text = "Nenhum cliente na fila no momento",
                FontSize = 16,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 50, 0, 0),
                IsVisible = false
            };

            // Stack de chamados
            _chamadosStack = new VerticalStackLayout
            {
                Spacing = 15,
                Padding = new Thickness(15, 20)
            };

            // ScrollView
            _scrollView = new ScrollView
            {
                Content = _chamadosStack
            };

            // Content area
            var contentLayout = new VerticalStackLayout
            {
                Spacing = 0,
                Padding = 0
            };

            contentLayout.Add(_loadingLabel);
            contentLayout.Add(_nenhumChamadoLabel);
            contentLayout.Add(_scrollView);

            mainGrid.Add(contentLayout, 0, 1);

            Content = mainGrid;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarFila();
            IniciarAtualizacaoAutomatica();
            await ConectarSignalR();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer?.Dispose();
            _ = DesconectarSignalR();
        }

        private async Task CarregarFila()
        {
            try
            {
                _loadingLabel.IsVisible = true;
                _nenhumChamadoLabel.IsVisible = false;
                _chamadosStack.Clear();

                System.Diagnostics.Debug.WriteLine("[FilaAtendimento] Carregando fila...");
                var chamados = await _apiService.ObterChamadosEmFilaAsync();

                if (chamados != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] {chamados.Count} chamados encontrados");
                    foreach (var c in chamados)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Chamado #{c.Id}: {c.NomeCliente} - {c.Assunto} - Status: {c.Status}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[FilaAtendimento] Nenhum chamado retornado (null)");
                }

                _loadingLabel.IsVisible = false;

                if (chamados == null || !chamados.Any())
                {
                    _nenhumChamadoLabel.IsVisible = true;
                    return;
                }

                foreach (var chamado in chamados)
                {
                    var card = CriarCardChamado(chamado);
                    _chamadosStack.Add(card);
                }
            }
            catch (Exception ex)
            {
                _loadingLabel.IsVisible = false;
                System.Diagnostics.Debug.WriteLine($"[FilaAtendimento] ERRO: {ex}");
                await CustomAlertService.ShowErrorAsync($"Erro ao carregar fila: {ex.Message}");
            }
        }

        private Frame CriarCardChamado(Chamado chamado)
        {
            var card = new Frame
            {
                BackgroundColor = Colors.White,
                BorderColor = Color.FromArgb("#E0E0E0"),
                CornerRadius = 12,
                Padding = new Thickness(15),
                Margin = new Thickness(0),
                HasShadow = true
            };

            var cardLayout = new VerticalStackLayout { Spacing = 12 };

            // Header do card
            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var clienteLabel = new Label
            {
                Text = chamado.NomeCliente ?? "Cliente",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#212121")
            };

            var statusBadge = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFC107"),
                CornerRadius = 12,
                Padding = new Thickness(10, 5),
                HasShadow = false
            };

            var statusLabel = new Label
            {
                Text = "AGUARDANDO",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            statusBadge.Content = statusLabel;

            headerGrid.Add(clienteLabel, 0, 0);
            headerGrid.Add(statusBadge, 1, 0);

            // Assunto
            var assuntoStack = new HorizontalStackLayout { Spacing = 8 };
            var assuntoIcon = new Label
            {
                Text = "📋",
                FontSize = 16
            };
            var assuntoLabel = new Label
            {
                Text = chamado.Assunto ?? "Sem assunto",
                FontSize = 14,
                TextColor = Color.FromArgb("#424242"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2
            };
            assuntoStack.Add(assuntoIcon);
            assuntoStack.Add(assuntoLabel);

            // Tempo de espera
            var tempoEspera = DateTime.Now - chamado.DataAbertura;
            var tempoStack = new HorizontalStackLayout { Spacing = 8 };
            var tempoIcon = new Label
            {
                Text = "⏱️",
                FontSize = 16
            };
            var tempoLabel = new Label
            {
                Text = $"Aguardando há {FormatarTempo(tempoEspera)}",
                FontSize = 13,
                TextColor = tempoEspera.TotalMinutes > 10 ? Color.FromArgb("#F44336") : Color.FromArgb("#757575")
            };
            tempoStack.Add(tempoIcon);
            tempoStack.Add(tempoLabel);

            // Separador
            var separator = new BoxView
            {
                HeightRequest = 1,
                BackgroundColor = Color.FromArgb("#E0E0E0"),
                Margin = new Thickness(0, 5)
            };

            // Botão Atender
            var atenderButton = new Button
            {
                Text = "Atender Agora",
                BackgroundColor = Color.FromArgb("#4CAF50"),
                TextColor = Colors.White,
                CornerRadius = 8,
                FontAttributes = FontAttributes.Bold,
                HeightRequest = 45,
                Margin = new Thickness(0, 5, 0, 0)
            };

            atenderButton.Clicked += async (s, e) => await OnAtenderClicked(chamado);

            cardLayout.Add(headerGrid);
            cardLayout.Add(assuntoStack);
            cardLayout.Add(tempoStack);
            cardLayout.Add(separator);
            cardLayout.Add(atenderButton);

            card.Content = cardLayout;
            return card;
        }

        private async Task OnAtenderClicked(Chamado chamado)
        {
            try
            {
                var confirmacao = await CustomAlertService.ShowQuestionAsync(
                    $"Deseja iniciar atendimento com {chamado.NomeCliente}?",
                    "Confirmar Atendimento"
                );

                if (!confirmacao) return;

                // Assumir o chamado
                var userId = Preferences.Get("UserId", 0);
                var sucesso = await _apiService.AssumirChamadoAsync(chamado.Id, userId);

                if (sucesso)
                {
                    await CustomAlertService.ShowSuccessAsync("Atendimento iniciado!");
                    
                    // Navegar para o chat (passando ID em vez do objeto)
                    var chatPage = new ChatAoVivoView(chamado.Id);
                    await Navigation.PushAsync(chatPage);
                }
                else
                {
                    await CustomAlertService.ShowErrorAsync("Não foi possível assumir o chamado");
                }
            }
            catch (Exception ex)
            {
                await CustomAlertService.ShowErrorAsync($"Erro ao atender: {ex.Message}");
            }
        }

        private void IniciarAtualizacaoAutomatica()
        {
            _timer = new System.Threading.Timer(
                async _ => await MainThread.InvokeOnMainThreadAsync(async () => await CarregarFila()),
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10)
            );
        }

        private async Task ConectarSignalR()
        {
            try
            {
                var apiUrl = _apiService.GetBaseUrl();
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl($"{apiUrl}/chatHub")
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<int>("NovoUsuarioNaFila", async (chamadoId) =>
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        await CarregarFila();
                    });
                });

                await _hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro SignalR: {ex.Message}");
            }
        }

        private async Task DesconectarSignalR()
        {
            try
            {
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                }
            }
            catch { }
        }

        private string FormatarTempo(TimeSpan tempo)
        {
            if (tempo.TotalMinutes < 1)
                return "menos de 1 minuto";
            if (tempo.TotalMinutes < 60)
                return $"{(int)tempo.TotalMinutes} minuto(s)";
            if (tempo.TotalHours < 24)
                return $"{(int)tempo.TotalHours} hora(s)";
            return $"{(int)tempo.TotalDays} dia(s)";
        }
    }
}
