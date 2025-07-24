using Editor.Core;
using Editor.UI;
using Editor.Input;

namespace Editor;

class Program
{
    private static bool debug = false;

    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            foreach (var arg in args)
            {
                // TODO: fix this dammn HACK: THIS IS FUCKING HACKY
                if (arg == "--debug")
                {
                    debug = true;
                }
            }
        }

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
                // do nothing...
            }

            // startup menu / handle file selection
            var startupResult = StartupMenu.ShowMenu(args);

            if (!startupResult.ShouldStartEditor || startupResult.Document == null)
            {
                return; // user quit
            }

            // editor components
            var document = startupResult.Document;
            var editorState = new EditorState();
            var viewport = new Viewport();
            var renderer = new ConsoleRenderer(viewport);
            renderer.RegisterWithDocument(document);
            var inputHandler = new InputHandler(document, editorState, viewport);

            if (debug) document.showDebugInfo = true;
            Console.Clear();
            
            renderer.Render(document, editorState); // render before loop to avoid forcing user to input to start the program
            // Main editor loop
            bool exitRequested = false;
            while (!exitRequested)
            {
                try
                {
                    editorState.UpdateFromDocument(document);
                    inputHandler.HandleInput();
                    editorState.UpdateFromDocument(document);
                    renderer.Render(document, editorState); // Render again after input

                    if (inputHandler.ShouldQuit)
                    {
                        ExitHandler.HandleExit(document, startupResult.IsNewFile);
                        exitRequested = true;
                    }
                }
                catch (Exception ex)
                {
                    if (debug)
                    {
                        ShowError($"Editor error: {ex}");
                    }
                    else
                    {
                        ShowError($"Editor error: {ex.Message}");
                    }

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