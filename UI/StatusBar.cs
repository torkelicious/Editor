#region

using System.Text;
using Editor.Core;

#endregion

namespace Editor.UI;

public class StatusBar
{
    private static readonly bool useNerdFonts = Config.Options is { UseNerdFonts: true };
    private static string? fileIcon = "📄";
    private static string recorderIcon = "🔴";
    private static string modifiedIcon = "📝";
    public static string? fileTypeNF = string.Empty;
    private static readonly bool showFileType = Config.Options is { StatusBarShowFileType: true };
    private static string _lastRenderedContent = string.Empty;
    private static int _lastDocumentHash = -1;
    private static readonly StringBuilder _buffer = new();

    public static void Render(Document document, EditorState editorState, int linesPadding, string lastInput = " ")
    {
        AnsiConsole.ResetColor();

        var newContent = BuildStatusBarContent(document, editorState, lastInput);
        var currentDocumentHash = GetDocumentHash(document);

        //  redraw only if content changed or document changed
        if (newContent == _lastRenderedContent && currentDocumentHash == _lastDocumentHash) return;

        _lastRenderedContent = newContent;
        _lastDocumentHash = currentDocumentHash;

        AnsiConsole.HideCursor();

        var width = Math.Max(1, Console.WindowWidth);
        var height = Math.Max(1, Console.WindowHeight);
        var statusBarStartY = Math.Max(0, height - linesPadding);

        // overwrite previous content with whitespace to avoid old text showing 
        if (statusBarStartY < height)
        {
            Console.SetCursorPosition(0, statusBarStartY);
            Console.Write(new string(' ', width));
        }

        if (statusBarStartY + 1 < height)
        {
            Console.SetCursorPosition(0, statusBarStartY + 1);
            Console.Write(new string(' ', width));
        }

        // draw new bar
        if (statusBarStartY < height)
        {
            Console.SetCursorPosition(0, statusBarStartY);
            AnsiConsole.Write(newContent);
        }

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

        var width = Math.Max(1, Console.WindowWidth);

        // separator
        _buffer.AppendLine("{DARKGRAY}" + new string('─', width));

        // Mode and position
        var modeColor = editorState.Mode switch
        {
            EditorMode.Normal => "BG_GREEN",
            EditorMode.Insert => "BG_YELLOW",
            EditorMode.Visual => "BG_CYAN",
            _ => "BG_WHITE"
        };

        var modeText = $"{{BOLD}} {editorState.Mode.ToString().ToUpper()} {{RESET}}";
        _buffer.Append($"{{{modeColor}}}{{BLACK}}{modeText}{{RESET}}");

        var positionText = $" {editorState.CursorLine + 1}:{editorState.CursorColumn + 1} ";
        _buffer.Append($"{{BG_WHITE}}{{BLACK}}{positionText}{{RESET}}");

        if (document.IsDirty)
            _buffer.Append($"{{BG_DARKBLUE}}{{WHITE}} {modifiedIcon} MODIFIED  {{RESET}}");

        if (!document.IsUntitled)
        {
            var fileEx = showFileType ? document.FileExtensionReadable : string.Empty;

            // file path truncation calculation
            var usedSpace = CalculateDisplayLength(editorState.Mode.ToString().ToUpper()) +
                            CalculateDisplayLength(positionText) +
                            (document.IsDirty ? CalculateDisplayLength($" {modifiedIcon} MODIFIED  ") : 0) +
                            CalculateDisplayLength($" {fileIcon} {fileEx} ");

            var reservedForHelp = Math.Min(40, width / 2);
            var maxPathLength = Math.Max(0, width - usedSpace - reservedForHelp);

            var fullPath = document.FilePath ?? string.Empty;
            string displayPath;

            if (fullPath.Length > maxPathLength && maxPathLength >= 4)
                displayPath = "..." + fullPath[^Math.Max(1, maxPathLength - 3)..];
            else if (maxPathLength == 0)
                displayPath = string.Empty;
            else
                displayPath = fullPath;

            _buffer.Append(
                $"{{BG_DARKCYAN}}{{BOLD}}{{BLACK}} {fileIcon} {fileEx} {{RESET}}{{BG_DARKCYAN}}{{WHITE}}{displayPath}{{RESET}}");
        }
        else
        {
            _buffer.Append("{BG_DARKMAGENTA}{WHITE}  ( NEW FILE )  {RESET}");
        }

        if (document.showDebugInfo)
            _buffer.Append($"{{BG_DARKBLUE}}{{WHITE}}{document.GetPerformanceInfo()}{{RESET}}");

        _buffer.AppendLine();

        // Help row
        var helpText = editorState.Mode switch
        {
            EditorMode.Normal =>
                "HJKL:Move | [I]nsert | [V]isual | X:Del | [D]elLine | [Y]ankLine/[P]aste | [U]ndo/[R]edo | [Q]uit",
            EditorMode.Insert => "Type to insert | Arrows:Move | ESC:Normal",
            EditorMode.Visual => "HJKL:Select | [Y]ank Selection | D/X: Delete Selection | ESC:Normal",
            _ => "Unknown mode"
        };

        var recText = $"{recorderIcon}[{lastInput}]";
        var recDisplayLength = CalculateDisplayLength(recText);
        var maxHelpLength = Math.Max(0, width - recDisplayLength);

        if (helpText.Length > maxHelpLength && maxHelpLength >= 4)
            helpText = string.Concat(helpText.AsSpan(0, maxHelpLength - 3), "...");

        var spacesNeeded = Math.Max(0, width - CalculateDisplayLength(helpText) - recDisplayLength);

        _buffer.Append($"{{BG_WHITE}}{{BOLD}}{{BLACK}}{helpText}");
        _buffer.Append(new string(' ', spacesNeeded));
        _buffer.Append($"{{RESET}}{{BG_WHITE}}{{BLACK}}{{ITALIC}}{{RED}}{recorderIcon}{{BLACK}}[{lastInput}]{{RESET}}");

        return _buffer.ToString();
    }

    private static int CalculateDisplayLength(string text)
    {
        // count 2 for surrogate pairs (emojis)
        var displayLength = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                displayLength += 2;
                i++;
            }
            else
            {
                displayLength += 1;
            }
        }

        return displayLength;
    }

    public static void setIcons()
    {
        if (useNerdFonts)
        {
            fileIcon = fileTypeNF;
            recorderIcon = "󰑋";
            modifiedIcon = "󰳼";
        }
        else
        {
            fileIcon = "📄";
            recorderIcon = "🔴";
            modifiedIcon = "📝";
        }
    }
}