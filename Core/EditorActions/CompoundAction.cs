namespace Editor.Core.EditorActions;

public class CompoundAction : IEditorAction
{
    private readonly List<IEditorAction> _actions = [];

    public int Count => _actions.Count;

    public void Do()
    {
        foreach (var action in _actions)
            action.Do();
    }

    public void Undo()
    {
        for (var i = _actions.Count - 1; i >= 0; i--)
            _actions[i].Undo();
    }

    public void Add(IEditorAction action)
    {
        _actions.Add(action);
    }
}