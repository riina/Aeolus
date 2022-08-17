using Aeolus.ModelProxies;

namespace Aeolus;

public partial class ProjectDirectoryProjects : ContentPage
{
    public ProjectDirectoryProjects()
    {
        InitializeComponent();
        var app = App.Me!;
        projectList.ItemsSource = app.ProjectDirectoryProjects;
        app.OnProjectDirectoryProjectsUpdated += App_OnProjectDirectoryProjectsUpdated;
    }

    private void App_OnProjectDirectoryProjectsUpdated(List<ProjectDirectoryProject> projects)
    {
        projectList.ItemsSource = projects;
    }

    private async void RefreshBtn_Clicked(object sender, EventArgs e)
    {
        var app = App.Me!;
        await app.UpdateProjectDirectoryProjectsAsync();
    }
}
