using Editor.Core;

public class DeleteAction : IEditorAction
{
    private readonly int _count;
    private readonly DeleteDirection _direction;
    private readonly Document _document;
    private readonly int _position;
    private string _deletedText;
    private int _previousCursor;

    public DeleteAction(Document document, int position, DeleteDirection direction, int count)
    {
        _document = document;
        _position = position;
        _direction = direction;
        _count = count;
    }

    public void Do()
    {
        _previousCursor = _document.CursorPosition;
        _document.MoveCursor(_position);

        // Collect deleted text for undo
        _deletedText = "";
        for (var i = 0; i < _count; i++)
        {
            var charPos = _direction == DeleteDirection.Backward
                ? _document.CursorPosition - 1
                : _document.CursorPosition;
            if (charPos < 0 || charPos >= _document.Length) break;
            _deletedText += _document.GetCharAt(charPos);
            _document.Delete(1, _direction);
        }
    }

    public void Undo()
    {
        var insertPos = _direction == DeleteDirection.Backward ? _position - _count : _position;
        _document.MoveCursor(insertPos);
        foreach (var c in _deletedText)
            _document.Insert(c);
        _document.MoveCursor(_previousCursor);
    }
}