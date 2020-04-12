using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface IEditor
    {
        bool SupportsAction(EditActions action);

        Task<bool> TryEditAsync(EditActions action);
    }

    public interface IFileEditor : IEditor
    {
        IStorageFile File { get; }
    }

    [Flags]
    public enum EditActions
    {
        None = 0,
        Save = 1,
        Undo = 2,
        Redo = 4,
        Cut = 8,
        Copy = 16,
        Paste = 32,
        Delete = 64,
        Close = 128,
        All = Save | Undo | Redo | Cut | Copy | Paste | Delete | Close
    }
}
