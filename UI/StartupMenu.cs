using Editor.Core;

namespace Editor.UI;

public class StartupMenu
{
    private const string Version = "0.0.0-Rewrite";
    
    public static EditorStartupResult ShowMenu(string[] args)
    {
        // command line arguments first
        if (args.Length > 0)
        {
            return HandleCommandLineArgs(args);
        }
        return ShowInteractiveMenu();
    }
    
    private static EditorStartupResult HandleCommandLineArgs(string[] args)
    {
        var filePath = args[0].Trim();
        
        if (File.Exists(filePath))
        {
            return new EditorStartupResult
            {
                Document = new Document(filePath),
                ShouldStartEditor = true,
                IsNewFile = false
            };
        }
        else
        {
            try
            {
                using var fileStream = File.Create(filePath);
                
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
        
        // Ensure minimum width
        while (Console.WindowWidth < ConsoleRenderer.MinimumConsoleWidth)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Window width too small! (Min: {ConsoleRenderer.MinimumConsoleWidth}c)");
            Console.WriteLine("Please resize your Console.");
            Thread.Sleep(500);
        }
        
        // Reset colors
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();
        
        string separator = new string('─', Math.Min(Console.WindowWidth, 60));
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(
@"         __    _ __     
   _____/ /_  (_) /_    
  / ___/ __ \/ / __/    
 (__  ) / / / / /_      
/____/_/ /_/_/\__/      ");
        
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"             SharpEditor [v{Version}]");
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
        Console.WriteLine("* Shitty text editor");
        Console.WriteLine("* by Torkelicious, 2025");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.Write("Enter your choice: ");
    }
    
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
        
        string? filePath = null;
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
                else
                {
                    return ShowInteractiveMenu();
                }
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