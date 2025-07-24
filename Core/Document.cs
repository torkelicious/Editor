using System.Text;

namespace Editor.Core;

public class Document : IDisposable
{
    /*
     * This is a wrapper for our ITextBuffer class, for document editing etc
     */
    public enum DocumentState
    {
        Clean,      // No changes
        Dirty,      // Has unsaved changes
        Loading,    // Currently Reading / Loading from a file
        Saving,     // Currently Writing / Saving to a file
        ReadOnly,   // File is ReadOnly / we lack permissions
        Error       // File operation failed error state
    }

    private ITextBuffer buffer;
    private string? filePath;
    private DocumentState docState;
    private DateTime lastModified;

    // Properties
    public DocumentState State => docState;
    public bool IsDirty => docState == DocumentState.Dirty;
    public bool IsReadOnly => docState == DocumentState.ReadOnly;
    public bool IsEditable => docState == DocumentState.Clean || docState == DocumentState.Dirty;
    public int Length => buffer.Length;
    public int CursorPosition => buffer.Position;
    public string? FilePath => filePath;
    public DateTime LastModified => lastModified;
    public bool IsUntitled => string.IsNullOrEmpty(filePath);
    
    // Indexer
    public char this[int index] => buffer[index];
    
    // Constructor
    public Document(string? filePath = null, bool useSwapping = false)
    {
        // Choose buffer type based on file size or user preference
        if (useSwapping || ShouldUseSwapping(filePath))
        {
            buffer = new SwappingBuffer();
        }
        else
        {
            buffer = new GapBuffer();
        }
        
        this.filePath = filePath;
        docState = DocumentState.Clean;
        lastModified = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            LoadFromFile(filePath);
        }
    }

    private static bool ShouldUseSwapping(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;
            
        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length > 5_000_000; // 5MB threshold
    }

    // State management
    private void SetState(DocumentState newState)
    {
        docState = newState;
        if (newState == DocumentState.Dirty)
        {
            lastModified = DateTime.UtcNow;   
        }
    }

    // Editing operations
    public void Insert(char character)
    {
        if (!IsEditable) return;
        buffer.Insert(character);
        SetState(DocumentState.Dirty);
    }

    public void Insert(string text)
    {
        if (!IsEditable || string.IsNullOrEmpty(text)) return;
        buffer.Insert(text);
        SetState(DocumentState.Dirty);
    }

    public void Delete(int count = 1, DeleteDirection direction = DeleteDirection.Forward)
    {
        if (!IsEditable) return;
        buffer.Delete(count, direction);
        SetState(DocumentState.Dirty);
    }
    public void Backspace(int count = 1)
    {
        Delete(count, DeleteDirection.Backward);
    }
    public void MoveCursor(int position)
    {
        buffer.MoveTo(position);
        // Moving cursor doesn't make document dirty
    }

    // File operations
    public void LoadFromFile(string filePath)
    {
        SetState(DocumentState.Loading);
        try
        {
            var content = File.ReadAllText(filePath);
            
            // Recreate buffer based on file size
            if (buffer is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            if (ShouldUseSwapping(filePath))
            {
                buffer = new SwappingBuffer();
            }
            else
            {
                buffer = new GapBuffer();
            }
            
            buffer.Insert(content);
            buffer.MoveTo(0);
            this.filePath = filePath;
            SetState(DocumentState.Clean);
            lastModified = File.GetLastWriteTimeUtc(filePath);
        }
        catch
        {
            SetState(DocumentState.Error);
            throw;
        }
    }

    public async Task SaveToFileAsync(string? filePath = null)
    {
        SetState(DocumentState.Saving);
        try
        {
            var targetPath = filePath ?? this.filePath ?? throw new InvalidOperationException("No file path specified");
            var content = GetText();
            await File.WriteAllTextAsync(targetPath, content);
            this.filePath = targetPath;
            SetState(DocumentState.Clean);
            lastModified = DateTime.UtcNow;
        }
        catch
        {
            SetState(DocumentState.Error);
            throw;
        }
    }

    public void SaveToFile(string? filePath = null)
    {
        SetState(DocumentState.Saving);
        try
        {
            var targetPath = filePath ?? this.filePath ?? throw new InvalidOperationException("No file path specified");
            var content = GetText();
            File.WriteAllText(targetPath, content);
            this.filePath = targetPath;
            SetState(DocumentState.Clean);
            lastModified = DateTime.UtcNow;
        }
        catch
        {
            SetState(DocumentState.Error);
            throw;
        }
    }

    // Text retrieval
    public string GetText()
    {
        var sb = new StringBuilder(buffer.Length);
        for (int i = 0; i < buffer.Length; i++)
        {
            sb.Append(buffer[i]);
        }
        return sb.ToString();
    }

    // Navigation helpers
    public (int line, int column) GetLineColumn(int position)
    {
        int line = 1, column = 1;
        for (int i = 0; i < Math.Min(position, Length); i++)
        {
            if (buffer[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }
        return (line, column);
    }

    public (int line, int column) CurrentLineColumn => GetLineColumn(CursorPosition);

    // Utility methods
    public void Clear()
    {
        if (!IsEditable) return;
        
        if (buffer is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        buffer = new GapBuffer();
        SetState(DocumentState.Dirty);
    }

    public bool HasUnsavedChanges()
    {
        return IsDirty;
    }

    public void Dispose()
    {
        if (buffer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}