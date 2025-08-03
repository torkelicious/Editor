using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Editor.UI;

public static class AnsiConsole
{
    public enum AnsiColor
    {
        Black = 30,
        DarkRed = 31,
        DarkGreen = 32,
        DarkYellow = 33,
        DarkBlue = 34,
        DarkMagenta = 35,
        DarkCyan = 36,
        Gray = 37,
        DarkGray = 90,
        Red = 91,
        Green = 92,
        Yellow = 93,
        Blue = 94,
        Magenta = 95,
        Cyan = 96,
        White = 97
    }

    public enum CursorShape
    {
        BlinkBlock = 1,
        SteadyBlock = 2,
        BlinkUnderline = 3,
        SteadyUnderline = 4,
        BlinkBar = 5,
        SteadyBar = 6
    }


    private static readonly object _lockObject = new();
    private static readonly ConcurrentDictionary<string, Regex> _regexCache = new();

    private static readonly Dictionary<AnsiColor, int> BackgroundColorMap =
        Enum.GetValues<AnsiColor>()
            .ToDictionary(color => color, GetBackgroundCode);

    private static readonly Dictionary<string, string> FormattingCodes = CreateFormattingCodes();

    private static AnsiColor _fg = AnsiColor.White;
    private static AnsiColor _bg = AnsiColor.Black;

    public static AnsiColor ForegroundColor
    {
        get => _fg;
        set => SetForegroundColor(value);
    }

    public static AnsiColor BackgroundColor
    {
        get => _bg;
        set => SetBackgroundColor(value);
    }

    private static int GetBackgroundCode(AnsiColor color)
    {
        return (int)color switch
        {
            >= 30 and <= 37 => (int)color + 10, // Standard colors: 30-37 -> 40-47
            >= 90 and <= 97 => (int)color + 10, // Bright colors: 90-97 -> 100-107
            _ => throw new ArgumentOutOfRangeException(nameof(color))
        };
    }

    private static Dictionary<string, string> CreateFormattingCodes()
    {
        var codes = new Dictionary<string, string>
        {
            { "RESET", "\x1b[0m" },
            { "BOLD", "\x1b[1m" },
            { "UNDERLINE", "\x1b[4m" },
            { "CLEAR", "\x1b[2J\x1b[H" }
        };

        // foreground colors from enum
        foreach (var color in Enum.GetValues<AnsiColor>())
        {
            codes[color.ToString().ToUpper()] = $"\x1b[{(int)color}m";
            codes[$"BG_{color.ToString().ToUpper()}"] = $"\x1b[{GetBackgroundCode(color)}m";
        }

        return codes;
    }

    public static void ResetColor()
    {
        lock (_lockObject)
        {
            _fg = AnsiColor.White;
            _bg = AnsiColor.Black;
            Console.Write("\x1b[0m");
        }
    }

    private static string Format(string input)
    {
        foreach (var tag in FormattingCodes) input = ReplaceIgnoreCase(input, "{" + tag.Key + "}", tag.Value);

        return input;
    }

    private static string ReplaceIgnoreCase(string input, string search, string replacement)
    {
        var regex = _regexCache.GetOrAdd(search, s =>
            new Regex(Regex.Escape(s), RegexOptions.IgnoreCase | RegexOptions.Compiled));

        return regex.Replace(input, replacement.Replace("$", "$$"));
    }

    public static void Write(string input)
    {
        Console.Write(Format(input));
    }

    public static void WriteLine(string input)
    {
        Console.WriteLine(Format(input));
    }

    public static void Clear()
    {
        Console.Write("\x1b[2J\x1b[H");
    }

    public static void SetCursorShape(CursorShape shape)
    {
        Console.Write($"\x1b[{(int)shape} q");
    }

    public static void ResetCursor()
    {
        SetCursorShape(CursorShape.SteadyBlock);
    }

    public static void SetForegroundColor(AnsiColor color)
    {
        lock (_lockObject)
        {
            _fg = color;
            Console.Write($"\x1b[{(int)color}m");
        }
    }

    public static void SetBackgroundColor(AnsiColor color)
    {
        lock (_lockObject)
        {
            _bg = color;
            Console.Write($"\x1b[{BackgroundColorMap[color]}m");
        }
    }

    public static void HideCursor()
    {
        Console.Write("\x1b[?25l");
    }

    public static void ShowCursor()
    {
        Console.Write("\x1b[?25h");
    }
}