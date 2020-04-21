using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Windows.Storage;
using Windows.UI;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class CodeEditorViewModel : ObservableObject, IFileEditor, ICodeEditor
    {
        private string? currentText;
        private int tabSize = 4;
        private Encoding? encoding;
        private NewLineMode newLineMode = NewLineMode.Crlf;

        public CodeEditorViewModel(IStorageFile file) : this(file, null)
        {
        }

        public CodeEditorViewModel(IStorageFile file, ISolutionLoader? solutionLoader)
        {
            File = file;
            SolutionLoader = solutionLoader;

            if (SolutionLoader != null)
            {
                CodeEditor = new RoslynCodeEditor(file, SolutionLoader);
                CodeEditor.DocumentChanged += OnCodeEditorDocumentChanged;
            }
        }

        public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

        public ICodeEditor? CodeEditor { get; set; }

        public ISolutionLoader? SolutionLoader { get; }

        public IStorageFile File { get; }

        public ObservableCollection<Diagnostic> CurrentDiagnostics { get; } = new ObservableCollection<Diagnostic>();

        public FindAndReplaceViewModel FindAndReplace { get; } = new FindAndReplaceViewModel();

        public string? CurrentText
        {
            get => currentText;
            set => Set(ref currentText, value);
        }

        public int TabSize
        {
            get => tabSize;
            set => Set(ref tabSize, value);
        }

        public Encoding? Encoding
        {
            get => encoding;
            set => Set(ref encoding, value);
        }

        public NewLineMode NewLineMode
        {
            get => newLineMode;
            set => Set(ref newLineMode, value);
        }

        public bool SupportsAction(EditActions action) => action switch
        {
            EditActions.Save => true,
            EditActions.Close => true,
            _ => false
        };

        public async Task<bool> TryEditAsync(EditActions action)
        {
            switch (action)
            {
                case EditActions.Save:
                    await SaveAsync();
                    return true;
                case EditActions.Close:
                    if (CurrentText != null)
                    {
                        string text = await LoadTextAsync();

                        if (text != CurrentText)
                        {
                            await ApplyChangesAsync(new TextChange(new TextSpan(0, CurrentText.Length), text), Encoding);
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }

        public async Task SaveAsync()
        {
            if (CurrentText != null)
            {
                using Stream stream = await File.OpenStreamForWriteAsync();
                await SaveAsync(stream);
            }
        }

        private async Task SaveAsync(Stream stream)
        {
            stream.SetLength(0);

            using StreamWriter writer = new StreamWriter(stream, Encoding);
            await writer.WriteAsync(CurrentText);
        }

        public async Task<string> LoadTextAsync()
        {
            using Stream stream = await File.OpenStreamForReadAsync();
            using StreamReader reader = new StreamReader(stream, Encoding.Default);

            string text = await reader.ReadToEndAsync();
            Encoding = reader.CurrentEncoding;

            return text;
        }

        public Task ApplyChangesAsync(TextChange change, Encoding? encoding)
        {
            return CodeEditor != null ? CodeEditor.ApplyChangesAsync(change, Encoding) : Task.CompletedTask;
        }

        public Task<CompletionList?> GetCompletionListAsync(int position)
        {
            return CodeEditor != null ? CodeEditor.GetCompletionListAsync(position) : Task.FromResult<CompletionList?>(null);
        }

        public ImmutableArray<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, string filterText)
        {
            return CodeEditor != null ? CodeEditor.FilterCompletionItems(items, filterText) : ImmutableArray<CompletionItem>.Empty;
        }

        public Task<CompletionChange?> GetCompletionChangeAsync(CompletionItem item)
        {
            return CodeEditor != null ? CodeEditor.GetCompletionChangeAsync(item) : Task.FromResult<CompletionChange?>(null);
        }

        public Task<SyntaxNode?> GetSyntaxNodeAsync(TextSpan span)
        {
            return CodeEditor != null ? CodeEditor.GetSyntaxNodeAsync(span) : Task.FromResult<SyntaxNode?>(null);
        }

        public Task<SemanticModel?> GetSemanticModelAsync()
        {
            return CodeEditor != null ? CodeEditor.GetSemanticModelAsync() : Task.FromResult<SemanticModel?>(null);
        }

        public async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync()
        {
            var diagnostics = await (CodeEditor != null ? CodeEditor.GetDiagnosticsAsync() : Task.FromResult(Enumerable.Empty<Diagnostic>()));

            CurrentDiagnostics.Clear();

            foreach (Diagnostic diagnostic in diagnostics)
            {
                CurrentDiagnostics.Add(diagnostic);
            }

            return diagnostics;
        }

        public Task<IEnumerable<ClassifiedSpan>> GetChangedClassifiedSpansAsync()
        {
            return CodeEditor != null ? CodeEditor.GetChangedClassifiedSpansAsync() : Task.FromResult(Enumerable.Empty<ClassifiedSpan>());
        }

        public Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync()
        {
            return CodeEditor != null ? CodeEditor.GetClassifiedSpansAsync() : Task.FromResult(Enumerable.Empty<ClassifiedSpan>());
        }

        private void OnCodeEditorDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (e.NewEncoding != null)
            {
                Encoding = e.NewEncoding;
            }

            DocumentChanged?.Invoke(this, e);
        }
    }

    public class DocumentChangedEventArgs : EventArgs
    {
        public DocumentChangedEventArgs(bool isClassificationChanged, string? newText, Encoding? newEncoding)
        {
            IsClassificationChanged = isClassificationChanged;
            NewText = newText;
            NewEncoding = newEncoding;
        }

        public bool IsClassificationChanged { get; }

        public string? NewText { get; }

        public Encoding? NewEncoding { get; }
    }

    public enum NewLineMode
    {
        Cr,
        Lf,
        Crlf
    }

    public class CodeTextFormat
    {
        public CodeTextFormat()
        {
        }

        public CodeTextFormat(Color foregroundColor)
        {
            ForegroundColor = foregroundColor;
        }

        public Color ForegroundColor { get; set; }
    }
}
