namespace Editor.Core;

public class UndoManager
{
    private readonly Stack<IEditorAction> _redoStack = new();
    private readonly Stack<IEditorAction> _undoStack = new();

    public void PerformAction(IEditorAction action)
    {
        action.Do();
        _undoStack.Push(action);
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;
        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;
        var action = _redoStack.Pop();
        action.Do();
        _undoStack.Push(action);
    }

    public void PushAction(IEditorAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
    }
}