#region

using Editor.Core;

#endregion

namespace Editor.UI;

public static class StartupMenu
{
    private const string Version = "0.0.3";

    private const string Logo =
        """
          __                          __      _  _         
         / /____ ____  ___ ____ ___  / /_____(_)(_)______ _
        / __/ _ `/ _ \/ _ `/ -_) _ \/ __/ __/ _ \/ __/ _ `/
        \__/\_,_/_//_/\_, /\__/_//_/\__/_/  \___/_/  \_,_/ 
                     /___/                                 
        """;

    private const string LogoProgramName = "TangentRöra";
    private const string License = "SPDX-License-Identifier: GPL-3.0-or-later";
    private const string Author = "torkelicious";

    private static readonly string LicenseText =
        $@"
This software is Licensed under: {License}
Copyright © {DateTime.Now.Year} {Author} 
";


    public static EditorStartupResult ShowMenu(string[] args)
    {
        // cli arguments first
        return args.Length > 0 ? HandleCommandLineArgs(args) : ShowInteractiveMenu();
    }

    private static EditorStartupResult HandleCommandLineArgs(string[] args)
    {
        var filePath = args[0].Trim();
        if (filePath == string.Empty) return ShowInteractiveMenu();

        if (File.Exists(filePath))
            return new EditorStartupResult
            {
                Document = new Document(filePath),
                ShouldStartEditor = true,
                IsNewFile = false
            };

        try
        {
            using (File.Create(filePath))
            { }

            return new EditorStartupResult
            {
                Document = new Document(filePath),
                ShouldStartEditor = true,
                IsNewFile = true
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Could not create file: {ex.Message}");
            Console.WriteLine("Going to menu...");
            Thread.Sleep(1500);
            return ShowInteractiveMenu();
        }
    }

    private static EditorStartupResult ShowInteractiveMenu()
    {
        while (true)
        {
            DrawMenu();
            var input = char.ToLower(Console.ReadKey(true).KeyChar);
            switch (input)
            {
                case 'n':
                    return CreateNewFile();

                case 'o':
                    return OpenExistingFile();

                case 'q':
                    return new EditorStartupResult { ShouldStartEditor = false };

                default:
                    ShowInvalidOption();
                    break;
            }
        }
    }

    private static void DrawMenu()
    {
        Console.Clear();

        // Reset colors
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();

        var separator = new string('─', Math.Min(Console.WindowWidth, 60));
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Console.WriteLine(Logo);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"             {LogoProgramName} [v{Version}]");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(LicenseText);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(separator);
        Console.WriteLine();

        Console.WriteLine("What would you like to do?");
        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  [N] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Create a new file");

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("  [O] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Open an existing file");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  [Q] ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Quit");

        Console.WriteLine();
        Console.WriteLine(separator);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(
            $"""
             // Editor Controls:
                 (NORMAL:) 
              *   HJKL to move || I: Insert mode || A: Append || X: Delete || D: Delete Line || O: Insert into NewLine || Q: Quit
              *   Y: Yank line || P: Paste yanked line
              *   G: to go to start of buffer || SHIFT+G: to go to end of buffer 
              *   Undo/Redo with U / R
              *   Navigate quickly with TAB and SHIFT+TAB (OR W and B)

                 (INSERT:)
              *   ARROW KEYS to move
              *   ESCAPE: return to NORMAL mode
             {separator}
             """);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.Write("Enter your choice: ");
    } // so many colors ^o^

    private static EditorStartupResult CreateNewFile()
    {
        return new EditorStartupResult
        {
            Document = new Document(), // Empty document
            ShouldStartEditor = true,
            IsNewFile = true
        };
    }

    private static EditorStartupResult OpenExistingFile()
    {
        Console.Clear();
        Console.WriteLine("Open File");
        Console.WriteLine(new string('─', 20));
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("(Press Ctrl+C to cancel)");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Enter the path to the file: ");

        var filePath = string.Empty;
        try
        {
            filePath = Console.ReadLine()?.Trim();
        }
        catch (InvalidOperationException)
        {
            // User pressed Ctrl+C
            return ShowInteractiveMenu();
        }

        if (string.IsNullOrEmpty(filePath))
        {
            ShowError("No file path entered!");
            return ShowInteractiveMenu();
        }

        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"File '{filePath}' doesn't exist. Create it? (y/N): ");
                Console.ForegroundColor = ConsoleColor.White;

                var response = Console.ReadKey().KeyChar;
                Console.WriteLine();

                if (char.ToLower(response) == 'y')
                {
                    using var fileStream = File.Create(filePath);
                    return new EditorStartupResult
                    {
                        Document = new Document(filePath),
                        ShouldStartEditor = true,
                        IsNewFile = true
                    };
                }

                return ShowInteractiveMenu();
            }

            return new EditorStartupResult
            {
                Document = new Document(filePath),
                ShouldStartEditor = true,
                IsNewFile = false
            };
        }
        catch (Exception ex)
        {
            ShowError($"Error opening file: {ex.Message}");
            return ShowInteractiveMenu();
        }
    }

    private static void ShowInvalidOption()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Invalid option!");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Please enter a valid choice (N/O/Q)");
        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }

    private static void ShowError(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey(true);
    }
}

public class EditorStartupResult
{
    public Document? Document { get; set; }
    public bool ShouldStartEditor { get; set; }
    public bool IsNewFile { get; set; }
}
