using GestaoChamados.Mobile.Helpers;
using GestaoChamados.Mobile.Views;

namespace GestaoChamados.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// Nota: MainPage está obsoleto no .NET 9 MAUI
		// A inicialização é feita via CreateWindow (abaixo)
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var appShell = new AppShell();
		var window = new Window(appShell);

		// Navegar após a criação da window
		appShell.Dispatcher.Dispatch(async () =>
		{
			// SEGURANÇA: Sempre limpar sessão anterior e exigir novo login
			// Isso evita que o app abra automaticamente logado, o que é um risco de segurança
			Settings.ClearAll();
			await appShell.GoToAsync($"//{nameof(LoginPage)}");
		});

		return window;
	}
}