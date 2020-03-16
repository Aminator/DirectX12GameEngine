using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectX12GameEngine.Mvvm;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Nito.AsyncEx;
using Windows.Storage;
using Windows.UI;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public class CodeEditorViewModel : ViewModelBase, IClosable
    {
        private readonly AsyncLock changeLock = new AsyncLock();

        private readonly Stack<Document> documentStack = new Stack<Document>();

        private int tabSize = 4;
        private Encoding? encoding;
        private NewLineMode newLineMode = NewLineMode.Crlf;
        private string? currentText;

        public CodeEditorViewModel(IStorageFile file, SolutionLoaderViewModel solutionLoader)
        {
            File = file;
            SolutionLoader = solutionLoader;
            SolutionLoader.Workspace.WorkspaceChanged += OnWorkspaceChanged;

            Solution solution = SolutionLoader.Workspace.CurrentSolution;
            Document? document = solution.GetDocument(GetDocumentId(solution));

            if (document != null)
            {
                documentStack.Push(document);
            }
        }

        private async void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            using (await changeLock.LockAsync())
            {
                DocumentId? id = GetDocumentId(e.NewSolution);
                Document? document = e.NewSolution.GetDocument(id);

                if (document != null)
                {
                    bool isClassificationChanged = e.DocumentId != id || e.Kind != WorkspaceChangeKind.DocumentChanged;
                    bool isTextChanged = e.DocumentId != id && e.Kind != WorkspaceChangeKind.DocumentChanged;

                    if (isClassificationChanged || isTextChanged)
                    {
                        documentStack.Push(document);

                        if (isTextChanged)
                        {
                            SourceText sourceText = await document.GetTextAsync();
                            Encoding = sourceText.Encoding;
                        }

                        DocumentChanged?.Invoke(sender, new DocumentChangedEventArgs(isClassificationChanged, isTextChanged));
                    }
                }
            }
        }

        public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

        public SolutionLoaderViewModel SolutionLoader { get; }

        public bool CanClose => false;

        public IStorageFile File { get; set; }

        public ObservableCollection<Diagnostic> CurrentDiagnostics { get; } = new ObservableCollection<Diagnostic>();

        public Document? CurrentDocument => documentStack.Count > 0 ? documentStack.Peek() : null;

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

        public async Task SaveAsync()
        {
            if (documentStack.Count == 0)
            {
                using Stream stream = await File.OpenStreamForWriteAsync();
                using StreamWriter writer = new StreamWriter(stream, Encoding);
                await writer.WriteAsync(CurrentText);
            }
        }

        public Task<bool> TryCloseAsync()
        {
            return Task.FromResult(true);
        }

        public async Task<string> LoadTextAsync()
        {
            if (documentStack.Count > 0)
            {
                Document document = documentStack.Peek();
                SourceText sourceText = await document.GetTextAsync();

                CurrentText = sourceText.ToString();
                Encoding = sourceText.Encoding;
            }
            else
            {
                using Stream stream = await File.OpenStreamForReadAsync();
                using StreamReader reader = new StreamReader(stream, Encoding.Default);

                CurrentText = await reader.ReadToEndAsync();
                Encoding = reader.CurrentEncoding;
            }

            return CurrentText;
        }

        public async Task ApplyChangesAsync(TextChange change)
        {
            if (!(change.Span.IsEmpty && string.IsNullOrEmpty(change.NewText)))
            {
                using (await changeLock.LockAsync())
                {
                    if (documentStack.Count > 0)
                    {
                        Document document = documentStack.Peek();

                        SourceText text = await document.GetTextAsync();
                        SourceText newText = text.WithChanges(change);

                        Document newDocument = document.WithText(newText);

                        if (SolutionLoader.Workspace.TryApplyChanges(newDocument.Project.Solution))
                        {
                            Solution solution = SolutionLoader.Workspace.CurrentSolution;

                            DocumentId? id = GetDocumentId(solution);
                            documentStack.Push(solution.GetDocument(id)!);

                            DocumentChanged?.Invoke(SolutionLoader.Workspace, new DocumentChangedEventArgs(true, false));
                        }
                    }
                    else
                    {
                        using Stream stream = await File.OpenStreamForWriteAsync();
                        using StreamWriter writer = new StreamWriter(stream, Encoding);
                        await writer.WriteAsync(CurrentText);
                    }
                }
            }
        }

        public async Task GetCompletionListAsync()
        {

            await Task.CompletedTask;
        }

        public async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(int position)
        {
            var diagnostics = await GetDiagnosticsAsync();

            return diagnostics.Where(d => d.Location.SourceSpan.Contains(position));
        }

        public async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync()
        {
            if (documentStack.Count > 0)
            {
                Document document = documentStack.Peek();

                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();

                if (syntaxTree != null)
                {
                    var diagnostics = syntaxTree.GetDiagnostics();

                    CurrentDiagnostics.Clear();

                    foreach (Diagnostic diagnostic in diagnostics)
                    {
                        CurrentDiagnostics.Add(diagnostic);
                    }

                    return diagnostics;
                }
            }

            return Enumerable.Empty<Diagnostic>();
        }

        public async Task<IEnumerable<ClassifiedSpan>> GetChangedClassifiedSpansAsync()
        {
            if (documentStack.Count >= 2)
            {
                Document[] documents = documentStack.ToArray();

                Document newDocument = documents[0];
                Document oldDocument = documents[1];

                SourceText newText = await newDocument.GetTextAsync();
                var newClassifiedSpans = await Classifier.GetClassifiedSpansAsync(newDocument, new TextSpan(0, newText.Length));

                SourceText oldText = await oldDocument.GetTextAsync();
                var oldClassifiedSpans = await Classifier.GetClassifiedSpansAsync(oldDocument, new TextSpan(0, oldText.Length));

                var textChanges = newText.GetChangeRanges(oldText);

                return GetChangedClassifiedSpans(textChanges.FirstOrDefault(), oldClassifiedSpans, newClassifiedSpans);
            }

            return await GetClassifiedSpansAsync();
        }

        public async Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync()
        {
            if (documentStack.Count > 0)
            {
                Document document = documentStack.Peek();
                SourceText text = await document.GetTextAsync();

                return await Classifier.GetClassifiedSpansAsync(document, new TextSpan(0, text.Length));
            }

            return Enumerable.Empty<ClassifiedSpan>();
        }

        private static IEnumerable<ClassifiedSpan> GetChangedClassifiedSpans(TextChangeRange textChange, IEnumerable<ClassifiedSpan> oldClassifiedSpans, IEnumerable<ClassifiedSpan> newClassifiedSpans)
        {
            int offset = textChange.NewLength - textChange.Span.Length;

            foreach (ClassifiedSpan span in newClassifiedSpans)
            {
                ClassifiedSpan offsettedSpan = span;

                if (span.TextSpan.Start > textChange.Span.End)
                {
                    offsettedSpan = new ClassifiedSpan(span.ClassificationType, new TextSpan(span.TextSpan.Start - offset, span.TextSpan.Length));
                }

                if (!oldClassifiedSpans.Contains(offsettedSpan))
                {
                    yield return span;
                }
            }
        }

        private DocumentId? GetDocumentId(Solution solution)
        {
            if (solution.FilePath is null) return null;

            string relativeDocumentFilePath = StorageExtensions.GetRelativePath(SolutionLoader.RootFolder!.Path, File.Path);
            string documentFilePath = Path.Combine(Path.GetDirectoryName(solution.FilePath), relativeDocumentFilePath);

            return solution.GetDocumentIdsWithFilePath(documentFilePath).FirstOrDefault();
        }
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

    public class DocumentChangedEventArgs : EventArgs
    {
        public DocumentChangedEventArgs(bool isClassificationChanged, bool isTextChanged)
        {
            IsClassificationChanged = isClassificationChanged;
            IsTextChanged = isTextChanged;
        }

        public bool IsClassificationChanged { get; }

        public bool IsTextChanged { get; }
    }

    public enum NewLineMode
    {
        Cr,
        Lf,
        Crlf
    }
}
