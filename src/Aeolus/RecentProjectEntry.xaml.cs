using Aeolus.ModelProxies;

namespace Aeolus;

public partial class RecentProjectEntry : ContentView
{
    public RecentProjectEntry()
    {
        InitializeComponent();
    }

    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        if (BindingContext is RecentProject project)
            await App.Me!.LoadProjectAsync(project.FullPath);
    }
}
