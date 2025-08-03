namespace Editor.UI;

public class Viewport
{
    private const int HorizontalPadding = 5;

    public int StartLine { get; private set; }
    public int StartColumn { get; private set; }
    public int VisibleLines { get; private set; }
    public int VisibleColumns { get; private set; }

    public void UpdateDimensions(int availableLines, int availableColumns)
    {
        if (VisibleLines != availableLines || VisibleColumns != availableColumns) AnsiConsole.Clear();
        VisibleLines = availableLines;
        VisibleColumns = availableColumns;
    }

    public void AdjustToShowCursor(int cursorLine, int cursorColumn)
    {
        AdjustVertical(cursorLine);
        AdjustHorizontal(cursorColumn);
    }

    private void AdjustVertical(int cursorLine)
    {
        // Keep cursor in view vertically
        if (cursorLine < StartLine)
            StartLine = cursorLine;
        else if (cursorLine >= StartLine + VisibleLines) StartLine = cursorLine - VisibleLines + 1;
    }

    private void AdjustHorizontal(int cursorColumn)
    {
        // Keep cursor in view horizontally with padding
        if (cursorColumn < StartColumn + HorizontalPadding)
            StartColumn = Math.Max(0, cursorColumn - HorizontalPadding);
        else if (cursorColumn >= StartColumn + VisibleColumns - HorizontalPadding)
            StartColumn = cursorColumn - VisibleColumns + HorizontalPadding + 1;
    }

    public (int screenX, int screenY) GetScreenPosition(int line, int column)
    {
        return (column - StartColumn, line - StartLine);
    }

    public void ScrollUp(int lines = 1)
    {
        StartLine = Math.Max(0, StartLine - lines);
    }

    public void ScrollDown(int lines = 1)
    {
        StartLine += lines;
    }
}