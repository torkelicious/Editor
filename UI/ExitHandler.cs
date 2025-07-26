using Editor.Core;

namespace Editor.UI;

public static class ExitHandler
{
    public static void HandleExit(Document document, bool isNewFile)
    {
        Console.Clear();
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Exiting");
        Console.WriteLine();

        if (document.IsDirty)
        {
            HandleUnsavedChanges(document, isNewFile);
        }
        else if (isNewFile && !document.IsUntitled)
        {
            HandleNewFileCleanup(document);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("No changes to save...");
        }
    }

    private static void HandleUnsavedChanges(Document document, bool isNewFile)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("You have unsaved changes!\n");

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Save changes before exiting? ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[Y]es");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(" / ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[n]o");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");

            var response = char.ToLower(Console.ReadKey().KeyChar);
            Console.WriteLine();

            switch (response)
            {
                case 'y':
                case '\r': // Enter key defaults to Yes
                    AttemptSave(document);
                    return;

                case 'n':
                    if (isNewFile && !document.IsUntitled)
                    {
                        HandleNewFileCleanup(document);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Exiting without saving changes.");
                    }

                    return;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Please enter Y, N, or C.");
                    Console.WriteLine();
                    break;
            }
        }
    }

    private static void AttemptSave(Document document)
    {
        try
        {
            if (document.IsUntitled)
            {
                // Need to get a file path
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("Enter filename to save as: ");
                Console.ForegroundColor = ConsoleColor.White;

                var filePath = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(filePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No filename entered. Exiting without saving...");
                    return;
                }

                // Check if file exists
                if (File.Exists(filePath))
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"File '{filePath}' already exists. Overwrite? (y/N): ");
                    Console.ForegroundColor = ConsoleColor.White;

                    var overwrite = char.ToLower(Console.ReadKey().KeyChar);
                    Console.WriteLine();

                    if (overwrite != 'y')
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Exiting without saving...");
                        return;
                    }
                }

                document.SaveToFile(filePath);
            }
            else
            {
                document.SaveToFile();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"File saved successfully: {document.FilePath}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error saving file: {ex.Message}");
            Console.WriteLine("Exiting without saving...");
        }
    }

    private static void HandleNewFileCleanup(Document document)
    {
        if (document.IsUntitled || !File.Exists(document.FilePath)) return;
        try
        {
            File.Delete(document.FilePath);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Cleaned up file buffer: {document.FilePath}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                $"Warning: Could not delete empty file: {ex.Message}\nMay require manual intervention!");
        }
    }
}