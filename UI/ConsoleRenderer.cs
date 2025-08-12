#region

using Editor.Core;

#endregion

namespace Editor.UI;

public class ConsoleRenderer(Viewport viewport)
{
    private const int LinesPadding = 3;
    private const int ColumnPadding = 1;
    private readonly HashSet<int> dirtyLines = [];
    private bool fullRedrawNeeded = true;

    public void RegisterWithDocument(Document document)
    {
        document.OnLineChanged += MarkLineDirty;
        document.OnDocumentChanged += MarkAllDirty;
        MarkAllDirty();
    }

    public void Render(Document document, EditorState editorState, string lastInput = " ")
    {
        AnsiConsole.ResetColor();
        AnsiConsole.HideCursor();
        var availableLines = Console.WindowHeight - LinesPadding;
        var availableColumns = Console.WindowWidth - ColumnPadding;

        var oldStartLine = viewport.StartLine;
        var oldStartColumn = viewport.StartColumn;

        viewport.UpdateDimensions(availableLines, availableColumns);
        viewport.AdjustToShowCursor(editorState.CursorLine, editorState.CursorColumn);

        var viewportMoved = oldStartLine != viewport.StartLine || oldStartColumn != viewport.StartColumn;

        if (fullRedrawNeeded || viewportMoved)
        {
            AnsiConsole.Clear();
            var startLine = viewport.StartLine;
            var endLine = Math.Min(startLine + viewport.VisibleLines, document.GetLineCount());
            for (var i = startLine; i < endLine; i++) dirtyLines.Add(i);

            fullRedrawNeeded = false;
        }

        RenderDocumentContent(document, editorState);
        StatusBar.Render(document, editorState, LinesPadding, lastInput);
        PositionCursor(editorState);
        AnsiConsole.ShowCursor();
        AnsiConsole.ResetColor();
    }

    private void RenderDocumentContent(Document document, EditorState editorState)
    {
        var startLine = viewport.StartLine;
        var endLine = Math.Min(startLine + viewport.VisibleLines, document.GetLineCount());

        dirtyLines.Add(editorState.CursorLine);
        if (editorState.CursorLine != editorState.PreviousCursorLine) dirtyLines.Add(editorState.PreviousCursorLine);

        if (editorState.HasSelection)
        {
            var (selStart, selEnd) = editorState.GetNormalizedSelection();
            var startLineNum = document.GetLineColumn(selStart).line - 1;
            var endLineNum = document.GetLineColumn(selEnd).line - 1;
            for (var i = startLineNum; i <= endLineNum; i++) dirtyLines.Add(i);
        }


        var linesToRender = new HashSet<int>(dirtyLines);

        foreach (var lineIndex in linesToRender)
            if (lineIndex >= startLine && lineIndex < endLine)
            {
                AnsiConsole.ResetColor();
                var lineContent = document.GetLine(lineIndex);
                var processedLine = ProcessLineForDisplay(lineContent, lineIndex, editorState, document);
                var screenY = lineIndex - viewport.StartLine;
                Console.SetCursorPosition(0, screenY);
                AnsiConsole.Write(processedLine.text.PadRight(viewport.VisibleColumns));
                DrawScrollIndicators(processedLine, lineIndex, editorState.CursorLine, screenY);
                AnsiConsole.ResetColor();
            }

        dirtyLines.Clear();
    }

    private void MarkLineDirty(int lineIndex)
    {
        dirtyLines.Add(lineIndex);
    }

    public void MarkAllDirty()
    {
        fullRedrawNeeded = true;
        dirtyLines.Clear();
    }

    private (string text, bool truncated) ProcessLineForDisplay(string line, int lineIndex, EditorState editorState,
        Document document)
    {
        // escape braces in the user content with backslashes to avoid messing with colors
        var displayLine = line.Replace("{", "\\{").Replace("}", "\\}");

        if (editorState.HasSelection)
        {
            var (selStart, selEnd) = editorState.GetNormalizedSelection();
            var lineStartPos = document.GetPositionFromLine(lineIndex + 1);
            var lineEndPos = lineStartPos + line.Length;

            var lineInSelection = selStart <= lineEndPos && selEnd > lineStartPos;

            if (lineInSelection)
            {
                if (line.Length > 0)
                {
                    var relativeStart = Math.Max(0, selStart - lineStartPos);
                    var relativeEnd = Math.Min(line.Length, selEnd - lineStartPos);

                    if (relativeStart < relativeEnd)
                    {
                        // escape in parts then add color codes
                        var before = line[..relativeStart].Replace("{", "\\{").Replace("}", "\\}");
                        var selected = line[relativeStart..relativeEnd].Replace("{", "\\{").Replace("}", "\\}");
                        var after = line[relativeEnd..].Replace("{", "\\{").Replace("}", "\\}");
                        displayLine = $"{before}{{BG_BLUE}}{selected}{{RESET}}{after}";
                    }
                }
                else
                {
                    displayLine = "{BG_BLUE} {RESET}";
                }
            }
        }

        if (viewport.StartColumn > 0 && displayLine.Length > viewport.StartColumn)
            displayLine = displayLine[viewport.StartColumn..];
        else if (viewport.StartColumn > 0)
            displayLine = string.Empty;
        var truncated = false;
        if (displayLine.Length > viewport.VisibleColumns)
        {
            displayLine = displayLine[..viewport.VisibleColumns];
            truncated = true;
        }

        return (displayLine, truncated);
    }

    private void DrawScrollIndicators((string text, bool truncated) lineContent, int lineIndex, int cursorLine,
        int screenY)
    {
        // Left indicator
        if (viewport.StartColumn > 0 && lineIndex == cursorLine)
        {
            Console.SetCursorPosition(0, screenY);
            AnsiConsole.Write("{YELLOW}<{WHITE}");
        }

        // Right indicator
        if (lineContent.truncated && lineIndex == cursorLine)
        {
            Console.SetCursorPosition(viewport.VisibleColumns - 1, screenY);
            AnsiConsole.Write("{YELLOW}>{WHITE}");
        }
    }

    private void PositionCursor(EditorState editorState)
    {
        var (screenX, screenY) = viewport.GetScreenPosition(
            editorState.CursorLine,
            editorState.CursorColumn
        );

        // clamp cursor within bounds
        screenX = Math.Max(0, Math.Min(screenX, Console.WindowWidth - 1));
        screenY = Math.Max(0, Math.Min(screenY, viewport.VisibleLines - 1));

        Console.SetCursorPosition(screenX, screenY);

        // Set cursor shape based on mode
        var cursorShape = editorState.Mode switch
        {
            EditorMode.Normal => AnsiConsole.CursorShape.SteadyBlock,
            EditorMode.Insert => AnsiConsole.CursorShape.BlinkBar,
            EditorMode.Visual => AnsiConsole.CursorShape.SteadyBar,
            _ => AnsiConsole.CursorShape.SteadyBlock
        };

        AnsiConsole.SetCursorShape(cursorShape);
    }

    public void MarkLinesDirty(int startLine, int endLine)
    {
        for (var i = startLine; i <= endLine; i++) dirtyLines.Add(i);
    }
}