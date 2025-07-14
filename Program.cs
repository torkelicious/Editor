using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;

/*
 * this "Editor" started as an atempt to remake my sticky-notes app as a console-app 
 * Controls are like shitty Vim motions
 * use this on small files only, as everything is stored in memory for now
 */

namespace Editor;

internal enum editorMode
{
    Normal,
    Insert
}

internal class Program
{
    public static List<string> linesBffrStore = new(); // All text is currently stored in this list TODO: implement buffering to temp file / swap instead of loading the whole text file to memory (Probably after a few refactorings though)
    public static string globalPath = string.Empty;
    public static bool editing, fileChanged, filePassed = false;
    private static void Main(string[] args)
    {
        // Accept CLI argument for file path
        if (args.Length > 0 )
        {
            if (File.Exists(args[0]))
            {
                FileOperations.readIntoBuffer(args[0]);
                filePassed = true;
            }
            else
            {
                try
                {
                    var fileStream = File.Create(args[0]);
                    fileStream.Close();
                    FileOperations.readIntoBuffer(args[0]);
                    filePassed = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not create file; {e}\n Going to menu...");
                    Inputs.initmode();
                }
            }
        }
        else { Inputs.initmode(); }
        
        try{ Console.SetWindowSize(Drawing.minimumConsoleWidth, Console.WindowHeight); } catch{ Console.Write("Could not set window size."); } // This only work on Windows

        // Main loop
        while (editing)
        {
            Drawing.drawScreen();
            Inputs.mainInputHandler();
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Clear();
        Console.WriteLine("Stopped editing\n");

        if (fileChanged)
        {
            FileOperations.saveOnExitDialog();
        } 
    }
}

internal class Inputs
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
                case ConsoleKey.Spacebar:
                    xPos++;
                    break;
                case ConsoleKey.I:
                    mode = editorMode.Insert;
                    break;
                case ConsoleKey.Q:
                    Program.editing = false;
                    break;
                case ConsoleKey.X:
                    FileOperations.del(1, false);
                    break;
                case ConsoleKey.Tab:
                    xPos += 4;
                    break;
                case ConsoleKey.Backspace:
                    xPos -= 4;
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
                    FileOperations.newLine();
                    break;
                case ConsoleKey.Backspace:
                case ConsoleKey.Delete:
                    FileOperations.del(1);
                    break;
                case ConsoleKey.Tab: // This is really fucking stupid but i am not in the mood to create some whole new bs for one key
                    for (int i = 0; i < 4; i++)
                    {
                        FileOperations.insertChar(' ');
                    }
                    break;
                default:
                    FileOperations.insertChar(key.KeyChar);
                    break;
            }
        }

        // Avoid trying to move out of bounds 
        if (yPos < 0) yPos = 0;
        if (yPos >= Program.linesBffrStore.Count) yPos = Program.linesBffrStore.Count - 1;
        if (yPos < 0) yPos = 0; // In case bfrdLines is empty
        var currentLineLength = yPos >= 0 && yPos < Program.linesBffrStore.Count ? Program.linesBffrStore[yPos].Length : 0;
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
- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
* use this on small files only (everything is stored in memory for now)

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
                FileOperations.readIntoBuffer(Console.ReadLine());
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

