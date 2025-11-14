using GestaoChamados.Mobile.ViewModels;

namespace GestaoChamados.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        BindingContext = new LoginViewModel();
    }
}
