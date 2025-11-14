using GestaoChamados.Mobile.ViewModels;
using System.Collections.Specialized;
using GestaoChamados.Shared.DTOs;
using Microsoft.Maui.Controls.Shapes;

namespace GestaoChamados.Mobile.Views;

public partial class DetalhesChamadoPage : ContentPage
{
    private DetalhesChamadoViewModel? _viewModel;

    public DetalhesChamadoPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
        {
            _viewModel.Mensagens.CollectionChanged -= Mensagens_CollectionChanged;
        }

        _viewModel = BindingContext as DetalhesChamadoViewModel;

        if (_viewModel != null)
        {
            _viewModel.Mensagens.CollectionChanged += Mensagens_CollectionChanged;
        }
    }

    private void Mensagens_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (ChatMessageDto message in e.NewItems)
            {
                AddMessageToUI(message);
            }

            // Scroll para o final
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await ChatScrollView.ScrollToAsync(0, ChatMessagesLayout.Height, true);
            });
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            ChatMessagesLayout.Children.Clear();
        }
    }

    private void AddMessageToUI(ChatMessageDto message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var messageBorder = new Border
            {
                BackgroundColor = message.IsBot ? Color.FromArgb("#E9ECEF") : Color.FromArgb("#17A2B8"),
                Padding = new Thickness(10, 8),
                Margin = message.IsBot ? new Thickness(0, 2, 40, 2) : new Thickness(40, 2, 0, 2),
                HorizontalOptions = message.IsBot ? LayoutOptions.Start : LayoutOptions.End,
                MaximumWidthRequest = 220,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle
                {
                    CornerRadius = message.IsBot 
                        ? new CornerRadius(12, 12, 12, 2) 
                        : new CornerRadius(12, 12, 2, 12)
                }
            };

            var layout = new VerticalStackLayout { Spacing = 3 };

            // Nome do remetente
            layout.Children.Add(new Label
            {
                Text = message.RemetenteNome,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = message.IsBot ? Color.FromArgb("#6B7280") : Colors.White,
                Opacity = 0.9
            });

            // Mensagem
            layout.Children.Add(new Label
            {
                Text = message.Mensagem,
                FontSize = 13,
                TextColor = message.IsBot ? Color.FromArgb("#374151") : Colors.White,
                LineBreakMode = LineBreakMode.WordWrap
            });

            // Hora
            layout.Children.Add(new Label
            {
                Text = message.DataEnvio.ToString("HH:mm"),
                FontSize = 9,
                TextColor = message.IsBot ? Color.FromArgb("#9CA3AF") : Colors.White,
                Opacity = 0.8,
                HorizontalOptions = LayoutOptions.End
            });

            messageBorder.Content = layout;
            ChatMessagesLayout.Children.Add(messageBorder);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_viewModel != null)
        {
            _viewModel.Mensagens.CollectionChanged -= Mensagens_CollectionChanged;
        }
    }
}
