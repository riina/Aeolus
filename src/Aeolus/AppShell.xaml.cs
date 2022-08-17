namespace Aeolus;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        Routing.RegisterRoute("failed", typeof(ProjectLoadFailedPage));
	}
}
