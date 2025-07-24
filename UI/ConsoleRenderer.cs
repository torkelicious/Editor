using Editor.Core;
using System.Diagnostics;
using System.Text;

namespace Editor.UI;

public class ConsoleRenderer
{
    private const int LinesPadding = 3;
    private const int ColumnPadding = 1;
    public const int MinimumConsoleWidth = 100;

    private readonly Viewport viewport;
    private readonly StatusBar statusBar;

    // Optimization 
    private Dictionary<int, string> lineCache = new Dictionary<int, string>();
    private HashSet<int> dirtyLines = new HashSet<int>();
    private bool fullRedrawNeeded = true;
    private int lastStartLine = -1;
    private int lastStartColumn = -1;

    public ConsoleRenderer(Viewport viewport)
    {
        this.viewport = viewport;
        this.statusBar = new StatusBar();
    }

    public void RegisterWithDocument(Document document)
    {
        document.OnLineChanged += MarkLineDirty;
        document.OnDocumentChanged += MarkAllDirty;
        MarkAllDirty();
    }

    public void Render(Document document, EditorState editorState)
    {
        EnsureMinimumSize();

        var availableLines = Console.WindowHeight - LinesPadding;
        var availableColumns = Console.WindowWidth - ColumnPadding;

        var oldStartLine = viewport.StartLine;
        var oldStartColumn = viewport.StartColumn;

        viewport.UpdateDimensions(availableLines, availableColumns);
        viewport.AdjustToShowCursor(editorState.CursorLine, editorState.CursorColumn);

        bool viewportMoved = oldStartLine != viewport.StartLine || oldStartColumn != viewport.StartColumn;

        if (fullRedrawNeeded || viewportMoved)
        {
            PrepareScreen(); // Clears the console
            lineCache.Clear();

            int startLine = viewport.StartLine;
            int endLine = Math.Min(startLine + viewport.VisibleLines, document.GetLineCount());
            for (int i = startLine; i < endLine; i++)
            {
                dirtyLines.Add(i);
            }

            fullRedrawNeeded = false;
        }

        RenderDocumentContent(document, editorState);
        statusBar.Render(document, editorState, LinesPadding);
        PositionCursor(editorState);

        lastStartLine = viewport.StartLine;
        lastStartColumn = viewport.StartColumn;
    }

    private void RenderDocumentContent(Document document, EditorState editorState)
    {
        int startLine = viewport.StartLine;
        int endLine = Math.Min(startLine + viewport.VisibleLines, document.GetLineCount());

        dirtyLines.Add(editorState.CursorLine);
        if (editorState.CursorLine != editorState.PreviousCursorLine)
        {
            dirtyLines.Add(editorState.PreviousCursorLine);
        }

        var linesToRender = new HashSet<int>(dirtyLines);

        foreach (var lineIndex in linesToRender)
        {
            if (lineIndex >= startLine && lineIndex < endLine)
            {
                string lineContent = document.GetLine(lineIndex);
                lineCache[lineIndex] = lineContent;

                var processedLine = ProcessLineForDisplay(
                    lineContent,
                    lineIndex,
                    editorState.CursorLine
                );

                var screenY = lineIndex - viewport.StartLine;
                Console.SetCursorPosition(0, screenY);
                Console.Write(processedLine.text.PadRight(viewport.VisibleColumns));
                DrawScrollIndicators(processedLine, lineIndex, editorState.CursorLine, screenY);
            }
        }

        dirtyLines.Clear();
    }


    public void MarkLineDirty(int lineIndex)
    {
        dirtyLines.Add(lineIndex);
        lineCache.Remove(lineIndex);
    }

    public void MarkAllDirty()
    {
        fullRedrawNeeded = true;
        lineCache.Clear();
        dirtyLines.Clear();
    }

    private void EnsureMinimumSize()
    {
        while (Console.WindowWidth < MinimumConsoleWidth)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Window width too small! (Min: {MinimumConsoleWidth}c)");
            Console.WriteLine("Please resize your Console.");
            Thread.Sleep(500);
        }
    }

    private void PrepareScreen()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();
    }

    private (string text, bool truncated) ProcessLineForDisplay(string line, int lineIndex, int cursorLine)
    {
        var displayLine = line;

        // horizontal scrolling
        if (viewport.StartColumn > 0 && displayLine.Length > viewport.StartColumn)
        {
            displayLine = displayLine.Substring(viewport.StartColumn);
        }
        else if (viewport.StartColumn > 0)
        {
            displayLine = string.Empty;
        }

        // line truncation
        bool truncated = false;
        if (displayLine.Length > viewport.VisibleColumns)
        {
            displayLine = displayLine.Substring(0, viewport.VisibleColumns);
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

        // clamp cursor within screen bounds
        screenX = Math.Max(0, Math.Min(screenX, Console.WindowWidth - 1));
        screenY = Math.Max(0, Math.Min(screenY, viewport.VisibleLines - 1));

        Console.SetCursorPosition(screenX, screenY);
    }
}