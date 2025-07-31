#region

using System.Runtime.InteropServices;
using Editor.Core.EditorActions;
using Editor.Input;
using Editor.UI;

#endregion


namespace Editor.Core;

public static class Initalizer
{
    private static bool isDebug;

    public static void initEditor(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            try
            {
                Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
                Console.WriteLine("\x1b[3J");
                Console.Clear();
            }
            catch
            {
                /* do nothing */
            }

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
        Console.WriteLine("\x1b[3J");
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
        Console.WriteLine("\x1b[3J");
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
}