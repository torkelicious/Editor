namespace Editor.Core;

/*
 * Main gap buffer class
 */

public class GapBuffer : ITextBuffer
{
    // Core
    private char[] buffer;
    private int gapStart, gapEnd;
    private const int MinGapSize = 32;
    private const int MaxGapSize = 1024;
    
    public char this[int index] { get => index < gapStart ? buffer[index] : buffer[index + (gapEnd - gapStart)]; }
    
    public int Position { get; private set; }
    public int Length => buffer.Length - (gapEnd - gapStart);
    
    // Constructor
    public GapBuffer(int initialSize = 1024)
    {
        buffer = new char[initialSize];
        gapStart = 0;
        gapEnd = initialSize;
        Position = 0;
    }
    // - - - - - - - - - - - - - - - - - - - - - - - //
    
    private void EnsureGapSize(int requiredSize)
    {
        int currentGapSize = gapEnd - gapStart;
        if (currentGapSize < requiredSize)
        {
            Grow(Math.Max(requiredSize, MinGapSize));
        }
    }
    
    // Gap sizing
    private void Grow(int additionalGapSize)
    {
        int newSize = buffer.Length + additionalGapSize;
        char[] newBuffer = new char[newSize];
        
        Array.Copy(buffer, 0, newBuffer, 0, gapStart);
        
        int afterGapLength = buffer.Length - gapEnd;
        int newGapEnd = gapStart + (gapEnd - gapStart) + additionalGapSize;
        Array.Copy(buffer, gapEnd, newBuffer, newGapEnd, afterGapLength);
        
        gapEnd = newGapEnd;
        buffer = newBuffer;
    }
    
    // Operations
    public void Insert(char character)
    {
        EnsureGapSize(1);
        buffer[gapStart++] = character;
        Position++;
    }
    
    public void Insert(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        EnsureGapSize(str.Length);
        for (int i = 0; i < str.Length; i++)
        {
            buffer[gapStart++] = str[i];
        }
        Position += str.Length;
    }

    public void Delete(int count = 1, DeleteDirection direction = DeleteDirection.Forward)
    {
        if (count <= 0) return;
        
        if (direction == DeleteDirection.Forward)
        {
            // Delete forward from cursor position (like 'x' in vim)
            int maxDel = Math.Min(count, Length - Position);
            gapEnd = Math.Min(gapEnd + maxDel, buffer.Length);
            // Position stays the same - cursor doesn't move
        }
        else // Backward
        {
            // Delete backward from cursor position (like backspace)
            if (Position == 0) return;
            int maxDel = Math.Min(count, Position);
            gapStart = Math.Max(gapStart - maxDel, 0);
            Position = Math.Max(Position - maxDel, 0);
        }
    }
    
    public void MoveTo(int position)
    {
        position = Math.Max(0, Math.Min(position, Length));

        if (position < gapStart) // Move left
        {
            int moveCount = gapStart - position;
            Array.Copy(buffer, position, buffer, gapEnd - moveCount, moveCount);
            gapStart = position;
            gapEnd -= moveCount;
        }
        else if (position > gapStart) // Move right
        {
            int moveCount = position - gapStart;
            Array.Copy(buffer, gapEnd, buffer, gapStart, moveCount);
            gapStart = position; 
            gapEnd += moveCount;
        }
        Position = position;
    }

    public void Dispose() {} // For interface compliance meh :/
}