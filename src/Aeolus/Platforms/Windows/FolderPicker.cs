#nullable enable
namespace Aeolus.Platforms.Windows;

public class FolderPicker : IFolderPicker
{
    public async Task<string?> PickFolderAsync()
    {
        var folderPicker = new global::Windows.Storage.Pickers.FolderPicker();
        folderPicker.FileTypeFilter.Add("*");
        IntPtr hwnd = ((MauiWinUIWindow)Application.Current!.Windows[0].Handler.PlatformView!).WindowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
        return (await folderPicker.PickSingleFolderAsync())?.Path;
    }
}
