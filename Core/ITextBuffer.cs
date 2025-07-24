namespace Editor.Core;

public interface ITextBuffer : IDisposable
{
    int Length { get; }
    int Position { get; }
    char this[int index] { get; }
    
    void Insert(char character);
    void Insert(string text);
    void Delete(int count = 1, DeleteDirection direction = DeleteDirection.Forward);
    void MoveTo(int position);
}