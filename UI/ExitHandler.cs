#region

using Editor.Core;

#endregion

namespace Editor.UI;

public static class ExitHandler
{
    public static void HandleExit(Document document, bool isNewFile)
    {
        AnsiConsole.HideCursor();
        AnsiConsole.Clear();
        AnsiConsole.WriteLine("{YELLOW}Exiting");
        AnsiConsole.WriteLine("");

        if (document.IsDirty)
            HandleUnsavedChanges(document, isNewFile);
        else if (document.cleanOnExit)
            HandleNewFileCleanup(document);
        else
            AnsiConsole.WriteLine("{GREEN}No changes to save...");
        AnsiConsole.ShowCursor();
    }

    private static void HandleUnsavedChanges(Document document, bool isNewFile)
    {
        AnsiConsole.HideCursor();
        AnsiConsole.WriteLine("{YELLOW}You have unsaved changes!\n");

        while (true)
        {
            AnsiConsole.Write("{WHITE}Save changes before exiting? ");
            AnsiConsole.Write("{GREEN}[Y]es");
            AnsiConsole.Write("{WHITE} / ");
            AnsiConsole.Write("{RED}[n]o");
            AnsiConsole.Write("{WHITE}: ");
            AnsiConsole.ShowCursor();
            var response = char.ToLower(Console.ReadKey().KeyChar);
            AnsiConsole.WriteLine("");

            switch (response)
            {
                case 'y':
                case '\r': // Enter key defaults to Yes
                    AttemptSave(document);
                    return;

                case 'n':
                    if (document.cleanOnExit)
                        HandleNewFileCleanup(document);
                    else
                        AnsiConsole.WriteLine("{RED}Exiting without saving changes.");
                    return;
                default:
                    AnsiConsole.WriteLine("{RED}Please enter Y or N.");
                    AnsiConsole.WriteLine("");
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
                AnsiConsole.WriteLine("");
                AnsiConsole.Write("{CYAN}Enter filename to save as: ");

                var filePath = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(filePath))
                {
                    AnsiConsole.WriteLine("{RED}No filename entered. Exiting without saving...");
                    return;
                }

                // Check file 
                if (File.Exists(filePath))
                {
                    AnsiConsole.WriteLine("");
                    AnsiConsole.Write($"{{YELLOW}}File '{filePath}' already exists. Overwrite? (y/N): ");

                    var overwrite = char.ToLower(Console.ReadKey().KeyChar);
                    AnsiConsole.WriteLine("");

                    if (overwrite != 'y')
                    {
                        AnsiConsole.WriteLine("{RED}Exiting without saving...");
                        return;
                    }
                }

                document.SaveToFile(filePath);
            }
            else
            {
                document.SaveToFile();
            }

            AnsiConsole.WriteLine($"{{GREEN}}File saved successfully: {document.FilePath}");
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine($"{{RED}}Error saving file: {ex.Message}");
            AnsiConsole.WriteLine("{RED}Exiting without saving...");
        }
    }

    private static void HandleNewFileCleanup(Document document)
    {
        if (document.cleanOnExit && !string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
            try
            {
                File.Delete(document.FilePath);
                AnsiConsole.WriteLine($"{{YELLOW}}Cleaned up file {document.FilePath}");
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteLine(
                    $"{{RED}}Warning: Could not delete temporary file: {ex.Message}\nMay require manual intervention!");
            }
    }
}