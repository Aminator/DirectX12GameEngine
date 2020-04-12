using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectX12GameEngine.Editor.ViewModels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

#nullable enable

namespace DirectX12GameEngine.Editor.Views
{
    public sealed partial class CodeEditorView : UserControl
    {
        private TextSpan previousSelection;
        private TextSpan currentSelection;

        public CodeEditorView()
        {
            InitializeComponent();

            FindAndReplaceFlyoutShadow.Receivers.Add(CodeEditor);

            CodeEditor.AddHandler(PointerMovedEvent, new PointerEventHandler(OnCodeEditorPointerMoved), true);
            CodeEditor.AddHandler(KeyDownEvent, new KeyEventHandler(OnCodeEditorKeyDown), true);

            CodeEditor.TextDocument.UndoLimit = 50;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public CodeEditorViewModel ViewModel => (CodeEditorViewModel)DataContext;

        public IEnumerable<Diagnostic> CurrentDiagnosticsAtPointerPosition { get; set; } = Enumerable.Empty<Diagnostic>();

        public SyntaxNode? CurrentSyntaxNodeAtPointerPosition { get; set; }

        public ISymbol? CurrentSymbolAtPointerPosition { get; set; }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.DocumentChanged += OnViewModelDocumentChanged;

            if (ViewModel.CurrentText is null)
            {
                ViewModel.CurrentText = await ViewModel.LoadTextAsync();
            }

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

            CodeEditorToolTip.IsOpen = false;
            CompletionListFlyout.Hide();
        }

        private async void OnViewModelDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            if (e.NewText != null)
            {
                CodeEditor.TextDocument.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string text);

                if (text != e.NewText)
                {
                    ViewModel.CurrentText = e.NewText;
                    CodeEditor.TextDocument.SetText(TextSetOptions.None, e.NewText);
                }
            }

            if (e.IsClassificationChanged)
            {
                if (e.NewText != null)
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
            ApplyClassifiedSpans(await ViewModel.GetChangedClassifiedSpansAsync());
            ClearDiagnostics();
            ApplyDiagnostics(await ViewModel.GetDiagnosticsAsync());
        }

        private async Task ReapplySyntaxHighlightingAsync()
        {
            ApplyClassifiedSpans(await ViewModel.GetClassifiedSpansAsync());
            ClearDiagnostics();
            ApplyDiagnostics(await ViewModel.GetDiagnosticsAsync());
        }

