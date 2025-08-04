#region

using Editor.Core;

#endregion

namespace Editor.UI;

public class EditorState
{
    public EditorMode Mode { get; set; } = EditorMode.Normal;
    public int CursorLine { get; private set; }
    public int CursorColumn { get; private set; }


    public int PreviousCursorLine { get; private set; }

    public int? SelectionStart { get; set; }
    private int SelectionEnd { get; set; }

    public bool HasSelection => SelectionStart.HasValue;
    public List<string> Clipboard { get; } = [];

    public void UpdateFromDocument(Document document)
    {
        PreviousCursorLine = CursorLine;
        var (line, col) = document.CurrentLineColumn;
        CursorLine = line - 1;
        CursorColumn = col - 1;
        SelectionEnd = document.CursorPosition;
    }

    public (int start, int end) GetNormalizedSelection()
    {
        if (!SelectionStart.HasValue) return (SelectionEnd, SelectionEnd);
        var start = Math.Min(SelectionStart.Value, SelectionEnd);
        var end = Math.Max(SelectionStart.Value, SelectionEnd);
        return (start, end);
    }
}

public enum EditorMode
{
    Normal,
    Insert,
    Visual
}