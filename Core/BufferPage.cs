namespace Editor.Core;

internal class BufferPage
{
    private readonly List<char> content;
    private readonly int maxSize;
    
    public int Index { get; private set; }
    public int Length => content.Count;
    public bool IsFull => content.Count >= maxSize;
    public bool IsDirty { get; private set; }

    public BufferPage(int index, int maxSize)
    {
        Index = index;
        this.maxSize = maxSize;
        content = new List<char>(maxSize);
        IsDirty = false;
    }

    public char GetChar(int offset)
    {
        if (offset < 0 || offset >= content.Count)
            throw new IndexOutOfRangeException();
        return content[offset];
    }

    public void Insert(int offset, char character)
    {
        if (offset < 0 || offset > content.Count)
            throw new IndexOutOfRangeException();
            
        content.Insert(offset, character);
        IsDirty = true;
    }

    public void Delete(int offset)
    {
        if (offset >= 0 && offset < content.Count)
        {
            content.RemoveAt(offset);
            IsDirty = true;
        }
    }

    public void LoadFromString(string data)
    {
        content.Clear();
        content.AddRange(data.ToCharArray());
        IsDirty = false;
    }

    public override string ToString()
    {
        return new string(content.ToArray());
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public string GetContentAfter(int offset)
    {
        if (offset >= content.Count) return string.Empty;
        var chars = content.Skip(offset).ToArray();
        return new string(chars);
    }

    public void TruncateAfter(int offset)
    {
        if (offset < content.Count)
        {
            content.RemoveRange(offset, content.Count - offset);
            IsDirty = true;
        }
    }

    public void AppendContent(string data)
    {
        content.AddRange(data.ToCharArray());
        IsDirty = true;
    }

    public string GetAllContent()
    {
        return new string(content.ToArray());
    }

    public void UpdateIndex(int newIndex)
    {
        Index = newIndex;
    }
}