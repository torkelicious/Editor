using System;
using System.IO;
using System.Threading;

/*
 * this "Editor" started as an atempt to remake my sticky-notes app as a console-app, but it seems to have gone in another direction
 * Controls are like shitty Vim motions
 */

namespace Editor;

internal enum editorMode
{
    Normal,
    Insert
}

internal class Program
{
    public static List<string> linesBffrStore = new();
    public static string globalPath = string.Empty;

    public static bool editing;

    public static bool
        fileChanged =
            false; // Have to set this in every editing function because i did not think of implementing it before...

    private static void Main(string[] args)
    {
        // Accept CLI argument for file path
        if (File.Exists(args[0]))
        {
            fileOperations.readIntoBuffer(args[0]);
            editing = true;
        }
        else
        {
            inputs.initmode();
        }

        try
        {
            Console.SetWindowSize(drawing.minimumConsoleWidth, Console.WindowHeight);
        }
        catch
        {
            Console.WriteLine("Could not set window size.");
        } // This seems to only work on Windows

        while (editing)
        {
            drawing.drawScreen();
            inputs.mainInputHandler();
        }

        // ask to save file after editing
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.BackgroundColor = ConsoleColor.Black;
        ;
        Console.Clear();
        Console.WriteLine("Stopped editing\n");

        if (fileChanged)
        {
            Console.WriteLine("Save file? Y/n");
            string Yn = Console.ReadLine().ToLower();
            if (Yn == "y" || Yn == "yes" || Yn == string.Empty)
            {
                if (globalPath == string.Empty)
                {
                    Console.WriteLine("Enter save path: ");
                    globalPath = Console.ReadLine();
                }

                fileOperations.writeToFile(globalPath);
            }
        }
        else
        {
            Console.WriteLine("Exiting without saving...");
            Environment.Exit(0);
        }
    }
}

internal class inputs
{
    public static editorMode mode = editorMode.Normal;
    public static int xPos, yPos;

    public static void mainInputHandler()
    {
        var key = Console.ReadKey(true);

        if (mode == editorMode.Normal)
        {
            switch (key.Key)
            {
                case ConsoleKey.J:
                case ConsoleKey.DownArrow:
                    yPos++;
                    break;
                case ConsoleKey.K:
                case ConsoleKey.UpArrow:
                    yPos--;
                    break;
                case ConsoleKey.H:
                case ConsoleKey.LeftArrow:
                    xPos--;
                    break;
                case ConsoleKey.L:
                case ConsoleKey.RightArrow:
                    xPos++;
                    break;
                case ConsoleKey.I:
                    mode = editorMode.Insert;
                    break;
                case ConsoleKey.Q:
                    Program.editing = false;
                    break;
                case ConsoleKey.X:
                    fileOperations.del(1, false);
                    break;
            }
        }
        else if (mode == editorMode.Insert)
        {
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    yPos--;
                    break;
                case ConsoleKey.DownArrow:
                    yPos++;
                    break;
                case ConsoleKey.LeftArrow:
                    xPos--;
                    break;
                case ConsoleKey.RightArrow:
                    xPos++;
                    break;
                case ConsoleKey.Escape:
                    mode = editorMode.Normal;
                    break;
                case ConsoleKey.Enter:
                    fileOperations.newLine();
                    break;
                case ConsoleKey.Backspace:
                    fileOperations.del(1);
                    break;
                default:
                    fileOperations.insertChar(key.KeyChar);
                    break;
            }
        }

        // Avoid trying to move outside the window
        if (yPos < 0) yPos = 0;
        if (yPos >= Program.linesBffrStore.Count) yPos = Program.linesBffrStore.Count - 1;
        if (yPos < 0) yPos = 0; // In case bfrdLines is empty

