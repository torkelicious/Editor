#region

using System.Text;
using Editor.Core;

#endregion

namespace Editor.UI;

public class StatusBar
{
    private static string _lastRenderedContent = string.Empty;
    private static int _lastDocumentHash = -1;
    private static readonly StringBuilder _buffer = new();

    public static void Render(Document document, EditorState editorState, int linesPadding, string lastInput = " ")
    {
        var newContent = BuildStatusBarContent(document, editorState, lastInput);
        var currentDocumentHash = GetDocumentHash(document);

        // Redraw only if content changed or document changed
        if (newContent == _lastRenderedContent && currentDocumentHash == _lastDocumentHash) return;

        _lastRenderedContent = newContent;
        _lastDocumentHash = currentDocumentHash;

        AnsiConsole.HideCursor();

        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding);
        AnsiConsole.Write(newContent);

        AnsiConsole.ShowCursor();
        AnsiConsole.ResetColor();
    }

    private static int GetDocumentHash(Document document)
    {
        var lineCount = document.GetLineCount();
        var totalLength = 0;
        for (var i = 0; i < lineCount; i++) totalLength += document.GetLine(i).Length;
        return HashCode.Combine(
            lineCount,
            totalLength,
            document.IsDirty
        );
    }

    private static string BuildStatusBarContent(Document document, EditorState editorState, string lastInput)
    {
        _buffer.Clear();

        // Separator 
        _buffer.AppendLine("{DARKGRAY}" + new string('─', Console.WindowWidth));

        // Mode and position 
        var modeColor = editorState.Mode == EditorMode.Normal ? "BG_GREEN" : "BG_YELLOW";
        var modeText = $" {editorState.Mode.ToString().ToUpper()} ";
        _buffer.Append($"{{{modeColor}}}{{BLACK}}{modeText}{{RESET}}");

        var positionText = $" {editorState.CursorLine + 1}:{editorState.CursorColumn + 1} ";
        _buffer.Append($"{{BG_WHITE}}{{BLACK}}{positionText}{{RESET}}");

        if (document.IsDirty)
            _buffer.Append("{BG_DARKBLUE}{WHITE}  MODIFIED  {RESET}");

        if (!document.IsUntitled)
        {
            var displayPath = document.FilePath!.Length > 50
                ? "..." + document.FilePath[^47..]
                : document.FilePath;
            _buffer.Append($"{{BG_DARKCYAN}}{{WHITE}}  📄 {displayPath}  {{RESET}}");
        }

        if (document.IsUntitled)
            _buffer.Append("{BG_DARKMAGENTA}{WHITE}  ( NEW FILE )  {RESET}");

        if (document.showDebugInfo)
            _buffer.Append($"{{BG_DARKBLUE}}{{WHITE}}{document.GetPerformanceInfo()}{{RESET}}");

        _buffer.AppendLine();

        // Help 
        var helpText =
            "HJKL/Arrows: Move || Q: Quit (NORMAL) || I: INSERT mode || ESC: NORMAL mode || X: Delete (NORMAL) ||";
        var rec = $"🔴[{lastInput}]";
        var maxHelpLength = Console.WindowWidth - rec.Length;

        if (helpText.Length > maxHelpLength)
            helpText = string.Concat(helpText.AsSpan(0, maxHelpLength - 3), "...");

        // Build line
        var helpLine = $"{{BOLD}}{{BG_WHITE}}{{BLACK}}{helpText}";
        var spacesNeeded = Console.WindowWidth - helpText.Length - rec.Length;
        helpLine += new string(' ', Math.Max(0, spacesNeeded));
        helpLine += $"{rec}{{RESET}}";

        _buffer.Append(helpLine);

        return _buffer.ToString();
    }
}