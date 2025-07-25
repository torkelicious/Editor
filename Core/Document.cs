using System.Text;

namespace Editor.Core;

public class Document : IDisposable
{
    /*
     * This is a wrapper for our ITextBuffer class, for document editing etc
     */
    public enum DocumentState
    {
        Clean, // No changes
        Dirty, // Has unsaved changes
        Loading, // Currently Reading / Loading from a file
        Saving, // Currently Writing / Saving to a file
        ReadOnly, // File is ReadOnly / we lack permissions
        Error // File operation failed error state
    }

    private ITextBuffer buffer;
    private string[]? cachedLines;
    private DateTime cacheTimestamp;
    private bool lineIndexValid;

    private List<int>? lineStartPositions; // Fast line lookup

    public bool showDebugInfo = false;

    // Constructor
    public Document(string? filePath = null)
    {
        FilePath = filePath;
        State = DocumentState.Clean;
        LastModified = DateTime.UtcNow;
        OriginalFileSize = 0;
        lineIndexValid = false;
        buffer = new GapBuffer();

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            OriginalFileSize = new FileInfo(filePath).Length;
            showLoadingInfo();
            LoadFromFile(filePath);
        }
        else
        {
            // For a new doc the index is valid.
            lineStartPositions = new List<int> { 0 };
            lineIndexValid = true;
        }
    }

    // Properties
    public DocumentState State { get; private set; }

    public bool IsDirty => State == DocumentState.Dirty;
    public bool IsReadOnly => State == DocumentState.ReadOnly;
    public bool IsEditable => State == DocumentState.Clean || State == DocumentState.Dirty;
    public int Length => buffer.Length;
    public int CursorPosition => buffer.Position;
    public string? FilePath { get; private set; }

    public DateTime LastModified { get; private set; }

    public bool IsUntitled => string.IsNullOrEmpty(FilePath);
    public long OriginalFileSize { get; }

    // Indexer
    public char this[int index] => buffer[index];

    public (int line, int column) CurrentLineColumn => GetLineColumn(CursorPosition);

    public void Dispose()
    {
        buffer.Dispose();
        cachedLines = null;
        OnLineChanged = null;
        OnDocumentChanged = null;
        lineStartPositions = null;
    }

    public event Action<int>? OnLineChanged;
    public event Action? OnDocumentChanged;

    // State management
    private void SetState(DocumentState newState)
    {
        State = newState;
        if (newState == DocumentState.Dirty) LastModified = DateTime.UtcNow;
    }

    // Editing operations
    private void HandleInsert(int currentLine, bool containsNewline)
    {
        InvalidateLineIndex();

        if (containsNewline)
            OnDocumentChanged?.Invoke();
        else
            OnLineChanged?.Invoke(currentLine);

        SetState(DocumentState.Dirty);
    }

    public void Insert(char character)
    {
        if (!IsEditable) return;

        var currentLine = CurrentLineColumn.line - 1;
        buffer.Insert(character);

        HandleInsert(currentLine, character == '\n');
    }

    public void Insert(string text)
    {
        if (!IsEditable || string.IsNullOrEmpty(text)) return;

        var currentLine = CurrentLineColumn.line - 1;
        buffer.Insert(text);

        HandleInsert(currentLine, text.Contains('\n'));
    }

    public void Delete(int count = 1, DeleteDirection direction = DeleteDirection.Forward)
    {
        if (!IsEditable) return;

        var currentLine = CurrentLineColumn.line - 1;
        var newlineDeleted = false;

        if (direction == DeleteDirection.Forward)
        {
            for (var i = 0; i < count && buffer.Position + i < buffer.Length; i++)
                if (buffer[buffer.Position + i] == '\n')
                {
                    newlineDeleted = true;
                    break;
                }
        }
        else // Backward
        {
            for (var i = 1; i <= count && buffer.Position - i >= 0; i++)
                if (buffer[buffer.Position - i] == '\n')
                {
                    newlineDeleted = true;
                    break;
                }
        }

        buffer.Delete(count, direction);
        InvalidateLineIndex();

        if (newlineDeleted)
            OnDocumentChanged?.Invoke();
        else
            OnLineChanged?.Invoke(currentLine);

        SetState(DocumentState.Dirty);
    }


    public void Backspace(int count = 1)
    {
        Delete(count, DeleteDirection.Backward);
    }

    public void MoveCursor(int position)
    {
        buffer.MoveTo(position);
    }

    // File operations
    public void LoadFromFile(string filePath)
    {
        SetState(DocumentState.Loading);
        try
        {
            var content = File.ReadAllText(filePath);
            buffer.Insert(content);
            buffer.MoveTo(0);
            this.FilePath = filePath;
            InvalidateLineIndex();
            SetState(DocumentState.Clean);
            LastModified = File.GetLastWriteTimeUtc(filePath);
            OnDocumentChanged?.Invoke();
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
            var targetPath = filePath ?? this.FilePath ?? throw new InvalidOperationException("No file path specified");
            var content = GetText();
            File.WriteAllText(targetPath, content);
            this.FilePath = targetPath;
            SetState(DocumentState.Clean);
            LastModified = DateTime.UtcNow;
        }
        catch
        {
            SetState(DocumentState.Error);
            throw;
        }
    }

    public string GetText()
    {
        var sb = new StringBuilder(buffer.Length);
        for (var i = 0; i < buffer.Length; i++) sb.Append(buffer[i]);

        return sb.ToString();
    }

    // Navigation helpers
    public (int line, int column) GetLineColumn(int position)
    {
        // !! this is a slow operation and should be used sparingly !!
        int line = 1, column = 1;
        for (var i = 0; i < Math.Min(position, Length); i++)
            if (buffer[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }

        return (line, column);
    }

    // Utility methods
    public void Clear()
    {
        if (!IsEditable) return;

        buffer.Dispose();
        buffer = new GapBuffer();
        InvalidateLineIndex();
        SetState(DocumentState.Dirty);
        OnDocumentChanged?.Invoke();
    }

    public bool HasUnsavedChanges()
    {
        return IsDirty;
    }

    private void showLoadingInfo()
    {
        var sizeMB = OriginalFileSize / (1024.0 * 1024.0);
        Console.WriteLine($"Loading file: {Path.GetFileName(FilePath)} ({sizeMB:F1}MB)");
        Console.WriteLine($"Using {buffer}");
    }

    public string GetLine(int lineNumber)
    {
        var lines = GetCachedLines();
        return lineNumber >= 0 && lineNumber < lines.Length ? lines[lineNumber] : "";
    }

    public int GetLineCount()
    {
        if (!lineIndexValid) BuildLineIndex();

        return lineStartPositions?.Count ?? 1;
    }

    private string[] GetCachedLines()
    {
        if (cachedLines == null || cacheTimestamp < LastModified)
        {
            var text = GetText();
            cachedLines = string.IsNullOrEmpty(text) ? new[] { "" } : text.Split('\n');
            cacheTimestamp = DateTime.UtcNow;
        }

        return cachedLines;
    }

    public string GetPerformanceInfo()
    {
        var currentMemory = GC.GetTotalMemory(false) / (1024 * 1024);
        var bufferEfficiency = OriginalFileSize > 0 ? (double)currentMemory / (OriginalFileSize / (1024 * 1024)) : 1.0;

        return $"File: {OriginalFileSize / 1024}KB, " +
               $"Memory: ~{currentMemory}MB, " +
               $"Efficiency: {bufferEfficiency:F1}x, " +
               $"Lines: {GetLineCount():N0}, " +
               $"LineIndex: {(lineIndexValid ? "Active" : "Inactive")}";
    }

    // Line indexing 4 fast navigation
    private void BuildLineIndex()
    {
        lineStartPositions = new List<int> { 0 }; // Line 1 always starts at position 0.

        for (var i = 0; i < buffer.Length; i++)
            if (buffer[i] == '\n')
                lineStartPositions.Add(i + 1);

        lineIndexValid = true;
    }

    private void InvalidateLineIndex()
    {
        lineIndexValid = false;
        lineStartPositions = null;
        cachedLines = null;
    }

    public int GetPositionFromLine(int lineNumber, int column = 1)
    {
        if (!lineIndexValid) BuildLineIndex();

        if (lineStartPositions != null)
        {
            var lineIndex = lineNumber - 1; // to 0-based
            if (lineIndex < 0 || lineIndex >= lineStartPositions.Count) return Length;

            var lineStart = lineStartPositions[lineIndex];
            var maxColumn = GetLineLength(lineNumber);
            var targetColumn = Math.Min(column - 1, maxColumn); // 0-based clamp

            return lineStart + targetColumn;
        }

        // Fallback 
        return GetPositionFromLineColumnSlow(lineNumber, column);
    }

    private int GetLineLength(int lineNumber)
    {
        if (!lineIndexValid || lineStartPositions == null)
            // fallback
            return GetLine(lineNumber - 1).Length;

        var lineIndex = lineNumber - 1;
        if (lineIndex < 0 || lineIndex >= lineStartPositions.Count)
            return 0;

        var lineStart = lineStartPositions[lineIndex];
        var lineEnd = lineIndex + 1 < lineStartPositions.Count
            ? lineStartPositions[lineIndex + 1] - 1
            : buffer.Length;

        return Math.Max(0, lineEnd - lineStart);
    }

    private int GetPositionFromLineColumnSlow(int targetLine, int targetColumn)
    {
        var currentLine = 1;
        var position = 0;

        while (position < buffer.Length && currentLine < targetLine)
        {
            if (buffer[position] == '\n') currentLine++;

            position++;
        }

        var columnCount = 1;
        while (position < buffer.Length &&
               buffer[position] != '\n' &&
               columnCount < targetColumn)
        {
            position++;
            columnCount++;
        }

        return position;
    }
}