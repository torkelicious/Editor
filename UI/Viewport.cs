namespace Editor.UI;

public class Viewport
{
    private const int HorizontalPadding = 5;
    /*
     * This class mainly handles veiwport calculations so we can
     * pass them over to the ConsoleRenderer class
     * Most of this is ripped directly from the old prototype version of the code.
     */

    public int StartLine { get; private set; }
    public int StartColumn { get; private set; }
    public int VisibleLines { get; private set; }
    public int VisibleColumns { get; private set; }

    public void UpdateDimensions(int availableLines, int availableColumns)
    {
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

    public bool IsVisible(int line, int column)
    {
        return line >= StartLine &&
               line < StartLine + VisibleLines &&
               column >= StartColumn &&
               column < StartColumn + VisibleColumns;
    }

    public void ScrollUp(int lines = 1)
    {
        StartLine = Math.Max(0, StartLine - lines);
    }

    public void ScrollDown(int lines = 1)
    {
        StartLine += lines;
    }

    public void ScrollLeft(int columns = 1)
    {
        StartColumn = Math.Max(0, StartColumn - columns);
    }

    public void ScrollRight(int columns = 1)
    {
        StartColumn += columns;
    }
}