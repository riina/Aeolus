namespace Aeolus;

public partial class MainPage : ContentPage
{
    private readonly IFolderPicker _folderPicker;

    int count = 0;

    public MainPage(IFolderPicker folderPicker)
    {
        InitializeComponent();
        _folderPicker = folderPicker;
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }

    private async void OnOpenFolderClicked(object sender, EventArgs e)
    {
        var picked = await _folderPicker.PickFolderAsync();
        SelectionButton.Text = $"Picked [{picked ?? "null"}]";
    }
}

