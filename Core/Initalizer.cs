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
    // - - -

    private static bool isDebug;

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
        Console.OutputEncoding = Encoding.UTF8; // use UTF-8

        if (OperatingSystem.IsWindows())
            try
            {
                HandleWindowsOS(); // this sucks bruh
            }
            catch
            {
                /* do nothing */
            }
        else
            Console.Write("\x1b[3J\x1b[2J\x1b[H"); // probably not needed but why not

        if (args.Length > 0)
            for (var i = 0; i < args.Length; i++)
                if (args[i] == "--debug")
                {
                    isDebug = true;
                    args[i] = string.Empty;
                    break;
                }

        try
        {
            var startupResult = StartupMenu.ShowMenu(args);
            Console.Clear();
            if (!startupResult.ShouldStartEditor || startupResult.Document == null) return; // user quit
            // editor components
            var document = startupResult.Document;
            var editorState = new EditorState();
            var viewport = new Viewport();
            var renderer = new ConsoleRenderer(viewport);
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

        if (isDebug) document.showDebugInfo = true;
        Console.Clear();
        renderer.Render(document, editorState); // render once before loop to avoid forcing user to input

        // Main editor loop
        var exitRequested = false;
        while (!exitRequested)
            try
            {
                inputHandler.HandleInput();
                editorState.UpdateFromDocument(document);
                renderer.Render(document, editorState, inputHandler.lastInputToShow); // Render after input
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
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Red;
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

// i should look into using ansii escape codes for coloring ?