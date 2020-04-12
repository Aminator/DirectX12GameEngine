using DirectX12GameEngine.Mvvm;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class FindAndReplaceViewModel : ViewModelBase
    {
        private bool isVisible;
        private bool isReplaceVisible;
        private string? searchTerm;
        private string? replacementTerm;
        private EditorFindOptions findOptions;
        private bool useRegex;

        public bool IsVisible
        {
            get => isVisible;
            set
            {
                if (Set(ref isVisible, value))
                {
                    if (!isVisible)
                    {
                        IsReplaceVisible = false;
                        SearchTerm = null;
                        ReplacementTerm = null;
                    }
                }
            }
        }

        public bool IsReplaceVisible
        {
            get => isReplaceVisible;
            set => Set(ref isReplaceVisible, value);
        }

        public string? SearchTerm
        {
            get => searchTerm;
            set => Set(ref searchTerm, value);
        }

        public string? ReplacementTerm
        {
            get => replacementTerm;
            set => Set(ref replacementTerm, value);
        }

        public EditorFindOptions FindOptions
        {
            get => findOptions;
            set => Set(ref findOptions, value);
        }

        public bool MatchCase
        {
            get => findOptions.HasFlag(EditorFindOptions.Case);
            set => Set(ref findOptions, value ? findOptions | EditorFindOptions.Case : findOptions & ~EditorFindOptions.Case);
        }

        public bool MatchWord
        {
            get => findOptions.HasFlag(EditorFindOptions.Word);
            set => Set(ref findOptions, value ? findOptions | EditorFindOptions.Word : findOptions & ~EditorFindOptions.Word);
        }

        public bool UseRegex
        {
            get => useRegex;
            set => Set(ref useRegex, value);
        }
    }

    public enum EditorFindOptions
    {
        None = 0,
        Word = 2,
        Case = 4
    }
}
