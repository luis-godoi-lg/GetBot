using GestaoChamados.Mobile.Views;

namespace GestaoChamados.Mobile;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Registrar rotas para navegação
		Routing.RegisterRoute(nameof(NovoChamadoPage), typeof(NovoChamadoPage));
		Routing.RegisterRoute(nameof(DetalhesChamadoPage), typeof(DetalhesChamadoPage));
		Routing.RegisterRoute(nameof(FilaAtendimentoPage), typeof(FilaAtendimentoPage));
		Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
		Routing.RegisterRoute(nameof(ChamadosPage), typeof(ChamadosPage));
		Routing.RegisterRoute(nameof(DashboardPage), typeof(DashboardPage));
		Routing.RegisterRoute(nameof(GerenteDashboardPage), typeof(GerenteDashboardPage));
		Routing.RegisterRoute(nameof(RelatoriosPage), typeof(RelatoriosPage));
		Routing.RegisterRoute(nameof(GerenciarUsuariosPage), typeof(GerenciarUsuariosPage));
	}
}
