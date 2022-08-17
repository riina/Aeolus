using Aeolus.ModelProxies;

namespace Aeolus;

public partial class ProjectDirectoryEntry : ContentView
{
    public ProjectDirectoryEntry()
    {
        InitializeComponent();
    }

    private async void RemoveBtn_Clicked(object sender, EventArgs e)
    {
        // TODO dialog
        if (BindingContext is ProjectDirectory pd)
            await App.Me!.RemoveProjectDirectoryAsync(pd.FullPath);
    }
}
