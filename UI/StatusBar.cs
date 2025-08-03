#region

using Editor.Core;

#endregion

namespace Editor.UI;

public class StatusBar
{
    public static void Render(Document document, EditorState editorState, int linesPadding, string lastInput = " ")
    {
        DrawSeparator(linesPadding);
        DrawModeAndPosition(document, editorState, linesPadding);
        DrawHelpLine(linesPadding, lastInput);
        AnsiConsole.ResetColor();
    }

    private static void DrawSeparator(int linesPadding)
    {
        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding);
        AnsiConsole.Write("{DARKGRAY}" + new string('─', Console.WindowWidth));
    }

    private static void DrawModeAndPosition(Document document, EditorState editorState, int linesPadding)
    {
        var y = Console.WindowHeight - linesPadding + 1;
        Console.SetCursorPosition(0, y);

        // Mode indicator
        var modeColor = editorState.Mode == EditorMode.Normal ? "BG_GREEN" : "BG_YELLOW";
        var modeText = $" {editorState.Mode.ToString().ToUpper()} ";
        AnsiConsole.Write($"{{{modeColor}}}{{BLACK}}{modeText}{{RESET}}");

        // Position
        var positionText = $" {editorState.CursorLine + 1}:{editorState.CursorColumn + 1} ";
        AnsiConsole.Write($"{{BG_WHITE}}{{BLACK}}{positionText}{{RESET}}");

        // File status
        if (document.IsDirty) AnsiConsole.Write("{BG_DARKBLUE}{WHITE}  MODIFIED  {RESET}");

        if (!document.IsUntitled)
        {
            var displayPath = document.FilePath!.Length > 50
                ? "..." + document.FilePath[^47..]
                : document.FilePath;
            AnsiConsole.Write($"{{BG_DARKCYAN}}{{WHITE}}  📄 {displayPath}  {{RESET}}");
        }

        if (document.IsUntitled) AnsiConsole.Write("{BG_DARKMAGENTA}{WHITE}  ( NEW FILE )  {RESET}");

        // Debug
        if (document.showDebugInfo)
            AnsiConsole.Write($"{{BG_DARKBLUE}}{{WHITE}}{document.GetPerformanceInfo()}{{RESET}}");
    }

    private static void DrawHelpLine(int linesPadding, string lastInput = " ")
    {
        var y = Console.WindowHeight - linesPadding + 2;
        Console.SetCursorPosition(0, y);

        // Clear the entire line first
        AnsiConsole.Write("{BG_WHITE}" + new string(' ', Console.WindowWidth) + "{RESET}");
        Console.SetCursorPosition(0, y);

        var helpText =
            "HJKL/Arrows: Move || Q: Quit (NORMAL) || I: INSERT mode || ESC: NORMAL mode || X: Delete (NORMAL) ||";

        var rec = recorder(lastInput);

        var maxHelpLength = Console.WindowWidth - rec.Length;
        if (helpText.Length > maxHelpLength)
            helpText = string.Concat(helpText.AsSpan(0, maxHelpLength - 3), "...");

        AnsiConsole.Write($"{{BOLD}}{{BG_WHITE}}{{BLACK}}{helpText}{{RESET}}");

        var recPos = Console.WindowWidth - rec.Length;
        Console.SetCursorPosition(recPos, y);
        AnsiConsole.Write($"{{BG_WHITE}}{{BLACK}}{rec}{{RESET}}");
    }

    private static string recorder(string lastInput = "")
    {
        return $"🔴[{lastInput}]";
    }
}