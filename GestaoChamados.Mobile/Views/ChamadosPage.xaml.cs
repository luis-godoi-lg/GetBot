using GestaoChamados.Mobile.ViewModels;

namespace GestaoChamados.Mobile.Views;

public partial class ChamadosPage : ContentPage
{
    public ChamadosPage()
    {
        InitializeComponent();
        BindingContext = new ChamadosViewModel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Sempre recarregar a lista ao aparecer
        if (BindingContext is ChamadosViewModel vm)
        {
            await Task.Delay(100); // Pequeno delay para garantir que a UI está pronta
            vm.RefreshCommand.Execute(null);
        }
    }
}
