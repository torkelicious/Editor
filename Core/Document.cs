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

    public event Action<int>? OnLineChanged;
    public event Action? OnDocumentChanged;

    public bool showDebugInfo = false;

    private ITextBuffer buffer;
    private string? filePath;
    private DocumentState docState;
    private DateTime lastModified;

    private long originalFileSize;
    private string[]? cachedLines;
    private DateTime cacheTimestamp;

    private List<int>? lineStartPositions; // Fast line lookup
    private bool lineIndexValid;
    private const int LINE_INDEX_THRESHOLD = 1000; // Build index for files with 1000+ lines

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
    public long OriginalFileSize => originalFileSize;

    // Indexer
    public char this[int index] => buffer[index];

    // Constructor
    public Document(string? filePath = null)
    {
        this.filePath = filePath;
        docState = DocumentState.Clean;
        lastModified = DateTime.UtcNow;
        originalFileSize = 0;
        lineIndexValid = false;
        buffer = new GapBuffer();

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            originalFileSize = new FileInfo(filePath).Length;
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
    private void HandleInsert(int currentLine, bool containsNewline)
    {
        InvalidateLineIndex();

        if (containsNewline)
        {
            OnDocumentChanged?.Invoke();
        }
        else
        {
            OnLineChanged?.Invoke(currentLine);
        }

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
        bool newlineDeleted = false;

        if (direction == DeleteDirection.Forward)
        {
            for (int i = 0; i < count && buffer.Position + i < buffer.Length; i++)
            {
                if (buffer[buffer.Position + i] == '\n')
                {
                    newlineDeleted = true;
                    break;
                }
            }
        }
        else // Backward
        {
            for (int i = 1; i <= count && buffer.Position - i >= 0; i++)
            {
                if (buffer[buffer.Position - i] == '\n')
                {
                    newlineDeleted = true;
                    break;
                }
            }
        }

        buffer.Delete(count, direction);
        InvalidateLineIndex();

        if (newlineDeleted)
        {
            OnDocumentChanged?.Invoke();
        }
        else
        {
            OnLineChanged?.Invoke(currentLine);
        }

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
            this.filePath = filePath;
            InvalidateLineIndex();
            SetState(DocumentState.Clean);
            lastModified = File.GetLastWriteTimeUtc(filePath);
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
        // !! this is a slow operation and should be used sparingly !!
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
            var sizeMB = originalFileSize / (1024.0 * 1024.0);
            Console.WriteLine($"Loading file: {Path.GetFileName(filePath)} ({sizeMB:F1}MB)");
            Console.WriteLine($"Using {buffer}");
    }

    public string GetLine(int lineNumber)
    {
        var lines = GetCachedLines();
        return lineNumber >= 0 && lineNumber < lines.Length ? lines[lineNumber] : "";
    }

    public int GetLineCount()
    {
        if (!lineIndexValid)
        {
            BuildLineIndex();
        }

        return lineStartPositions?.Count ?? 1;
    }

    private string[] GetCachedLines()
    {
        if (cachedLines == null || cacheTimestamp < lastModified)
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
        var bufferEfficiency = originalFileSize > 0 ? (double)currentMemory / (originalFileSize / (1024 * 1024)) : 1.0;

        return $"File: {originalFileSize / 1024}KB, " +
               $"Memory: ~{currentMemory}MB, " +
               $"Efficiency: {bufferEfficiency:F1}x, " +
               $"Lines: {GetLineCount():N0}, " +
               $"LineIndex: {(lineIndexValid ? "Active" : "Inactive")}";
    }

    // Line indexing 4 fast navigation
    private void BuildLineIndex()
    {
        lineStartPositions = new List<int> { 0 }; // Line 1 always starts at position 0.

        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == '\n')
            {
                lineStartPositions.Add(i + 1);
            }
        }

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
        if (!lineIndexValid)
        {
            BuildLineIndex();
        }

        if (lineStartPositions != null)
        {
            var lineIndex = lineNumber - 1; // to 0-based
            if (lineIndex < 0 || lineIndex >= lineStartPositions.Count)
            {
                return Length;
            }

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
        {
            // fallback
            return GetLine(lineNumber - 1).Length;
        }

        var lineIndex = lineNumber - 1;
        if (lineIndex < 0 || lineIndex >= lineStartPositions.Count)
            return 0;

        var lineStart = lineStartPositions[lineIndex];
        var lineEnd = (lineIndex + 1 < lineStartPositions.Count)
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
            if (buffer[position] == '\n')
            {
                currentLine++;
            }

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

    public void Dispose()
    {
        buffer.Dispose();
        cachedLines = null;
        OnLineChanged = null;
        OnDocumentChanged = null;
        lineStartPositions = null;
    }
}