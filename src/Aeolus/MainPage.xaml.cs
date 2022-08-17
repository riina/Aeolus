namespace Aeolus;

public partial class MainPage : ContentPage
{
    private readonly IFolderPicker _folderPicker;

    public MainPage(IFolderPicker folderPicker)
    {
        InitializeComponent();
        _folderPicker = folderPicker;
        var app = App.Me!;
        projectList.ItemsSource = app.CL.GetProjectDirectoryProjects();
    }

    private async void OnOpenFolderClicked(object sender, EventArgs e)
    {
        var picked = await _folderPicker.PickFolderAsync();
        if (picked != null)
        {
            var app = App.Me!;
            var result = await app.CL.AddDirectoryAsync(picked);
            if (result.Success) await app.CL.UpdateDirectoryAsync(result.Model);
            projectList.ItemsSource = app.CL.GetProjectDirectoryProjects();
        }
    }

}

