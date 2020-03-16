using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Nito.AsyncEx;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class CodeEditorView : UserControl
    {
        private readonly AsyncLock changeLock = new AsyncLock();
        private readonly AsyncLock classifyLock = new AsyncLock();

        private TextSpan previousSelection;
        private TextSpan currentSelection;

        public CodeEditorView()
        {
            InitializeComponent();

            CodeEditor.AddHandler(PointerMovedEvent, new PointerEventHandler(OnCodeEditorPointerMoved), true);
            CodeEditor.TextDocument.UndoLimit = 50;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public CodeEditorViewModel ViewModel => (CodeEditorViewModel)DataContext;

        public IEnumerable<Diagnostic> CurrentDiagnosticsAtPointerPosition { get; set; } = Enumerable.Empty<Diagnostic>();

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.DocumentChanged += OnViewModelDocumentChanged;

            await ViewModel.LoadTextAsync();

            CodeEditor.TextDocument.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string text);

            if (text != ViewModel.CurrentText)
            {
                CodeEditor.TextDocument.SetText(TextSetOptions.None, ViewModel.CurrentText);
            }

            await ReapplySyntaxHighlightingAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.DocumentChanged -= OnViewModelDocumentChanged;
        }

        private async void OnViewModelDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (e.IsTextChanged)
            {
                using (await changeLock.LockAsync())
                {
                    Document? document = ViewModel.CurrentDocument;

                    if (document != null)
                    {
                        SourceText sourceText = await document.GetTextAsync();
                        string newText = sourceText.ToString();

                        CodeEditor.TextDocument.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string text);

                        if (text != newText)
                        {
                            ViewModel.CurrentText = newText;
                            CodeEditor.TextDocument.SetText(TextSetOptions.None, newText);
                        }
                    }
                }
            }

            if (e.IsClassificationChanged)
            {
                if (e.IsTextChanged)
                {
                    await ReapplySyntaxHighlightingAsync();
                }
                else
                {
                    await ApplySyntaxHighlightingChangesAsync();
                }
            }
        }

        private async void ReapplySyntaxHighlighting()
        {
            if (ViewModel is null) return;

            await ReapplySyntaxHighlightingAsync();
        }

        private async Task ApplySyntaxHighlightingChangesAsync()
        {
            using (await classifyLock.LockAsync())
            {
                ApplyClassifiedSpans(await ViewModel.GetChangedClassifiedSpansAsync());
                ClearDiagnostics();
                ApplyDiagnostics(await ViewModel.GetDiagnosticsAsync());
            }
        }

        private async Task ReapplySyntaxHighlightingAsync()
        {
            using (await classifyLock.LockAsync())
            {
                ApplyClassifiedSpans(await ViewModel.GetClassifiedSpansAsync());
                ClearDiagnostics();
                ApplyDiagnostics(await ViewModel.GetDiagnosticsAsync());
            }
        }

        private void ApplyClassifiedSpans(IEnumerable<ClassifiedSpan> classifiedSpans)
        {
            var themeDictionary = (ResourceDictionary)Resources.ThemeDictionaries[CodeEditor.ActualTheme == ElementTheme.Default
                ? Application.Current.RequestedTheme.ToString()
                : CodeEditor.ActualTheme.ToString()];

            foreach (ClassifiedSpan classifiedSpan in classifiedSpans)
            {
                if (themeDictionary.TryGetValue(classifiedSpan.ClassificationType, out object value))
                {
                    CodeTextFormat format = (CodeTextFormat)value;

                    TextSpan span = GetAdjustedTextSpan(classifiedSpan.TextSpan, ViewModel.CurrentText!, ViewModel.NewLineMode);
                    ITextRange range = CodeEditor.Document.GetRange(span.Start, span.End);

                    range.CharacterFormat.ForegroundColor = format.ForegroundColor;
                }
            }
        }

        private void ClearDiagnostics()
        {
            CodeEditor.Document.GetText(TextGetOptions.AdjustCrlf, out string text);
            ITextRange textRange = CodeEditor.Document.GetRange(0, text.Length - 1);

            (textRange.CharacterFormat.Underline, textRange.CharacterFormat.BackgroundColor) = (UnderlineType.None, Colors.Transparent);
        }

        private void ApplyDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                TextSpan span = GetAdjustedTextSpan(diagnostic.Location.SourceSpan, ViewModel.CurrentText!, ViewModel.NewLineMode);
                ITextRange range = CodeEditor.Document.GetRange(span.Start, span.End);

                (range.CharacterFormat.Underline, range.CharacterFormat.BackgroundColor) = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Hidden => (UnderlineType.Dotted, Colors.Aqua),
                    DiagnosticSeverity.Info => (UnderlineType.Dotted, Colors.Gray),
                    DiagnosticSeverity.Warning => (UnderlineType.Wave, Colors.Yellow),
                    DiagnosticSeverity.Error => (UnderlineType.Wave, Colors.Red),
                    _ => (UnderlineType.Undefined, Colors.Transparent)
                };
            }
        }

        private void OnCodeEditorSelectionChanging(RichEditBox sender, RichEditBoxSelectionChangingEventArgs e)
        {
            previousSelection = currentSelection;
            currentSelection = new TextSpan(e.SelectionStart, e.SelectionLength);
        }

        private void OnCodeEditorSelectionChanged(object sender, RoutedEventArgs e)
        {
        }

        private async void OnCodeEditorTextChanging(RichEditBox sender, RichEditBoxTextChangingEventArgs e)
        {
            if (e.IsContentChanging && previousSelection != currentSelection)
            {
                using (await changeLock.LockAsync())
                {
                    CodeEditor.TextDocument.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string wholeText);

                    if (wholeText != ViewModel.CurrentText)
                    {
                        int start = Math.Min(previousSelection.Start, currentSelection.Start);

                        ITextRange range = CodeEditor.Document.GetRange(start, currentSelection.End);
                        range.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string text);

                        TextSpan span = GetAdjustedTextSpan(TextSpan.FromBounds(start, previousSelection.End), ViewModel.CurrentText, ViewModel.NewLineMode, true);
                        TextChange textChange = new TextChange(span, text);

                        ViewModel.CurrentText = wholeText;

                        await ViewModel.ApplyChangesAsync(textChange);
                    }
                }
            }
        }

        private void OnCodeEditorTextChanged(object sender, RoutedEventArgs e)
        {
        }

        private void OnCodeEditorCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
        {
        }

        private void OnCodeEditorKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab)
            {
                bool isShiftDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

                ITextSelection selection = CodeEditor.Document.Selection;

                ITextRange range = selection.GetClone();
                int selectionStart = range.StartPosition;
                range.StartOf(TextRangeUnit.Line, false);
                int lineStart = range.StartPosition;

                int charactersFromLineStart = selectionStart - lineStart;
                int charactersOffsetFromTabSize = charactersFromLineStart % ViewModel.TabSize;

                if (isShiftDown)
                {
                    int charactersToDelete = charactersOffsetFromTabSize == 0 ? ViewModel.TabSize : charactersOffsetFromTabSize;
                    ITextRange deletionRange = selection.GetClone();

                    if (deletionRange.StartPosition > lineStart)
                    {
                        deletionRange.MoveStart(TextRangeUnit.Character, -1);

                        int safeCharactersToDelete;

                        for (safeCharactersToDelete = 0; safeCharactersToDelete < charactersToDelete && deletionRange.StartPosition >= lineStart && char.IsWhiteSpace(deletionRange.Character); safeCharactersToDelete++)
                        {
                            deletionRange.MoveStart(TextRangeUnit.Character, -1);
                        }

                        CodeEditor.Document.GetRange(selection.StartPosition, selection.StartPosition).Delete(TextRangeUnit.Character, -safeCharactersToDelete);
                    }
                }
                else
                {
                    int charactersToAdd = ViewModel.TabSize - charactersOffsetFromTabSize;
                    string tabString = new string(' ', charactersToAdd);

                    selection.TypeText(tabString);
                }

                e.Handled = true;
            }
        }

        private async void OnCodeEditorPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint point = e.GetCurrentPoint(CodeEditor);
            ScrollViewer scrollViewer = CodeEditor.FindDescendant<ScrollViewer>();

            Point position = new Point(point.Position.X + scrollViewer.HorizontalOffset - 12, point.Position.Y + scrollViewer.VerticalOffset - 12);
            ITextRange range = CodeEditor.Document.GetRangeFromPoint(position, PointOptions.ClientCoordinates);

            int startPosition = GetAdjustedTextSpan(new TextSpan(range.StartPosition, 0), ViewModel.CurrentText, ViewModel.NewLineMode, true).Start;
            var diagnostics = await ViewModel.GetDiagnosticsAsync(startPosition);

            if (!CurrentDiagnosticsAtPointerPosition.SequenceEqual(diagnostics, DiagnosticsEqualityComparer.Default))
            {
                CurrentDiagnosticsAtPointerPosition = diagnostics;

                if (diagnostics.Count() > 0)
                {
                    range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Bottom, PointOptions.ClientCoordinates, out Point textRangePoint);
                    Point toolTipPosition = new Point(textRangePoint.X - scrollViewer.HorizontalOffset, textRangePoint.Y - scrollViewer.VerticalOffset);

                    CodeEditorToolTip.IsOpen = true;
                    CodeEditorToolTip.PlacementRect = new Rect(toolTipPosition, new Size(12, 12));

                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(diagnostics.First().GetMessage());

                    foreach (Diagnostic diagnostic in diagnostics.Skip(1))
                    {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                        stringBuilder.Append(diagnostic.GetMessage());
                    }

                    CodeEditorToolTip.Content = stringBuilder.ToString();
                }
                else
                {
                    CodeEditorToolTip.IsOpen = false;
                }
            }
        }

        private void OnCodeEditorToolTipOpened(object sender, RoutedEventArgs e)
        {
            if (CurrentDiagnosticsAtPointerPosition.Count() == 0)
            {
                CodeEditorToolTip.IsOpen = false;
            }
        }

        private void OnCodeEditorToolTipClosed(object sender, RoutedEventArgs e)
        {
            if (CurrentDiagnosticsAtPointerPosition.Count() > 0)
            {
                CodeEditorToolTip.IsOpen = true;
            }
        }

        private static TextSpan GetAdjustedTextSpan(TextSpan span, string? text, NewLineMode newLineMode, bool addOffset = false)
        {
            if (newLineMode != NewLineMode.Crlf || text is null)
            {
                return span;
            }

            if (addOffset)
            {
                text = text.Replace("\r\n", "\r");
            }

            int startOffset = text.Take(span.Start).Count(c => c == '\r');
            int endOffset = text.Take(span.End).Count(c => c == '\r');

            return TextSpan.FromBounds(span.Start + (addOffset ? +startOffset : -startOffset), span.End + (addOffset ? +endOffset : -endOffset));
        }

        private static TextGetOptions TextGetOptionsFromNewLineMode(NewLineMode newLineMode) => newLineMode switch
        {
            NewLineMode.Cr => TextGetOptions.AdjustCrlf,
            NewLineMode.Lf => TextGetOptions.AdjustCrlf | TextGetOptions.UseLf,
            NewLineMode.Crlf => TextGetOptions.AdjustCrlf | TextGetOptions.UseCrlf,
            _ => TextGetOptions.AdjustCrlf
        };
    }

    public static class StringHelper
    {
        public static string ToUpper(object value) => value.ToString().ToUpper();

        public static string ToLower(object value) => value.ToString().ToLower();
    }

    public static class EncodingHelper
    {
        public static Encoding GetEncoding(string name) => name switch
        {
            "UTF-8" => Encoding.Default,
            "UTF-8 BOM" => Encoding.UTF8,
            "UTF-16 LE" => Encoding.Unicode,
            "UTF-16 BE" => Encoding.BigEndianUnicode,
            _ => Encoding.Default
        };

        public static string? GetName(Encoding? encoding)
        {
            if (encoding is null) return null;

            byte[] preamble = encoding.GetPreamble();

            return encoding switch
            {
                UTF8Encoding _ when preamble.Length == 0 => "UTF-8",
                UTF8Encoding _ when preamble[0] == 0xef && preamble[1] == 0xbb && preamble[2] == 0xbf => "UTF-8 BOM",
                UnicodeEncoding _ when preamble[0] == 0xff && preamble[1] == 0xfe => "UTF-16 LE",
                UnicodeEncoding _ when preamble[0] == 0xfe && preamble[1] == 0xff => "UTF-16 BE",
                _ => "UTF-8"
            };
        }
    }

    public class DiagnosticsEqualityComparer : EqualityComparer<Diagnostic>
    {
        public static new DiagnosticsEqualityComparer Default { get; } = new DiagnosticsEqualityComparer();

        public override bool Equals(Diagnostic x, Diagnostic y)
        {
            return x.Location == y.Location;
        }

        public override int GetHashCode(Diagnostic obj)
        {
            return obj.Location.GetHashCode();
        }
    }

    public class Int32ToBooleanComparisonConverter : IValueConverter
    {
        public int CurrentValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value == int.Parse((string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? int.Parse((string)parameter) : CurrentValue;
        }
    }

    public class EncodingToBooleanComparisonConverter : IValueConverter
    {
        public Encoding? CurrentValue { get; set; }

        public object Convert(object? value, Type targetType, object parameter, string language)
        {
            return ((Encoding?)value) == EncodingHelper.GetEncoding((string)parameter);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? EncodingHelper.GetEncoding((string)parameter) : CurrentValue;
        }
    }

    public class NewLineModeToBooleanComparisonConverter : IValueConverter
    {
        public NewLineMode CurrentValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (NewLineMode)value == Enum.Parse<NewLineMode>((string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Enum.Parse<NewLineMode>((string)parameter) : CurrentValue;
        }
    }
}
