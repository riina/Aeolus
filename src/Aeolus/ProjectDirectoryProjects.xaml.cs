namespace Aeolus;

public partial class ProjectDirectoryProjects : ContentPage
{
    public ProjectDirectoryProjects()
    {
        InitializeComponent();
        var app = App.Me!;
        projectList.ItemsSource = app.ProjectDirectoryProjects;
    }

    private async void RefreshBtn_Clicked(object sender, EventArgs e)
    {
        var app = App.Me!;
        await app.CL.UpdateAllDirectoriesAsync();
        app.UpdateProjectDirectoryProjects();
    }
}
