using Editor.Core;

namespace Editor.UI;

public class ConsoleRenderer
{
    private const int LinesPadding = 3;
    private const int ColumnPadding = 1;
    public const int MinimumConsoleWidth = 100;
    
    private readonly Viewport viewport;
    
    public ConsoleRenderer(Viewport viewport)
    {
        this.viewport = viewport;
    }
    
    public void Render(Document document, EditorState editorState)
    {
        EnsureMinimumSize();
        PrepareScreen();
        
        var availableLines = Console.WindowHeight - LinesPadding;
        var availableColumns = Console.WindowWidth - ColumnPadding;
        
        viewport.UpdateDimensions(availableLines, availableColumns);
        viewport.AdjustToShowCursor(editorState.CursorLine, editorState.CursorColumn);
        
        RenderDocumentContent(document, editorState);
        RenderStatusBar(document, editorState);
        PositionCursor(editorState);
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
    
    private void RenderDocumentContent(Document document, EditorState editorState)
    {
        var documentLines = GetDocumentLines(document);
        var linesDrawn = 0;
        
        for (var lineIndex = viewport.StartLine; 
             lineIndex < documentLines.Count && linesDrawn < viewport.VisibleLines; 
             lineIndex++)
        {
            var lineContent = ProcessLineForDisplay(
                documentLines[lineIndex], 
                lineIndex, 
                editorState.CursorLine
            );
            
            Console.SetCursorPosition(0, linesDrawn);
            Console.Write(lineContent.text);
            
            // Draw scroll indicators
            DrawScrollIndicators(lineContent, lineIndex, editorState.CursorLine, linesDrawn);
            
            linesDrawn++;
        }
    }
    
    private (string text, bool truncated) ProcessLineForDisplay(string line, int lineIndex, int cursorLine)
    {
        var displayLine = line;
        
        // Handle horizontal scrolling
        if (viewport.StartColumn > 0 && displayLine.Length > viewport.StartColumn)
        {
            displayLine = displayLine.Substring(viewport.StartColumn);
        }
        else if (viewport.StartColumn > 0)
        {
            displayLine = string.Empty;
        }
        
        // Handle line truncation
        bool truncated = false;
        if (displayLine.Length > viewport.VisibleColumns)
        {
            displayLine = displayLine.Substring(0, viewport.VisibleColumns);
            truncated = true;
        }
        
        return (displayLine, truncated);
    }
    
    private void DrawScrollIndicators((string text, bool truncated) lineContent, int lineIndex, int cursorLine, int screenY)
    {
        // Left scroll indicator
        if (viewport.StartColumn > 0 && lineIndex == cursorLine)
        {
            Console.SetCursorPosition(0, screenY);
            Console.Write("<");
        }
        
        // Right scroll indicator
        if (lineContent.truncated && lineIndex == cursorLine)
        {
            Console.SetCursorPosition(viewport.VisibleColumns, screenY);
            Console.Write(">");
        }
    }
    
    private void RenderStatusBar(Document document, EditorState editorState)
    {
        var StatusBar = new StatusBar();
        StatusBar.Render(document, editorState, LinesPadding);
    }
    
    private void PositionCursor(EditorState editorState)
    {
        var (screenX, screenY) = viewport.GetScreenPosition(
            editorState.CursorLine, 
            editorState.CursorColumn
        );
        
        Console.SetCursorPosition(screenX, screenY);
    }
    
    private List<string> GetDocumentLines(Document document)
    {
        var text = document.GetText();
        if (string.IsNullOrEmpty(text)) return new List<string> { "" };
        
        var lines = text.Split('\n').ToList();
        
        // Handle case where document ends with newline
        if (text.EndsWith('\n') && lines.LastOrDefault() == "")
            lines.RemoveAt(lines.Count - 1);
            
        return lines;
    }
}