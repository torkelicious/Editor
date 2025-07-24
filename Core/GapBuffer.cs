namespace Editor.Core;

/*
 * Main gap buffer class
 */

public class GapBuffer
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
   private void Grow(int additionalGapSz)
   {
       int newSize = buffer.Length + additionalGapSz;
       char[] newBuffer = new char[newSize];
       Array.Copy(buffer, 0, newBuffer, 0, gapStart);
       int afterGapLen = buffer.Length - gapEnd;  
       Array.Copy(buffer, gapEnd, newBuffer, gapStart + (gapEnd - gapStart) + additionalGapSz, afterGapLen);
       gapEnd = gapStart + (gapEnd - gapStart) + additionalGapSz;
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
       Position+= str.Length;
   }

   public void Delete(int count = 1)
   {
       if (count <= 0) return;
       int maxDel = Math.Min(count, Length - Position );
       gapEnd = Math.Min(gapEnd + maxDel, buffer.Length);
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

}
