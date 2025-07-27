namespace Editor.Core;

/*
 * Main gap buffer class
 */

public class GapBuffer : ITextBuffer
{
    private const int MinGapSize = 32;
    private const int MaxGapSize = 1024; // TODO: use this when paging is implemented !!
    
    // Core
    private char[] buffer;
    private int gapStart, gapEnd;

    // Constructor
    public GapBuffer(int initialSize = 1024)
    {
        buffer = new char[initialSize];
        gapStart = 0;
        gapEnd = initialSize;
        Position = 0;
    }

    public char this[int index] => index < gapStart ? buffer[index] : buffer[index + (gapEnd - gapStart)];

    public int Position { get; private set; }
    public int Length => buffer.Length - (gapEnd - gapStart);

    // Operations
    public void Insert(char character)
    {
        MoveTo(Position);
        EnsureGapSize(1);
        buffer[gapStart++] = character;
        Position++;
    }

    public void Insert(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        MoveTo(Position);
        EnsureGapSize(str.Length);
        foreach (var t in str)
            buffer[gapStart++] = t;

        Position += str.Length;
    }

    public void Delete(int count = 1, DeleteDirection direction = DeleteDirection.Forward)
    {
        if (count <= 0) return;

        if (direction == DeleteDirection.Forward)
        {
            var maxDel = Math.Min(count, Length - Position);
            gapEnd = Math.Min(gapEnd + maxDel, buffer.Length);
        }
        else // Backward
        {
            // (backspace)
            if (Position == 0) return;
            var maxDel = Math.Min(count, Position);
            gapStart = Math.Max(gapStart - maxDel, 0);
            Position = Math.Max(Position - maxDel, 0);
        }
    }

    public void MoveTo(int position)
    {
        position = Math.Max(0, Math.Min(position, Length));

        // Move left
        if (position < gapStart)
        {
            var moveCount = gapStart - position;
            Array.Copy(buffer, position, buffer, gapEnd - moveCount, moveCount);
            gapStart -= moveCount;
            gapEnd -= moveCount;
        }
        // Move right
        else if (position > gapStart)
        {
            var moveCount = position - gapStart;
            Array.Copy(buffer, gapEnd, buffer, gapStart, moveCount);
            gapStart += moveCount;
            gapEnd += moveCount;
        }

        Position = position;
    }

    public void Dispose()
    { /*
    For interface compliance, we already manage this stuff via our char array,
    this is a remnant of when i was experimenting with multiple types of buffering via the interface
    keeping it for interface compliance incase i reimplement a new buffer in the future
    but it is safe to remove from both this class and the interface as long as the document class is updated.
    */ }
    

    // Gap sizing
    private void EnsureGapSize(int requiredSize)
    {
        var currentGapSize = gapEnd - gapStart;
        if (currentGapSize < requiredSize) Grow(Math.Max(requiredSize, MinGapSize));
    }

    private void Grow(int additionalGapSize)
    {
        var newSize = buffer.Length + additionalGapSize;
        var newBuffer = new char[newSize];

        Array.Copy(buffer, 0, newBuffer, 0, gapStart);

        var afterGapLength = buffer.Length - gapEnd;
        var newGapEnd = gapStart + (gapEnd - gapStart) + additionalGapSize;
        Array.Copy(buffer, gapEnd, newBuffer, newGapEnd, afterGapLength);

        gapEnd = newGapEnd;
        buffer = newBuffer;
    }
}