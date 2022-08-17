namespace Aeolus;

public partial class ProjectDirectories : ContentPage
{
    private readonly IFolderPicker _folderPicker;

    public ProjectDirectories(IFolderPicker picker)
	{
		InitializeComponent();
        _folderPicker = picker;
        var app = App.Me!;
    }

    private async void AddBtn_Clicked(object sender, EventArgs e)
    {
        var picked = await _folderPicker.PickFolderAsync();
        if (picked != null)
        {
            var app = App.Me!;
            await app.AddProjectDirectoryAsync(picked);
        }

    }
}
