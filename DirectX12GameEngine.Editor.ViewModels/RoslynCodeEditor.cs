using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Nito.AsyncEx;
using Windows.Storage;

namespace DirectX12GameEngine.Editor.ViewModels
{
    public interface ICodeEditor
    {
        event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

        Task ApplyChangesAsync(TextChange change, Encoding? encoding);

        Task<CompletionList?> GetCompletionListAsync(int position);

        ImmutableArray<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, string filterText);

        Task<CompletionChange?> GetCompletionChangeAsync(CompletionItem item);

        Task<SyntaxNode?> GetSyntaxNodeAsync(TextSpan span);

        Task<SemanticModel?> GetSemanticModelAsync();

        Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync();

        Task<IEnumerable<ClassifiedSpan>> GetChangedClassifiedSpansAsync();

        Task<IEnumerable<ClassifiedSpan>> GetClassifiedSpansAsync();
    }

    public class RoslynCodeEditor : ICodeEditor
    {
        private readonly SemaphoreSlim changeLock = new SemaphoreSlim(1, 1);
        private readonly Stack<Document> documentStack = new Stack<Document>();
        private readonly IStorageFile file;

        public RoslynCodeEditor(IStorageFile file, ISolutionLoader solutionLoader)
        {
            this.file = file;

            SolutionLoader = solutionLoader;
            SolutionLoader.Workspace.WorkspaceChanged += OnWorkspaceChanged;

            Solution solution = SolutionLoader.Workspace.CurrentSolution;
            Document? document = solution.GetDocument(solution.GetDocumentIdsWithFilePath(GetSolutionDocumentFilePath(file.Path)).FirstOrDefault());

            if (document != null)
            {
                documentStack.Push(document);
            }
        }

        public event EventHandler<DocumentChangedEventArgs>? DocumentChanged;

        public ISolutionLoader SolutionLoader { get; }

        private Document? CurrentDocument => documentStack.Count > 0 ? documentStack.Peek() : null;

        public async Task ApplyChangesAsync(TextChange change, Encoding? encoding)
        {
            if (!(change.Span.IsEmpty && string.IsNullOrEmpty(change.NewText)))
            {
                using (await changeLock.LockAsync())
                {
                    if (CurrentDocument != null)
                    {
                        Document document = CurrentDocument;

                        SourceText text = await document.GetTextAsync();
                        SourceText newText = text.WithChanges(change);

                        if (encoding != null && newText.Encoding != encoding)
                        {
                            newText = SourceText.From(newText.ToString(), encoding);
                        }

                        Document newDocument = document.WithText(newText);

                        if (SolutionLoader.Workspace.TryApplyChanges(newDocument.Project.Solution))
                        {
                            Solution solution = SolutionLoader.Workspace.CurrentSolution;

                            DocumentId? id = solution.GetDocumentIdsWithFilePath(GetSolutionDocumentFilePath(file.Path)).FirstOrDefault();
                            documentStack.Push(solution.GetDocument(id)!);

                            DocumentChanged?.Invoke(SolutionLoader.Workspace, new DocumentChangedEventArgs(true, null, null));
                        }
                    }
                }
            }
        }

        private async void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            using (await changeLock.LockAsync())
            {
                DocumentId? id = e.NewSolution.GetDocumentIdsWithFilePath(GetSolutionDocumentFilePath(file.Path)).FirstOrDefault();
                Document? document = e.NewSolution.GetDocument(id);

                if (document != null)
                {
                    bool isClassificationChanged = e.DocumentId != id || e.Kind != WorkspaceChangeKind.DocumentChanged;
                    bool isTextChanged = e.DocumentId != id && e.Kind != WorkspaceChangeKind.DocumentChanged;

                    if (isClassificationChanged || isTextChanged)
                    {
                        documentStack.Push(document);

                        string? newText = null;
                        Encoding? newEncoding = null;

                        if (isTextChanged)
                        {
                            SourceText sourceText = await document.GetTextAsync();
                            newText = sourceText.ToString();
                            newEncoding = sourceText.Encoding;
                        }

                        DocumentChanged?.Invoke(sender, new DocumentChangedEventArgs(isClassificationChanged, newText, newEncoding));
                    }
                }
            }
        }

        public Task<CompletionList?> GetCompletionListAsync(int position)
        {
            if (CurrentDocument != null)
            {
                Document document = CurrentDocument;
                CompletionService completionService = CompletionService.GetService(document);

                return completionService.GetCompletionsAsync(document, position);
            }

            return Task.FromResult<CompletionList?>(null);
        }

        public ImmutableArray<CompletionItem> FilterCompletionItems(ImmutableArray<CompletionItem> items, string filterText)
        {
            if (CurrentDocument != null)
            {
                CompletionService completionService = CompletionService.GetService(CurrentDocument);
                return completionService.FilterItems(CurrentDocument, items, filterText);
            }

            return ImmutableArray<CompletionItem>.Empty;
        }

        public Task<CompletionChange?> GetCompletionChangeAsync(CompletionItem item)
        {
            if (CurrentDocument != null)
            {
                CompletionService completionService = CompletionService.GetService(CurrentDocument);
                return completionService.GetChangeAsync(CurrentDocument, item);
            }

            return Task.FromResult<CompletionChange?>(null);
        }

        public async Task<SyntaxNode?> GetSyntaxNodeAsync(TextSpan span)
        {
            if (CurrentDocument != null)
            {
                Document document = CurrentDocument;
                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();

                if (syntaxTree != null)
                {
                    SyntaxNode root = await syntaxTree.GetRootAsync();
                    SyntaxNode node = root.FindNode(span);

                    return node;
                }
            }

            return null;
        }

        public async Task<SemanticModel?> GetSemanticModelAsync()
        {
            if (CurrentDocument != null)
            {
                Document document = CurrentDocument;
                SemanticModel? semanticModel = await document.GetSemanticModelAsync();

                return semanticModel;
            }

            return null;
        }

        public async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync()
        {
            if (CurrentDocument != null)
            {
                Document document = CurrentDocument;
                SyntaxTree? syntaxTree = await document.GetSyntaxTreeAsync();

                if (syntaxTree != null)
                {
                    return syntaxTree.GetDiagnostics();
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
            if (CurrentDocument != null)
            {
                Document document = CurrentDocument;
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

                if (span.TextSpan.Start > textChange.Span.End && span.TextSpan.Start > offset)
                {
                    offsettedSpan = new ClassifiedSpan(span.ClassificationType, new TextSpan(span.TextSpan.Start - offset, span.TextSpan.Length));
                }

                if (!oldClassifiedSpans.Contains(offsettedSpan))
                {
                    yield return span;
                }
            }
        }

        private string? GetSolutionDocumentFilePath(string documentFilePath)
        {
            if (SolutionLoader.TemporarySolutionFolder is null) return null;

            string relativeDocumentFilePath = StorageExtensions.GetRelativePath(SolutionLoader.RootFolder!.Path, documentFilePath);
            return Path.Combine(SolutionLoader.TemporarySolutionFolder.Path, relativeDocumentFilePath);
        }
    }
}