internal class FileOperations
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
            Program.editing = true;
            Program.filePassed= true;
            return;
        }
        Console.WriteLine("\nTry again (or press Ctrl+C to quit):");
        readIntoBuffer(Console.ReadLine());
    }

    public static void insertChar(char c)
    {
        Program.fileChanged = true;

        while (Program.linesBffrStore.Count <= Inputs.yPos) Program.linesBffrStore.Add("");

        var currLine = Program.linesBffrStore[Inputs.yPos];
        if (Inputs.xPos >= currLine.Length)
            currLine = currLine.PadRight(Inputs.xPos) + c;
        else
            currLine = currLine.Insert(Inputs.xPos, c.ToString());
        Program.linesBffrStore[Inputs.yPos] = currLine;
        Inputs.xPos++;
    }

    public static void newLine()
    {
        Program.fileChanged = true;

        var currLine = string.Empty;
        var newLine = string.Empty;

        if (Inputs.yPos < Program.linesBffrStore.Count)
        {
            currLine = Program.linesBffrStore[Inputs.yPos];
            if (Inputs.xPos < currLine.Length)
            {
                newLine = currLine.Substring(Inputs.xPos);
                currLine = currLine.Substring(0, Inputs.xPos);
                Program.linesBffrStore[Inputs.yPos] = currLine;
            }
        }

        Program.linesBffrStore.Insert(Inputs.yPos + 1, newLine);
        Inputs.yPos++;
        Inputs.xPos = 0;
    }

    public static void del(int count = 1, bool moveCursor = true)
    {
        Program.fileChanged = true;

        if (Program.linesBffrStore.Count <= 0) return;

        int posOffset = moveCursor ? 1 : 0;

        if (Inputs.xPos > 0) // normal deletion within a line
        {
            string currLine = Program.linesBffrStore[Inputs.yPos];
            if (Inputs.xPos <= currLine.Length)
            {
                int charsToDelete = Math.Min(count, Inputs.xPos);
                if (Inputs.xPos - posOffset + charsToDelete <= currLine.Length)
                {
                    currLine = currLine.Remove(Inputs.xPos - posOffset, charsToDelete);
                    Program.linesBffrStore[Inputs.yPos] = currLine;

                    if (moveCursor) Inputs.xPos -= charsToDelete;
                }
            }
        }
        else if (Inputs.xPos == 0 && !moveCursor) // deletion in normal mode (x key)
        {
            string currLine = Program.linesBffrStore[Inputs.yPos];
            if (currLine.Length > 0)
            {
                int charsToDelete = Math.Min(count, currLine.Length);
                currLine = currLine.Remove(0, charsToDelete);
                Program.linesBffrStore[Inputs.yPos] = currLine;
            }
        }
        else if (Inputs.xPos == 0 && Inputs.yPos > 0) // line joining
        {
            string prevLine = Program.linesBffrStore[Inputs.yPos - 1];
            string currLine = Program.linesBffrStore[Inputs.yPos];

            Inputs.xPos = prevLine.Length;
            Program.linesBffrStore[Inputs.yPos - 1] = prevLine + currLine;
            Program.linesBffrStore.RemoveAt(Inputs.yPos);
            Inputs.yPos--;
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

    public static void saveOnExitDialog()
    {
        if (Program.fileChanged)
        {
            Console.WriteLine("Save file? Y/n");
            string Yn = Console.ReadLine().ToLower();
            if (Yn == "y" || Yn == "yes" || Yn == string.Empty)
            {
                if (Program.globalPath == string.Empty)
                {
                    Console.WriteLine("Enter save path: ");
                    Program.globalPath = Console.ReadLine();
                }
                if (File.Exists(Program.globalPath) && !Program.filePassed)
                {
                    Console.WriteLine("\nA file with this name already exists, would you like to overwrite it? y/N");
                    Yn = Console.ReadLine().ToLower();
                    if (Yn != "y" && Yn != "yes")
                    {
                        Console.WriteLine("Exiting without saving...");
                        Environment.Exit(0); // Exit before we can write to the file
                    }
                }
                writeToFile(Program.globalPath);
            }
        }
        else
        {
            Console.WriteLine("Exiting without saving...");
            Environment.Exit(0);
        }
    }
}

internal class Drawing
{
    private static int viewportStartLine;
    private static int veiwportStartCol;
    
    //private static List<string> currentScreenLines = new();
   
    // Free lines/Colums for other stuff like status bar etc
    private static int linesPadding = 3;
    private static int columnPadding = 1; 
   
    private static int linesToDraw = 80;
    private static int colsToDraw;  
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
        colsToDraw = Console.WindowWidth - columnPadding; 
        
        adjustViewport();
        var linesDrawn = 0;

        for (var i = viewportStartLine; i < Program.linesBffrStore.Count && linesDrawn < linesToDraw; i++)
        {
            var lineToRender = Program.linesBffrStore[i];

            if (veiwportStartCol > 0 && lineToRender.Length > veiwportStartCol)
            {
                lineToRender = lineToRender.Substring(veiwportStartCol);
            }
            else if (veiwportStartCol > 0)
            {
                lineToRender = string.Empty;
            }

            bool truncated = false;
            if (lineToRender.Length > colsToDraw)
            {
                lineToRender = lineToRender.Substring(0, colsToDraw );
                truncated = true;
            }
            
            // main drawing 
            Console.SetCursorPosition(0, linesDrawn);
            Console.Write(lineToRender);
            
            if (veiwportStartCol > 0 && i == Inputs.yPos) // this might cause problem with text deletion..?
            {
                Console.SetCursorPosition(0, linesDrawn);
                Console.Write("<");
            }
            
            if (truncated && i == Inputs.yPos)
            {
                Console.SetCursorPosition(colsToDraw, linesDrawn);
                Console.Write(">"); 
            }

            linesDrawn++;
        }
        drawStatusLine();
        Console.SetCursorPosition(Inputs.xPos - veiwportStartCol, Inputs.yPos - viewportStartLine);
    }

    private static void adjustViewport()
    {
        // vertical
        if (Inputs.yPos < viewportStartLine) viewportStartLine = Inputs.yPos;
        else if (Inputs.yPos >= viewportStartLine + linesToDraw) viewportStartLine = Inputs.yPos - linesToDraw + 1;
        
        // horizontal
        int horizPadding = 5;
        
        // left
        if (Inputs.xPos < veiwportStartCol + horizPadding) veiwportStartCol = Math.Max(0, Inputs.xPos - horizPadding); 
        else if (Inputs.xPos >= veiwportStartCol + colsToDraw - horizPadding) veiwportStartCol = Inputs.xPos - colsToDraw + horizPadding + 1;
        
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
       ConsoleColor modeClr = Inputs.mode == editorMode.Normal ? ConsoleColor.Green : ConsoleColor.Yellow;
       Console.BackgroundColor = modeClr;
       
        Console.SetCursorPosition(0, Console.WindowHeight - linesPadding + 1);
        var modeTxt = $"Mode: {Inputs.mode.ToString()}";
        var posTxt = $"{Inputs.yPos + 1}:{Inputs.xPos + 1}";
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