        var currentLineLength =
            yPos >= 0 && yPos < Program.linesBffrStore.Count ? Program.linesBffrStore[yPos].Length : 0;
        if (xPos < 0) xPos = 0;
        if (xPos > currentLineLength) xPos = currentLineLength;
    }

    public static void initmode()
    {
        Console.Clear();
        Console.WriteLine(
            @"
What would you like to do?

Enter: 'n' for a new note
Enter: 'o' to open a note

Enter 'q' to quit

");
        var inp = char.ToLower(Console.ReadKey(true).KeyChar);
        switch (inp)
        {
            case 'n':
                // Initialize empty buffer 
                Program.linesBffrStore.Add("");
                break;
            case 'o':
                Console.Clear();
                Console.WriteLine("(Press CTRL+C to quit)\nEnter the path to the note you want to open: ");
                fileOperations.readIntoBuffer(Console.ReadLine());
                break;
            case 'q':
                Environment.Exit(0);
                break;
            default:
                Console.Clear();
                Console.WriteLine("(Press CTRL+C to quit)\nPlease enter a valid option! \n");
                initmode();
                break;
        }

        Program.editing = true; // Variable responsible for the main loop
    }
}

internal class fileOperations
{
    public static void readIntoBuffer(string path)
    {
        bool succesfullyRead = false;
        try
        {
            Console.Clear();
            File.ReadAllLines(path).ToList().ForEach(line => Program.linesBffrStore.Add(line));
            // Handle empty files
            if (Program.linesBffrStore.Count == 0) Program.linesBffrStore.Add("");
            succesfullyRead = true;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Directory not found!");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("You do not have permission to read this file!");
        }
        catch (IOException e)
        {
            Console.WriteLine("An error occurred while reading:\n" + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occured:\n" + e.Message);
        }

        if (succesfullyRead)
        {
            Program.globalPath = path;
            return;
        }

        Console.WriteLine("\nTry again (or press Ctrl+C to quit):");
        readIntoBuffer(Console.ReadLine());
    }

    public static void insertChar(char c)
    {
        Program.fileChanged = true;

        while (Program.linesBffrStore.Count <= inputs.yPos) Program.linesBffrStore.Add("");

        var currLine = Program.linesBffrStore[inputs.yPos];
        if (inputs.xPos >= currLine.Length)
            currLine = currLine.PadRight(inputs.xPos) + c;
        else
            currLine = currLine.Insert(inputs.xPos, c.ToString());
        Program.linesBffrStore[inputs.yPos] = currLine;
        inputs.xPos++;
    }

    public static void newLine()
    {
        Program.fileChanged = true;

        var currLine = string.Empty;
        var newLine = string.Empty;

        if (inputs.yPos < Program.linesBffrStore.Count)
        {
            currLine = Program.linesBffrStore[inputs.yPos];
            if (inputs.xPos < currLine.Length)
            {
                newLine = currLine.Substring(inputs.xPos);
                currLine = currLine.Substring(0, inputs.xPos);
                Program.linesBffrStore[inputs.yPos] = currLine;
            }
        }

        Program.linesBffrStore.Insert(inputs.yPos + 1, newLine);
        inputs.yPos++;
        inputs.xPos = 0;
    }

    public static void del(int count = 1, bool moveCursor = true)
    {
        Program.fileChanged = true;

        if (Program.linesBffrStore.Count <= 0) return;

        int posOffset = moveCursor ? 1 : 0;

        if (inputs.xPos > 0) // normal deletion within a line
        {
            string currLine = Program.linesBffrStore[inputs.yPos];
            if (inputs.xPos <= currLine.Length)
            {
                int charsToDelete = Math.Min(count, inputs.xPos);
                if (inputs.xPos - posOffset + charsToDelete <= currLine.Length)
                {
                    currLine = currLine.Remove(inputs.xPos - posOffset, charsToDelete);
                    Program.linesBffrStore[inputs.yPos] = currLine;

                    if (moveCursor) inputs.xPos -= charsToDelete;
                }
            }
        }
        else if (inputs.xPos == 0 && !moveCursor) // deletion in normal mode (x key)
        {
            string currLine = Program.linesBffrStore[inputs.yPos];
            if (currLine.Length > 0)
            {
                int charsToDelete = Math.Min(count, currLine.Length);
                currLine = currLine.Remove(0, charsToDelete);
                Program.linesBffrStore[inputs.yPos] = currLine;
            }
        }
        else if (inputs.xPos == 0 && inputs.yPos > 0) // line joining
        {
            string prevLine = Program.linesBffrStore[inputs.yPos - 1];
            string currLine = Program.linesBffrStore[inputs.yPos];

            inputs.xPos = prevLine.Length;
            Program.linesBffrStore[inputs.yPos - 1] = prevLine + currLine;
            Program.linesBffrStore.RemoveAt(inputs.yPos);
            inputs.yPos--;
        }
    }

    public static void writeToFile(string path)
    {
        bool saved = false;

        Console.Clear();
        try
        {
            File.WriteAllLines(path, Program.linesBffrStore);
            Console.WriteLine($"Saved to file: {path}");
            saved = true;
        }
        catch (DirectoryNotFoundException)
        {
            Console.WriteLine("Directory not found!");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("You do not have permission to write here!");
        }
        catch (IOException e)
        {
            Console.WriteLine("An error occurred while writing:\n" + e.Message);
        }
        catch (Exception e)
        {
            Console.WriteLine("An error occured:\n" + e.Message);
        }

        if (saved) return; // avoid looping lul
        Console.WriteLine("\nTry again (or press Ctrl+C to quit):");
        writeToFile(Console.ReadLine());
    }
}

internal class drawing
{
    private static int viewportStartLine;
    private static List<string> currentScreenLines = new();
    private static readonly int linesPadding = 3;
    private static int linesToDraw = 80;
    public static int minimumConsoleWidth = 100;

