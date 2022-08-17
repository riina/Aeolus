namespace Aeolus;

public partial class ProjectDirectoryProjects : ContentPage
{
    public ProjectDirectoryProjects()
    {
        InitializeComponent();
    }

    private async void RefreshBtn_Clicked(object sender, EventArgs e)
    {
        var app = App.Me!;
        await app.UpdateProjectDirectoryProjectsAsync();
    }
}
