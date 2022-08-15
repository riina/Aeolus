#nullable enable
namespace Aeolus;

public interface IFolderPicker
{
    Task<string?> PickFolderAsync();
}
