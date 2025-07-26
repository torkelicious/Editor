using Editor.Core;
using Editor.UI;

namespace Editor.Input;

public class InputHandler(Document document, EditorState editorState, Viewport viewport)
{
    public bool ShouldQuit { get; private set; }

    public void HandleInput()
    {
        var key = Console.ReadKey(true);

        if (editorState.Mode == EditorMode.Normal)
            HandleNormalMode(key);
        else if (editorState.Mode == EditorMode.Insert) HandleInsertMode(key);

        editorState.UpdateFromDocument(document);
        ClampCursorPosition();
    }

    private void HandleNormalMode(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            // Movement
            case ConsoleKey.J:
            case ConsoleKey.DownArrow:
                MoveCursorDown();
                break;

            case ConsoleKey.K:
            case ConsoleKey.UpArrow:
                MoveCursorUp();
                break;

            case ConsoleKey.H:
            case ConsoleKey.LeftArrow:
                MoveCursorLeft();
                break;

            case ConsoleKey.L:
            case ConsoleKey.RightArrow:
            case ConsoleKey.Spacebar:
                MoveCursorRight();
                break;

            case ConsoleKey.I:
                editorState.Mode = EditorMode.Insert;
                break;

            case ConsoleKey.Q:
                ShouldQuit = true;
                break;

            case ConsoleKey.X:
                // Delete character at cursor (vim 'x')
                document.Delete();
                break;

            // Tab/Shift-Tab indent in normal mode
            case ConsoleKey.Tab:
                if ((key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
                {
                    // Shift+Tab  move left 4 spaces
                    for (var i = 0; i < 4 && document.CursorPosition > 0; i++)
                        document.MoveCursor(document.CursorPosition - 1);
                }
                else
                {
                    // move right 4 spaces or to next tab stop
                    var currentCol = document.CurrentLineColumn.column;
                    var nextTabStop = ((currentCol - 1) / 4 + 1) * 4 + 1;
                    var targetPos = document.CursorPosition + (nextTabStop - currentCol);
                    document.MoveCursor(Math.Min(targetPos, document.Length));
                }

                break;

            case ConsoleKey.A:
                // insert after cursor
                MoveCursorRight();
                editorState.Mode = EditorMode.Insert;
                break;

            case ConsoleKey.O:
                // open new line below
                MoveToEndOfLine();
                document.Insert('\n');
                editorState.Mode = EditorMode.Insert;
                break;

            case ConsoleKey.D:
                // delete line with d
                if (key.Modifiers == ConsoleModifiers.None)
                    // Simple delete for now...
                    DeleteCurrentLine();

                break;

            // Scrolling
            case ConsoleKey.PageUp:
                viewport.ScrollUp(viewport.VisibleLines / 2);
                break;

            case ConsoleKey.PageDown:
                viewport.ScrollDown(viewport.VisibleLines / 2);
                break;
        }
    }

    private void HandleInsertMode(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            // Movement 
            case ConsoleKey.UpArrow:
                MoveCursorUp();
                break;

            case ConsoleKey.DownArrow:
                MoveCursorDown();
                break;

            case ConsoleKey.LeftArrow:
                MoveCursorLeft();
                break;

            case ConsoleKey.RightArrow:
                MoveCursorRight();
                break;

            case ConsoleKey.Escape:
                editorState.Mode = EditorMode.Normal;
                if (document.CursorPosition > 0) document.MoveCursor(document.CursorPosition - 1);

                break;

            // text operations
            case ConsoleKey.Enter:
                document.Insert('\n');
                break;

            case ConsoleKey.Backspace:
                document.Delete(1, DeleteDirection.Backward);
                break;

            case ConsoleKey.Delete:
                document.Delete();
                break;

            case ConsoleKey.Tab:
                /* insert 4 spaces
                 * maybe using \t could be a good idea instead, something i must test.
                 */
                document.Insert("    ");
                break;

            // regular input
            default:
                if (!char.IsControl(key.KeyChar)) document.Insert(key.KeyChar);

                break;
        }
    }

    // fast movement w the indexing
    private void MoveCursorUp()
    {
        var (currentLine, currentCol) = document.CurrentLineColumn;
        if (currentLine > 1)
        {
            var targetPos = document.GetPositionFromLine(currentLine - 1, currentCol);
            document.MoveCursor(targetPos);
        }
    }

    private void MoveCursorDown()
    {
        var (currentLine, currentCol) = document.CurrentLineColumn;
        var totalLines = document.GetLineCount();
        if (currentLine < totalLines)
        {
            var targetPos = document.GetPositionFromLine(currentLine + 1, currentCol);
            document.MoveCursor(targetPos);
        }
    }

    private void MoveCursorLeft()
    {
        if (document.CursorPosition > 0) document.MoveCursor(document.CursorPosition - 1);
    }

    private void MoveCursorRight()
    {
        if (document.CursorPosition < document.Length) document.MoveCursor(document.CursorPosition + 1);
    }

    private void MoveToEndOfLine()
    {
        var (currentLine, _) = document.CurrentLineColumn;
        var currentPos = document.GetPositionFromLine(currentLine);

        while (currentPos < document.Length && document[currentPos] != '\n') currentPos++;

        document.MoveCursor(currentPos);
    }

    private void DeleteCurrentLine()
    {
        var (currentLine, _) = document.CurrentLineColumn;
        var lineStart = document.GetPositionFromLine(currentLine);
        var lineEnd = lineStart;

        while (lineEnd < document.Length && document[lineEnd] != '\n') lineEnd++;

        if (lineEnd < document.Length && document[lineEnd] == '\n') lineEnd++;

        document.MoveCursor(lineStart);
        var deleteLength = lineEnd - lineStart;
        if (deleteLength > 0) document.Delete(deleteLength);
    }

    private void ClampCursorPosition()
    {
        if (document.CursorPosition > document.Length) document.MoveCursor(document.Length);
    }
}