#region

using System.Text;
using Editor.Core;

#endregion

namespace Editor.UI;

public class StatusBar
{
    public static bool useNerdFonts = false;
    private static string fileIcon = "📄";
    private static string recorderIcon = "🔴";
    private static string modifiedIcon = "📝";
    public static string fileTypeNF = string.Empty;
    private static readonly bool showFileType = true;
    public static bool forceRedraw;
    private static string _lastRenderedContent = string.Empty;
    private static int _lastDocumentHash = -1;
    private static readonly StringBuilder _buffer = new();

    public static void Render(Document document, EditorState editorState, int linesPadding, string lastInput = " ")
    {
        AnsiConsole.ResetColor();

        var newContent = BuildStatusBarContent(document, editorState, lastInput);
        var currentDocumentHash = GetDocumentHash(document);

        // Redraw only if content changed or document changed
        if (newContent == _lastRenderedContent && currentDocumentHash == _lastDocumentHash && !forceRedraw) return;

        _lastRenderedContent = newContent;
        _lastDocumentHash = currentDocumentHash;

        AnsiConsole.HideCursor();

        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding);
        AnsiConsole.Write(newContent);

        AnsiConsole.ShowCursor();
        AnsiConsole.ResetColor();
        forceRedraw = false;
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
        var modeColor = editorState.Mode switch
        {
            EditorMode.Normal => "BG_GREEN",
            EditorMode.Insert => "BG_YELLOW",
            EditorMode.Visual => "BG_CYAN"
        };

        var modeText = $"{{BOLD}} {editorState.Mode.ToString().ToUpper()} {{RESET}}";
        _buffer.Append($"{{{modeColor}}}{{BLACK}}{modeText}{{RESET}}");
        var positionText = $" {editorState.CursorLine + 1}:{editorState.CursorColumn + 1} ";
        _buffer.Append($"{{BG_WHITE}}{{BLACK}}{positionText}{{RESET}}");

        if (document.IsDirty)
            _buffer.Append($"{{BG_DARKBLUE}}{{WHITE}} {modifiedIcon} MODIFIED  {{RESET}}");

        if (!document.IsUntitled)
        {
            string fileEx = String.Empty;
            if (showFileType)
            {
                fileEx = document.FileExtensionReadable;
            }
            
            var displayPath = document.FilePath!.Length > 50
                ? "..." + document.FilePath[^47..]
                : document.FilePath;
            _buffer.Append(
                $"{{BG_DARKCYAN}}{{BOLD}}{{BLACK}} {fileIcon} {fileEx} {{RESET}}{{BG_DARKCYAN}}{{WHITE}}{displayPath}{{RESET}}");
        }

        if (document.IsUntitled)
            _buffer.Append("{BG_DARKMAGENTA}{WHITE}  ( NEW FILE )  {RESET}");

        if (document.showDebugInfo)
            _buffer.Append($"{{BG_DARKBLUE}}{{WHITE}}{document.GetPerformanceInfo()}{{RESET}}");

        _buffer.AppendLine();

        var helpText = editorState.Mode switch
        {
            EditorMode.Normal =>
                "HJKL:Move | [I]nsert | [V]isual | X:Del | [D]elLine | [Y]ankLine/[P]aste | [U]ndo/[R]edo | [Q]uit",
            EditorMode.Insert => "Type to insert | Arrows:Move | ESC:Normal",
            EditorMode.Visual => "HJKL:Select | [Y]ank Selection | D/X: Delete Selection | ESC:Normal",
            _ => "Unknown mode"
        };
        var rec = $"{{RED}}{recorderIcon}{{BLACK}}[{lastInput}]";
        var maxHelpLength = Console.WindowWidth - rec.Length;

        if (helpText.Length > maxHelpLength)
            helpText = string.Concat(helpText.AsSpan(0, maxHelpLength - 3), "...");

        // Build line
        var helpLine = $"{{BG_WHITE}}{{BOLD}}{{BLACK}}{helpText}"; // this dosent show properly on some terminals
        var spacesNeeded = Console.WindowWidth - helpText.Length - rec.Length;
        helpLine += new string(' ', Math.Max(0, spacesNeeded));
        helpLine +=
            $"{{RESET}}{{BG_WHITE}}{{BLACK}}{{ITALIC}}{rec}{{RESET}}"; // this works but holy is this a mess of a string
        _buffer.Append(helpLine);
        return _buffer.ToString();
    }

    public static void setIcons()
    {
        if (useNerdFonts)
        {
            fileIcon = fileTypeNF;
            recorderIcon = "󰑋"; // idk what to put here
            modifiedIcon = "󰳼";
        }
        else
        {
            fileIcon = "📄";
            recorderIcon = "🔴";
        }
    }
}