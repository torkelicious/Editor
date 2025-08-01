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
        var availableLines = Console.WindowHeight - LinesPadding;
        var availableColumns = Console.WindowWidth - ColumnPadding;

        var oldStartLine = viewport.StartLine;
        var oldStartColumn = viewport.StartColumn;

        viewport.UpdateDimensions(availableLines, availableColumns);
        viewport.AdjustToShowCursor(editorState.CursorLine, editorState.CursorColumn);

        var viewportMoved = oldStartLine != viewport.StartLine || oldStartColumn != viewport.StartColumn;

        if (fullRedrawNeeded || viewportMoved)
        {
            Console.Clear();
            var startLine = viewport.StartLine;
            var endLine = Math.Min(startLine + viewport.VisibleLines, document.GetLineCount());
            for (var i = startLine; i < endLine; i++) dirtyLines.Add(i);

            fullRedrawNeeded = false;
        }

        RenderDocumentContent(document, editorState);
        StatusBar.Render(document, editorState, LinesPadding, lastInput);
        PositionCursor(editorState);
    }

    private void RenderDocumentContent(Document document, EditorState editorState)
    {
        var startLine = viewport.StartLine;
        var endLine = Math.Min(startLine + viewport.VisibleLines, document.GetLineCount());

        dirtyLines.Add(editorState.CursorLine);
        if (editorState.CursorLine != editorState.PreviousCursorLine) dirtyLines.Add(editorState.PreviousCursorLine);

        var linesToRender = new HashSet<int>(dirtyLines);

        foreach (var lineIndex in linesToRender)
            if (lineIndex >= startLine && lineIndex < endLine)
            {
                var lineContent = document.GetLine(lineIndex);

                var processedLine = ProcessLineForDisplay(
                    lineContent
                );

                var screenY = lineIndex - viewport.StartLine;
                Console.SetCursorPosition(0, screenY);
                Console.Write(processedLine.text.PadRight(viewport.VisibleColumns));
                DrawScrollIndicators(processedLine, lineIndex, editorState.CursorLine, screenY);
            }

        dirtyLines.Clear();
    }


    private void MarkLineDirty(int lineIndex)
    {
        dirtyLines.Add(lineIndex);
    }

    private void MarkAllDirty()
    {
        fullRedrawNeeded = true;
        dirtyLines.Clear();
    }

    private (string text, bool truncated) ProcessLineForDisplay(string line)
    {
        var displayLine = line;

        // horizontal scrolling
        if (viewport.StartColumn > 0 && displayLine.Length > viewport.StartColumn)
            displayLine = displayLine[viewport.StartColumn..];
        else if (viewport.StartColumn > 0) displayLine = string.Empty;

        // truncation
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
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("<");
            Console.ForegroundColor = ConsoleColor.White;
        }

        // Right indicator
        if (lineContent.truncated && lineIndex == cursorLine)
        {
            Console.SetCursorPosition(viewport.VisibleColumns - 1, screenY);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(">");
            Console.ForegroundColor = ConsoleColor.White;
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
    }
}