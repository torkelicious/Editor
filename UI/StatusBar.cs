using Editor.Core;

namespace Editor.UI;

public class StatusBar
{
    public void Render(Document document, EditorState editorState, int linesPadding)
    {
        DrawSeparator(linesPadding);
        DrawModeAndPosition(document, editorState, linesPadding);
        DrawHelpLine(linesPadding);
        ResetColors();
    }

    private void DrawSeparator(int linesPadding)
    {
        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('─', Console.WindowWidth));
    }

    private void DrawModeAndPosition(Document document, EditorState editorState, int linesPadding)
    {
        var y = Console.WindowHeight - linesPadding + 1;
        Console.SetCursorPosition(0, y);

        var modeColor = editorState.Mode == EditorMode.Normal
            ? ConsoleColor.Green
            : ConsoleColor.Yellow;

        Console.BackgroundColor = modeColor;
        Console.ForegroundColor = ConsoleColor.Black;
        var modeText = $" {editorState.Mode.ToString().ToUpper()} ";
        Console.Write(modeText);

        // Pos
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        var positionText = $" {editorState.CursorLine + 1}:{editorState.CursorColumn + 1} ";
        Console.Write(positionText);

        // File status 
        if (document.IsDirty)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  MODIFIED  ");
        }

        if (!document.IsUntitled)
        {
            Console.BackgroundColor = ConsoleColor.DarkCyan;
            Console.ForegroundColor = ConsoleColor.White;
            var displayPath = document.FilePath!.Length > 50
                ? "..." + document.FilePath.Substring(document.FilePath.Length - 47)
                : document.FilePath;
            Console.Write($"  📄 {displayPath}  ");
        }

        if (document.IsUntitled)
        {
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  ( NEW FILE )  ");
        }

        // Debug 
        if (document.showDebugInfo)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(document.GetPerformanceInfo());
        }
    }

    private void DrawHelpLine(int linesPadding)
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;

        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding + 2);
        string helpText =
            "HJKL/Arrows: Move || Q: Quit (NORMAL) || I: INSERT mode || ESC: NORMAL mode || X: Delete (NORMAL)";

        // Truncate 
        if (helpText.Length > Console.WindowWidth) helpText = helpText.Substring(0, Console.WindowWidth - 3) + "...";

        Console.Write(helpText.PadRight(Console.WindowWidth));
    }

    private void ResetColors()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
    }
}