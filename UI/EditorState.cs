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

    public void UpdateFromDocument(Document document)
    {
        PreviousCursorLine = CursorLine;
        var (line, col) = document.CurrentLineColumn;
        CursorLine = line - 1;
        CursorColumn = col - 1;
    }
    // TODO: add a visual / select mode so we arent just copying lines
    public List<string> Clipboard { get; set; } = [];
}


public enum EditorMode
{
    Normal,
    Insert
}
