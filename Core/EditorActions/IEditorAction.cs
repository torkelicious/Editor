namespace Editor.Core;

public interface IEditorAction
{
    void Do();
    void Undo();
}