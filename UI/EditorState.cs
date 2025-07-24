using Editor.Core;

namespace Editor.UI;

public class EditorState
{
    public EditorMode Mode { get; set; } = EditorMode.Normal;
    public int CursorLine { get; private set; } = 0;
    public int CursorColumn { get; private set; } = 0;
    public int PreviousCursorLine { get; private set; } = 0;

    public void UpdateFromDocument(Document document)
    {
        PreviousCursorLine = CursorLine;
        var (line, col) = document.CurrentLineColumn;
        CursorLine = line - 1;
        CursorColumn = col - 1;
    }

    public void SetCursorPosition(int line, int column)
    {
        CursorLine = Math.Max(0, line);
        CursorColumn = Math.Max(0, column);
    }
}

public enum EditorMode
{
    Normal,
    Insert
}