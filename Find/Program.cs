using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Find
{
    class Program
    {
        static string[] fileExtensions = { "*.cs", "*.js", "*.css" };
        static string settingsFilePath = "program.settings";

        [STAThread]
        static void Main(string[] args)
        {
            Settings settings = LoadSettings(settingsFilePath);

            if (settings != null)
            {
                fileExtensions = settings.FileExtensions;
                Console.WriteLine($"Using last used directory: {settings.DirectoryPath}");
                Console.WriteLine($"Using last used file extensions: {string.Join(", ", fileExtensions)}");
            }
            else
            {
                settings = new Settings();
            }

            if (string.IsNullOrEmpty(settings.DirectoryPath) || !Directory.Exists(settings.DirectoryPath))
            {
                Console.Write("Enter the directory to search in: ");
                settings.DirectoryPath = Console.ReadLine();

                if (string.IsNullOrEmpty(settings.DirectoryPath) || !Directory.Exists(settings.DirectoryPath))
                {
                    Console.WriteLine("Invalid directory. Exiting application.");
                    return;
                }

                SaveSettings(settingsFilePath, settings);
            }

            while (true)
            {
                Console.WriteLine($"Current file extensions: {string.Join(", ", fileExtensions)}");

                Console.Write("Enter the keyword to search for (or type 'd' to change directory, 'f' to change file extensions, 'c' to count lines, or 'q' to quit): ");
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    continue;
                }

                string command = input.Trim().ToLowerInvariant();

                if (command == "q")
                {
                    Console.WriteLine("Exiting application.");
                    break;
                }

                if (command == "d")
                {
                    Console.Write("Enter the new directory to search in: ");
                    settings.DirectoryPath = Console.ReadLine();

                    if (string.IsNullOrEmpty(settings.DirectoryPath) || !Directory.Exists(settings.DirectoryPath))
                    {
                        Console.WriteLine("Invalid directory. Exiting application.");
                        return;
                    }

                    SaveSettings(settingsFilePath, settings);
                    continue;
                }

                if (command == "f")
                {
                    Console.WriteLine("Enter the new file extensions as a comma-separated list (e.g., .cs,.js,.css):");
                    string extensions = Console.ReadLine();

                    if (string.IsNullOrEmpty(extensions))
                    {
                        Console.WriteLine("Invalid file extensions. Please try again.");
                        continue;
                    }

                    settings.FileExtensions = extensions.Split(',')
                                                         .Select(ext => $"*{ext.Trim()}")
                                                         .ToArray();
                    fileExtensions = settings.FileExtensions;
                    Console.WriteLine($"File extensions updated: {string.Join(", ", fileExtensions)}");
                    SaveSettings(settingsFilePath, settings);
                    continue;
                }

                if (command == "c")
                {
                    Console.WriteLine("Counting lines in files...");

                    int totalLines = 0;
                    foreach (var pattern in fileExtensions)
                    {
                        var files = Directory.GetFiles(settings.DirectoryPath, pattern, SearchOption.AllDirectories);
                        int linesCount = files.Sum(file => File.ReadLines(file).Count());

                        totalLines += linesCount;

                        Console.WriteLine($"{pattern}: {linesCount.ToString("N").Replace(".00", "")} lines in {files.Length.ToString("N").Replace(".00", "")} files.");
                    }

                    Console.WriteLine($"Total lines in all files: {totalLines.ToString("N").Replace(".00", "")}\n");
                    continue;
                }

                string keyword = input;

                var filesToSearch = fileExtensions
                    .SelectMany(pattern => Directory.EnumerateFiles(settings.DirectoryPath, pattern, SearchOption.AllDirectories))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int totalFiles = filesToSearch.Count;
                int searchedFiles = 0;

                Console.WriteLine($"Searching for \"{keyword}\" in {totalFiles.ToString("N0")} {string.Join(", ", fileExtensions)} files...");
                Console.WriteLine();

                int progressLineTop = Console.CursorTop;
                Console.WriteLine();

                bool found = false;
                int resultCount = 0;

                StringBuilder clipboardOutput = new StringBuilder();
                clipboardOutput.AppendLine("Keyword\tFileName\tFilePath\tLineNumber\tLineText");

                StringBuilder consoleResults = new StringBuilder();

                bool originalCursorVisible = Console.CursorVisible;
                Console.CursorVisible = false;

                try
                {
                    foreach (var file in filesToSearch)
                    {
                        searchedFiles++;
                        WriteProgressLine(progressLineTop, searchedFiles, totalFiles);

                        int lineNumber = 0;

                        foreach (var line in File.ReadLines(file))
                        {
                            lineNumber++;

                            if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                resultCount++;

                                clipboardOutput.Append(EscapeTabSeparatedValue(keyword));
                                clipboardOutput.Append('\t');

                                clipboardOutput.Append(EscapeTabSeparatedValue(Path.GetFileName(file)));
                                clipboardOutput.Append('\t');

                                clipboardOutput.Append(EscapeTabSeparatedValue(file));
                                clipboardOutput.Append('\t');

                                clipboardOutput.Append(lineNumber);
                                clipboardOutput.Append('\t');

                                clipboardOutput.Append(EscapeTabSeparatedValue(line));
                                clipboardOutput.AppendLine();

                                consoleResults.AppendLine($"Keyword found in file: {file}");
                                consoleResults.AppendLine($"Line {lineNumber}: {line}");
                                consoleResults.AppendLine();
                            }
                        }
                    }
                }
                finally
                {
                    Console.CursorVisible = originalCursorVisible;
                }

                Console.SetCursorPosition(0, progressLineTop + 1);
                Console.WriteLine();

                if (!found)
                {
                    Console.WriteLine("No occurrences found.");
                }
                else
                {
                    Console.Write(consoleResults.ToString());

                    Console.WriteLine($"{resultCount.ToString("N0")} result(s) found.");

                    Console.Write("Copy tab-separated results to clipboard for Excel? (y/n, Enter to skip): ");
                    string copyInput = Console.ReadLine();

                    if (!string.IsNullOrWhiteSpace(copyInput) &&
                        copyInput.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
                    {
                        Clipboard.SetText(clipboardOutput.ToString());
                        Console.WriteLine("Results copied to clipboard in tab-separated format.");
                    }
                    else
                    {
                        Console.WriteLine("Clipboard copy skipped.");
                    }
                }

                Console.WriteLine("Search complete.\n");
            }
        }

        static void CopySearchResultsToClipboard(List<SearchResult> results)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Keyword\tFileName\tFilePath\tLineNumber\tLineText");

            foreach (var result in results)
            {
                sb.Append(EscapeTabSeparatedValue(result.Keyword));
                sb.Append('\t');

                sb.Append(EscapeTabSeparatedValue(result.FileName));
                sb.Append('\t');

                sb.Append(EscapeTabSeparatedValue(result.FilePath));
                sb.Append('\t');

                sb.Append(result.LineNumber);
                sb.Append('\t');

                sb.Append(EscapeTabSeparatedValue(result.LineText));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString());
        }

        static string EscapeTabSeparatedValue(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value
                .Replace("\t", " ")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        static void SaveSettings(string filePath, Settings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        static Settings LoadSettings(string filePath)
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Settings>(json);
            }

            return null;
        }
        static void WriteProgressLine(int progressLineTop, int searchedFiles, int totalFiles)
        {
            string progressText = $"Files searched: {searchedFiles.ToString("N0")} / {totalFiles.ToString("N0")}";

            Console.SetCursorPosition(0, progressLineTop);
            Console.Write(progressText.PadRight(Console.WindowWidth - 1));
        }
    }

    class SearchResult
    {
        public string Keyword { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string LineText { get; set; }
    }

    class Settings
    {
        public string DirectoryPath { get; set; }

        public string[] FileExtensions { get; set; } =
        {
            "*.cs",
            "*.js",
            "*.css"
        };
    }
}