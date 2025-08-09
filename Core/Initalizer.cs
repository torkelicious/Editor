#region

using System.Runtime.InteropServices;
using System.Text;
using Editor.Core.EditorActions;
using Editor.Input;
using Editor.UI;

#endregion


namespace Editor.Core;

public static class Initalizer
{
    // winapi
    // - - -
    private const int STD_OUTPUT_HANDLE = -11;

    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    private static string configDir = @"";
    // - - -

    private static bool isDebug;

    public static ConsoleRenderer renderer;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    [DllImport("kernel32.dll")]
    private static extern bool SetConsoleCP(uint wCodePageID);

    public static void initEditor(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (OperatingSystem.IsWindows())
            try
            {
                HandleWindowsOS();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Terminal may be unsupported, or render wrong!");
            }

        if (args.Length > 0)
            for (var i = 0; i < args.Length; i++)
                switch (args[i])
                {
                    case "--debug"
                        : // Debug info WILL break rendering on small enough terminals as it is not truncated (and it should not be!!)
                        isDebug = true;
                        args[i] = string.Empty;
                        break;
                }

        Config.Load();

        try
        {
            var startupResult = StartupMenu.ShowMenu(args);
            AnsiConsole.Clear();
            if (!startupResult.ShouldStartEditor || startupResult.Document == null) return; // user quit
            // editor components
            var document = startupResult.Document;
            var editorState = new EditorState();
            var viewport = new Viewport();
            renderer = new ConsoleRenderer(viewport);
            var undoManager = new UndoManager();
            renderer.RegisterWithDocument(document);
            // !! We create inputHandler in the mainLoop func !!
            MainLoop(document, editorState, viewport, renderer, startupResult, undoManager);
        }
        catch (Exception ex)
        {
            // fatal error
            ShowError(ex, true, true);
        }
    }

    private static void MainLoop(Document document, EditorState editorState, Viewport viewport,
        ConsoleRenderer renderer, EditorStartupResult startupResult, UndoManager undoManager)
    {
        var inputHandler = new InputHandler(document, editorState, viewport, undoManager);
        StatusBar.setIcons();
        if (isDebug) document.showDebugInfo = true;
        AnsiConsole.Clear();
        AnsiConsole.HideCursor();
        renderer.Render(document, editorState); // render once before loop to avoid forcing user to input
        AnsiConsole.ShowCursor();
        // Main editor loop
        var exitRequested = false;
        while (!exitRequested)
            try
            {
                inputHandler.HandleInput();
                editorState.UpdateFromDocument(document);
                AnsiConsole.HideCursor();
                renderer.Render(document, editorState, inputHandler.lastInputToShow); // Render after input
                AnsiConsole.ShowCursor();
                if (inputHandler.ShouldQuit)
                {
                    ExitHandler.HandleExit(document, startupResult.IsNewFile);
                    exitRequested = true;
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
    }

    private static void ShowError(Exception ex, bool isFatal = false, bool quit = false)
    {
        Console.Write("\x1b[?25l"); // write raw instead of using AnsiConsole wrapper for error safety
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red; // avoid using ansiconsole colors for error safety too 
        if (isFatal)
        {
            Console.WriteLine("Fatal error:");
            Console.WriteLine(ex);
            Console.WriteLine("Stacktrace;");
            Console.WriteLine(ex.StackTrace);
        }
        else
        {
            Console.WriteLine("Error;");
            Console.WriteLine(ex.Message);
        }

        Console.Write("\x1b[?25h");
        if (!quit) return;
        Environment.Exit(-1);
    }

    private static void HandleWindowsOS() // run on Windows only!
    {
        var handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (GetConsoleMode(handle, out var mode))
            SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        else
            Console.Error.WriteLine("Could not get console mode.");

        // Set UTF-8 code page
        SetConsoleOutputCP(65001);
        SetConsoleCP(65001);
        try
        {
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        }
        catch
        {
        }

        Console.Write("\x1b[3J\x1b[2J\x1b[H");
    }
}