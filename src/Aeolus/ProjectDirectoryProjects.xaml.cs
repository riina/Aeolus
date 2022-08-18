namespace Aeolus;

public partial class ProjectDirectoryProjects : ContentPage
{
    public ProjectDirectoryProjects()
    {
        InitializeComponent();
        var app = App.Me!;
        app.UpdateProjectDirectoryProjects();
    }

    private async void RefreshBtn_Clicked(object sender, EventArgs e)
    {
        await App.Me!.UpdateProjectDirectoryProjectsAsync();
    }

    private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        App.Me!.SetProjectDirectoryProjectSearch(e.NewTextValue);
    }
}