        private void ApplyClassifiedSpans(IEnumerable<ClassifiedSpan> classifiedSpans)
        {
            string themeName = CodeEditor.ActualTheme == ElementTheme.Default
                ? Application.Current.RequestedTheme.ToString()
                : CodeEditor.ActualTheme.ToString();

            var themeDictionary = (ResourceDictionary)Resources.ThemeDictionaries[themeName];

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

        private void ApplyDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                TextSpan span = GetAdjustedTextSpan(diagnostic.Location.SourceSpan, ViewModel.CurrentText!, ViewModel.NewLineMode);
                ITextRange diagnosticRange = CodeEditor.Document.GetRange(span.Start, span.End);

                if (diagnosticRange.Length == 0)
                {
                    diagnosticRange.MoveStart(TextRangeUnit.Word, -1);
                }

                (diagnosticRange.CharacterFormat.Underline) = diagnostic.Severity switch
                {
                    DiagnosticSeverity.Hidden => (UnderlineType.Dotted),
                    DiagnosticSeverity.Info => (UnderlineType.Dotted),
                    DiagnosticSeverity.Warning => (UnderlineType.Wave),
                    DiagnosticSeverity.Error => (UnderlineType.Wave),
                    _ => (UnderlineType.Undefined)
                };
            }
        }

        private void ClearDiagnostics()
        {
            ITextRange textRange = CodeEditor.Document.GetRange(0, TextConstants.MaxUnitCount);
            textRange.CharacterFormat.Underline = UnderlineType.None;
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
                HighlightSearchBoxMatches();

                CodeEditor.TextDocument.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string wholeText);

                string? previousText = ViewModel.CurrentText;

                if (wholeText != previousText)
                {
                    ViewModel.CurrentText = wholeText;

                    int start = Math.Min(previousSelection.Start, currentSelection.Start);

                    ITextRange range = CodeEditor.Document.GetRange(start, currentSelection.End);
                    range.GetText(TextGetOptionsFromNewLineMode(ViewModel.NewLineMode), out string text);

                    TextSpan span = GetAdjustedTextSpan(TextSpan.FromBounds(start, previousSelection.End), previousText, ViewModel.NewLineMode, true);
                    TextChange textChange = new TextChange(span, text);

                    await ViewModel.ApplyChangesAsync(textChange, null);
                }
            }
        }

        private void OnCodeEditorTextChanged(object sender, RoutedEventArgs e)
        {
        }

        private async void OnCodeEditorCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs e)
        {
            if (e.Character == '.' || IsValidCSharpIdentifierCharacter(e.Character))
            {
                await ShowCompletionListAsync();
            }
            else
            {
                CompletionListFlyout.Hide();
            }
        }

        private async void OnCodeEditorKeyDown(object sender, KeyRoutedEventArgs e)
        {
            bool isShiftDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
            bool isControlDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            bool isMenuDown = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);

            switch (e.Key)
            {
                case VirtualKey.Tab:
                    TabSelection(isShiftDown);

                    e.Handled = true;
                    break;
                case VirtualKey.J:
                    if (isControlDown)
                    {
                        await ShowCompletionListAsync();
                    }
                    break;
                case VirtualKey.F:
                    if (isControlDown)
                    {
                        ShowFindAndReplaceFlyout();
                    }
                    break;
            }
        }

        private void ShowFindAndReplaceFlyout()
        {
            ViewModel.FindAndReplace.IsVisible = true;
        }

        private void HideFindAndReplaceFlyout()
        {
            ViewModel.FindAndReplace.IsVisible = false;
        }

        private void HighlightSearchBoxMatches()
        {
            ClearHighlights();

            Color highlightBackgroundColor = Colors.Orange;

            string? textToFind = SearchTextBox.Text;

            if (textToFind != null)
            {
                ITextRange searchRange = CodeEditor.Document.GetRange(0, 0);

                FindOptions findOptions = MatchCaseToggleButton.IsChecked!.Value ? FindOptions.Case : FindOptions.None;
                findOptions = MatchWordToggleButton.IsChecked!.Value ? FindOptions.Word : findOptions;

                while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, findOptions) > 0)
                {
                    searchRange.CharacterFormat.BackgroundColor = highlightBackgroundColor;
                }
            }
        }

        private void ClearHighlights()
        {
            ITextRange textRange = CodeEditor.Document.GetRange(0, TextConstants.MaxUnitCount);
            textRange.CharacterFormat.BackgroundColor = Colors.Transparent;
        }

        private async Task ShowCompletionListAsync()
        {
            if (ViewModel.CodeEditor is null) return;

            ITextRange range = CodeEditor.Document.Selection;
            
            if (range.Length < 0)
            {
                range.EndPosition = range.StartPosition;
            }
            else
            {
                range.StartPosition = range.EndPosition;
            }

            TextSpan selectionSpan = GetAdjustedTextSpan(TextSpan.FromBounds(range.StartPosition, range.EndPosition), ViewModel.CurrentText, ViewModel.NewLineMode, true);

            CompletionList? completionList = await ViewModel.GetCompletionListAsync(selectionSpan.Start);

            if (completionList != null)
            {
                bool expandsToWord = true;

                ITextRange wordRange = range.GetClone();

                if (wordRange.Character == '\r')
                {
                    ITextRange wordRangeClone = range.GetClone();
                    wordRangeClone.Move(TextRangeUnit.Word, -1);

                    expandsToWord = IsValidCSharpIdentifierCharacter(wordRangeClone.Character);
                }

                if (expandsToWord)
                {
                    wordRange.Move(TextRangeUnit.Word, -1);
                    wordRange.MoveEnd(TextRangeUnit.Word, 1);
                }

                string word = wordRange.Text.Trim();

                var completionItems = ViewModel.FilterCompletionItems(completionList.Items, word);

                CompletionListFlyout.Items.Clear();

                foreach (CompletionItem item in completionItems)
                {
                    var properties = item.Properties;

                    if (properties.TryGetValue("SymbolName", out string symbolName))
                    {
                        MenuFlyoutItem menuFlyoutItem = new MenuFlyoutItem
                        {
                            Text = symbolName
                        };

                        menuFlyoutItem.Click += async (s, e) =>
                        {
                            CompletionChange? completionChange = await ViewModel.GetCompletionChangeAsync(item);

                            if (completionChange != null)
                            {
                                TextSpan span = GetAdjustedTextSpan(completionChange.TextChange.Span, ViewModel.CurrentText, ViewModel.NewLineMode);

                                CodeEditor.Document.Selection.StartPosition = span.Start;
                                CodeEditor.Document.Selection.EndPosition = span.End;

                                CodeEditor.Document.Selection.TypeText(completionChange.TextChange.NewText);
                            }
                        };

                        CompletionListFlyout.Items.Add(menuFlyoutItem);
                    }
                }
                   
                if (CompletionListFlyout.Items.Count > 0)
                {
                    Point flyoutPosition = GetAdjustedPointFromDocument(range);
                    flyoutPosition = new Point(flyoutPosition.X + 12, flyoutPosition.Y + 8);

                    CompletionListFlyout.ShowAt(CodeEditor, new FlyoutShowOptions { Position = flyoutPosition, ShowMode = FlyoutShowMode.Transient });
                    return;
                }
            }

            CompletionListFlyout.Hide();
        }

        private void TabSelection(bool isShiftDown)
        {
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
        }

        private async void OnCodeEditorPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint point = e.GetCurrentPoint(CodeEditor);
            await ShowDiagnostics(point.Position);
        }

        private async Task ShowDiagnostics(Point point)
        {
            ScrollViewer scrollViewer = CodeEditor.FindDescendant<ScrollViewer>();

            Point position = new Point(point.X + scrollViewer.HorizontalOffset - 12, point.Y + scrollViewer.VerticalOffset - 12);
            ITextRange pointerRange = CodeEditor.Document.GetRangeFromPoint(position, PointOptions.ClientCoordinates);
            TextSpan caretSpan = GetAdjustedTextSpan(TextSpan.FromBounds(pointerRange.StartPosition, pointerRange.EndPosition), ViewModel.CurrentText, ViewModel.NewLineMode, true);

            SyntaxNode? node = !char.IsWhiteSpace(pointerRange.Character) ? await ViewModel.GetSyntaxNodeAsync(caretSpan) : null;

            var diagnostics = await ViewModel.GetDiagnosticsAsync();
            diagnostics = diagnostics.Where(diagnostic =>
            {
                TextSpan span = GetAdjustedTextSpan(diagnostic.Location.SourceSpan, ViewModel.CurrentText, ViewModel.NewLineMode);
                ITextRange diagnosticRange = CodeEditor.Document.GetRange(span.Start, span.End);

                if (diagnosticRange.Length == 0)
                {
                    diagnosticRange.MoveStart(TextRangeUnit.Word, -1);
                }

                return TextSpan.FromBounds(diagnosticRange.StartPosition, diagnosticRange.EndPosition).Contains(pointerRange.StartPosition);
            });

            if (node != CurrentSyntaxNodeAtPointerPosition || !CurrentDiagnosticsAtPointerPosition.SequenceEqual(diagnostics, DiagnosticsEqualityComparer.Default))
            {
                CurrentSyntaxNodeAtPointerPosition = node;
                CurrentDiagnosticsAtPointerPosition = diagnostics;

                SemanticModel? semanticModel = null;
                ISymbol? symbol = null;

                if (node != null)
                {
                    semanticModel = await ViewModel.GetSemanticModelAsync();

                    if (semanticModel != null)
                    {
                        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(node);
                        symbol = symbolInfo.Symbol;
                    }
                }

                CurrentSymbolAtPointerPosition = symbol;

                bool hasDiagnostics = diagnostics.Count() > 0;

                if (symbol != null || hasDiagnostics)
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    if (symbol != null)
                    {
                        stringBuilder.Append(symbol.ToDisplayString());
                    }

                    if (hasDiagnostics)
                    {
                        if (symbol != null)
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine();
                        }

                        stringBuilder.Append(diagnostics.First().GetMessage());

                        foreach (Diagnostic diagnostic in diagnostics.Skip(1))
                        {
                            stringBuilder.AppendLine();
                            stringBuilder.AppendLine();
                            stringBuilder.Append(diagnostic.GetMessage());
                        }
                    }

                    CodeEditorToolTip.Content = stringBuilder.ToString();

                    Point toolTipPosition = GetAdjustedPointFromDocument(pointerRange);
                    Rect exclusionRect = new Rect(toolTipPosition, new Size(12, 12));

                    CodeEditorToolTip.PlacementRect = exclusionRect;
                    CodeEditorToolTip.IsOpen = true;
                }
                else
                {
                    CodeEditorToolTip.IsOpen = false;
                }
            }
        }

        private Point GetAdjustedPointFromDocument(ITextRange range)
        {
            ScrollViewer scrollViewer = CodeEditor.FindDescendant<ScrollViewer>();

            range.GetPoint(HorizontalCharacterAlignment.Left, VerticalCharacterAlignment.Bottom, PointOptions.ClientCoordinates, out Point textRangePoint);
            return new Point(textRangePoint.X - scrollViewer.HorizontalOffset, textRangePoint.Y - scrollViewer.VerticalOffset);
        }

        private void OnCodeEditorToolTipOpened(object sender, RoutedEventArgs e)
        {
            if (CurrentSymbolAtPointerPosition is null && CurrentDiagnosticsAtPointerPosition.Count() == 0)
            {
                CodeEditorToolTip.IsOpen = false;
            }
        }

        private void OnCodeEditorToolTipClosed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded && (CurrentSymbolAtPointerPosition != null || CurrentDiagnosticsAtPointerPosition.Count() > 0))
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

        private bool IsValidCSharpIdentifierCharacter(char value)
        {
            return value == '_' || char.IsLetterOrDigit(value);
        }
    }

    public static class StringHelper
    {
        public static string ToUpper(object value) => value.ToString().ToUpper();

        public static string ToLower(object value) => value.ToString().ToLower();
    }

    public static class VisibilityHelper
    {
        public static Visibility NegateVisibility(Visibility value) => value == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
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
            return EncodingHelper.GetName((Encoding?)value) == (string)parameter;
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
