using Editor.Core;

public class InsertAction : IEditorAction
{
    private readonly char? _charContent;
    private readonly Document _document;
    private readonly int _position;
    private readonly string _stringContent;
    private int _previousCursor;

    public InsertAction(Document document, int position, char character)
    {
        _document = document;
        _position = position;
        _charContent = character;
        _stringContent = null;
    }

    public InsertAction(Document document, int position, string content)
    {
        _document = document;
        _position = position;
        _charContent = null;
        _stringContent = content;
    }

    public void Do()
    {
        _previousCursor = _document.CursorPosition;
        _document.MoveCursor(_position);
        if (_charContent.HasValue)
            _document.Insert(_charContent.Value);
        else
            _document.Insert(_stringContent);
    }

    public void Undo()
    {
        _document.MoveCursor(_position);
        var len = _charContent.HasValue ? 1 : _stringContent.Length;
        _document.Delete(len);
        _document.MoveCursor(_previousCursor);
    }
}