    public static void drawScreen()
    {
        while (Console.WindowWidth < minimumConsoleWidth)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Window width too small! (Min: {minimumConsoleWidth}c)\nPlease resize your Console.");
            System.Threading.Thread.Sleep(500);
        }

        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Clear();
        linesToDraw = Console.WindowHeight - linesPadding;
        adjustViewport();

        var linesDrawn = 0;

        for (var i = viewportStartLine; i < Program.linesBffrStore.Count && linesDrawn < linesToDraw; i++)
        {
            var lineToRender = Program.linesBffrStore[i];
            if (lineToRender.Length > Console.WindowWidth)
                lineToRender =
                    lineToRender.Substring(0, Console.WindowWidth - 1) +
                    "+"; //TODO: implement veiwport BS on x axis aswell

            Console.SetCursorPosition(0, linesDrawn);
            Console.Write(lineToRender);
            linesDrawn++;
        }

        drawStatusLine();
        Console.SetCursorPosition(inputs.xPos, inputs.yPos - viewportStartLine);
    }

    private static void adjustViewport()
    {
        if (inputs.yPos < viewportStartLine)
            viewportStartLine = inputs.yPos;
        else if (inputs.yPos >= viewportStartLine + linesToDraw) viewportStartLine = inputs.yPos - linesToDraw + 1;
    }

    private static void drawStatusLine()
    {
        //separator
        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding);
        Console.Write(new string('-', Console.WindowWidth));

        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        // Second line
        // coloring for modes
        if (inputs.mode == editorMode.Normal)
            Console.BackgroundColor = ConsoleColor.Green;
        else
            Console.BackgroundColor = ConsoleColor.Yellow;
        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding + 1);
        var modeTxt = $"Mode: {inputs.mode.ToString()}";
        var posTxt = $"{inputs.yPos + 1}:{inputs.xPos + 1}";
        Console.Write($"{modeTxt} || {posTxt} ||");

        Console.BackgroundColor = ConsoleColor.DarkBlue;
        if (Program.fileChanged)
        {
            Console.Write("  MODIFIED");
        }

        if (Program.globalPath != string.Empty)
        {
            Console.Write($"  󰝰 {Path.GetFullPath(Program.globalPath)}");
        }

        // Third line
        Console.BackgroundColor = ConsoleColor.White;
        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding + 2);
        Console.Write(
            "HJKL/Arrows: Move || q: Quit (NORMAL) || i: INSERT mode || ESC: NORMAL mode || x: Delete (NORMAL)");
    }
}