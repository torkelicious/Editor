namespace Editor.Core.EditorActions;

public interface IEditorAction
{
    void Do();
    void Undo();
}