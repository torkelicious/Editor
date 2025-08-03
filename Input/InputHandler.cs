#region

using Editor.Core;
using Editor.Core.EditorActions;
using Editor.UI;

#endregion

namespace Editor.Input;

public class InputHandler(Document document, EditorState editorState, Viewport viewport, UndoManager undoManager)
{
    private CompoundAction? _insertSession;
    public string lastInputToShow = string.Empty;
    public bool ShouldQuit { get; private set; }

    public void HandleInput()
    {
        var key = Console.ReadKey(true);
        if (editorState.Mode == EditorMode.Normal)
        {
            lastInputToShow = key.Key.ToString().ToLower();
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift)) lastInputToShow = $"Shift+{lastInputToShow.ToUpper()}";
            HandleNormalMode(key);
        }
        else if (editorState.Mode == EditorMode.Insert)
        {
            HandleInsertMode(key);
        }

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
                // Delete character at cursor 
                undoManager.PerformAction(new DeleteAction(document, document.CursorPosition, DeleteDirection.Forward,
                    1));
                break;

            case ConsoleKey.Tab:
            case ConsoleKey.W:
                if ((key.Modifiers & ConsoleModifiers.Shift) == ConsoleModifiers.Shift)
                {
                    // Shift+Tab or Shift+W move left 4 spaces
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
            case ConsoleKey.B:
                // move left 4 spaces
                for (var i = 0; i < 4 && document.CursorPosition > 0; i++)
                    document.MoveCursor(document.CursorPosition - 1);
                break;

            case ConsoleKey.A:
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
                    DeleteCurrentLine();
                break;

            case ConsoleKey.PageUp:
                viewport.ScrollUp(viewport.VisibleLines / 2);
                break;

            case ConsoleKey.PageDown:
                viewport.ScrollDown(viewport.VisibleLines / 2);
                break;

            case ConsoleKey.U:
                undoManager.Undo();
                break;
            case ConsoleKey.R:
                undoManager.Redo();
                break;
            case ConsoleKey.Y:
                YankLine();
                break;
            case ConsoleKey.P:
                Paste();
                break;
            case ConsoleKey.G:
                if (key.Modifiers == ConsoleModifiers.Shift)
                {
                    document.MoveCursor(document.Length); // go to end of buff 
                    return;
                }

                document.MoveCursor(0); // go to start of buff
                break;
            case ConsoleKey.S:
                if (key.Modifiers == ConsoleModifiers.Control) AttemptQuickSave(document);

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
                if (_insertSession is { Count: > 0 })
                {
                    undoManager.PushAction(_insertSession); // Only register, do not execute again!!!
                    _insertSession = null;
                }

                if (document.CursorPosition > 0 && document.CursorPosition <= document.Length &&
                    document[document.CursorPosition - 1] != '\n') document.MoveCursor(document.CursorPosition - 1);
                break;

            case ConsoleKey.Enter:
                BufferInsertWithUndo('\n');
                break;

            case ConsoleKey.Backspace:
                if (document.CursorPosition > 0)
                {
                    document.MoveCursor(document.CursorPosition - 1);
                    BufferDeleteWithUndo();
                }

                break;

            case ConsoleKey.Delete:
                BufferDeleteWithUndo(1, DeleteDirection.Backward);
                break;

            case ConsoleKey.Tab:
                BufferInsertWithUndo("    ");
                break;

            // regular input
            default:
                if (!char.IsControl(key.KeyChar))
                    BufferInsertWithUndo(key.KeyChar);
                break;
        }
    }

    private void BufferInsertWithUndo(char c)
    {
        _insertSession ??= new CompoundAction();
        var action = new InsertAction(document, document.CursorPosition, c);
        action.Do(); // Apply immediately so we can see it visually
        _insertSession.Add(action);
    }

    private void BufferInsertWithUndo(string str)
    {
        _insertSession ??= new CompoundAction();
        var action = new InsertAction(document, document.CursorPosition, str);
        action.Do();
        _insertSession.Add(action);
    }

    private void BufferDeleteWithUndo(int count = 1, DeleteDirection direction = DeleteDirection.Forward)
    {
        _insertSession ??= new CompoundAction();
        var action = new DeleteAction(document, document.CursorPosition, direction, count);
        action.Do();
        _insertSession.Add(action);
    }

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
        document.MoveCursor(lineStart);
        while (lineEnd < document.Length && document[lineEnd] != '\n') lineEnd++;
        if (lineEnd < document.Length && document[lineEnd] == '\n') lineEnd++;
        var deleteLength = lineEnd - lineStart;
        if (deleteLength > 0)
            undoManager.PerformAction(new DeleteAction(document, lineStart, DeleteDirection.Forward, deleteLength));
        MoveCursorLeft(); // still janky
    }

    private void YankLine()
    {
        var (currentLine, _) = document.CurrentLineColumn;
        var lineText = document.GetLine(currentLine - 1);
        editorState.Clipboard
            .Clear(); // we arent using any clipboard history stuff rn so we can just clear it since we always fetch the last item anyways
        editorState.Clipboard.Add(lineText);
    }

    private void Paste()
    {
        if (editorState.Clipboard.Count <= 0) return;
        var action = new InsertAction(document, document.CursorPosition, editorState.Clipboard[^1]);
        undoManager.PerformAction(action);
    }

    private void ClampCursorPosition()
    {
        if (document.CursorPosition > document.Length) document.MoveCursor(document.Length);
    }

    private void AttemptQuickSave(Document document)
    {
        try
        {
            if (document.IsUntitled)
            {
                AnsiConsole.HideCursor();
                AnsiConsole.Clear();
                AnsiConsole.WriteLine("{CYAN}Save File");
                AnsiConsole.WriteLine(new string('─', 20));
                AnsiConsole.WriteLine("");
                AnsiConsole.Write("{WHITE}Enter filename to save as: ");
                AnsiConsole.ShowCursor();
                var filePath = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(filePath))
                {
                    ShowQuickMessage("{RED}No filename entered. Save cancelled.");
                    return;
                }

                if (File.Exists(filePath))
                {
                    AnsiConsole.WriteLine("");
                    AnsiConsole.Write($"{{YELLOW}}File '{filePath}' already exists. Overwrite? (y/N): ");

                    var overwrite = char.ToLower(Console.ReadKey().KeyChar);
                    AnsiConsole.WriteLine("");

                    if (overwrite != 'y')
                    {
                        ShowQuickMessage("{RED}Save cancelled.");
                        return;
                    }
                }

                document.SaveToFile(filePath);
                ShowQuickMessage($"{{GREEN}}File saved successfully: {filePath}");
            }
            else
            {
                document.SaveToFile(document.FilePath);
                ShowQuickMessage($"{{GREEN}}File saved: {document.FilePath}");
            }
        }
        catch (Exception ex)
        {
            ShowQuickMessage($"{{RED}}Error saving file: {ex.Message}");
        }
    }

    private void ShowQuickMessage(string message)
    {
        AnsiConsole.Clear();
        AnsiConsole.HideCursor();
        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine(message);
        AnsiConsole.WriteLine("");
        AnsiConsole.WriteLine("{DARKGRAY}Press any key to continue...");
        Console.ReadKey(true);
        AnsiConsole.Clear();
        StatusBar.forceRedraw = true;
        Initalizer.renderer.MarkAllDirty();
    }
}