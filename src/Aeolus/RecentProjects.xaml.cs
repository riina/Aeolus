namespace Aeolus;

public partial class RecentProjects : ContentPage
{
	public RecentProjects()
	{
		InitializeComponent();
        var app = App.Me!;
        app.UpdateRecentProjects();
    }

    private async void ClearBtn_Clicked(object sender, EventArgs e)
    {
        var app = App.Me!;
        await app.UpdateProjectDirectoryProjectsAsync();
    }
}
