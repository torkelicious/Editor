using Editor.Core;
using Editor.UI;
using Editor.Input;

namespace Editor;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Set window size if possible (Windows only)
            try
            {
                if (Console.WindowWidth < ConsoleRenderer.MinimumConsoleWidth)
                {
                    Console.SetWindowSize(ConsoleRenderer.MinimumConsoleWidth, Console.WindowHeight);
                }
            }
            catch
            {
                // Ignore if can't set window size (Console.SetWindowSize is a windows-only feature)
            }
            
            // startup menu / handle file selection
            var startupResult = StartupMenu.ShowMenu(args);
            
            if (!startupResult.ShouldStartEditor || startupResult.Document == null)
            {
                return; // user quit
            }
            
            // editor components
            using var document = startupResult.Document;
            var editorState = new EditorState();
            var viewport = new Viewport();
            var renderer = new ConsoleRenderer(viewport);
            var inputHandler = new InputHandler(document, editorState, viewport);
            
            // Main editor loop
            bool exitRequested = false;
            while (!exitRequested)
            {
                try
                {
                    editorState.UpdateFromDocument(document);
                    renderer.Render(document, editorState);
                    inputHandler.HandleInput();
                    
                    if (inputHandler.ShouldQuit)
                    {
                        // Handle exit with save prompt
                        ExitHandler.HandleExit(document, startupResult.IsNewFile);
                        exitRequested = true;
                    }
                }
                catch (Exception ex)
                {
                    // Error handling during editor operation
                    ShowError($"Editor error: {ex.Message}");
                    
                    Console.WriteLine("Continue editing? (y/N): ");
                    var response = Console.ReadKey().KeyChar;
                    
                    if (char.ToLower(response) != 'y')
                    {
                        ExitHandler.HandleExit(document, startupResult.IsNewFile);
                        exitRequested = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Fatal error
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FATAL ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
    
    private static void ShowError(string message)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ERROR:");
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}