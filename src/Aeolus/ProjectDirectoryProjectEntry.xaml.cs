using Aeolus.ModelProxies;

namespace Aeolus;

public partial class ProjectDirectoryProjectEntry : ContentView
{
	public ProjectDirectoryProjectEntry()
	{
		InitializeComponent();
	}

    private async void TapGestureRecognizer_Tapped(object sender, EventArgs e)
    {
        if (BindingContext is ProjectDirectoryProject project)
            await App.Me!.LoadProjectAsync(project.FullPath);
    }
}
