using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Find
{
    class Program
    {
        static string[] fileExtensions = { "*.cs", "*.js", "*.css" };
        static string settingsFilePath = "program.settings";

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
                // Display the current file extensions
                Console.WriteLine($"Current file extensions: {string.Join(", ", fileExtensions)}");

                // Ask for the keyword to search for, change directory, change file extensions, or count lines
                Console.Write("Enter the keyword to search for (or type 'd' to change directory, 'f' to change file extensions, 'c' to count lines, or 'q' to quit): ");
                string input = Console.ReadLine();

                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine("Invalid input. Please try again.");
                    continue;
                }

                if (input.ToLower() == "q")
                {
                    Console.WriteLine("Exiting application.");
                    break;
                }

                if (input.ToLower() == "d")
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

                if (input.ToLower().StartsWith("f"))
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

                if (input.ToLower() == "c")
                {
                    Console.WriteLine("Counting lines in files...");

                    int totalLines = 0;
                    foreach (var pattern in fileExtensions)
                    {
                        var files = Directory.GetFiles(settings.DirectoryPath, pattern, SearchOption.AllDirectories);
                        int linesCount = files.Sum(file => File.ReadLines(file).Count());

                        totalLines += linesCount;

                        Console.WriteLine($"{pattern}: {linesCount.ToString("N")} lines in {files.Length.ToString("N")} files.");
                    }

                    Console.WriteLine($"Total lines in all files: {totalLines.ToString("N")}\n");
                    continue;
                }

                string keyword = input;

                // Get all files with the specified extensions in the directory and its subdirectories
                var filesToSearch = fileExtensions.SelectMany(pattern => Directory.GetFiles(settings.DirectoryPath, pattern, SearchOption.AllDirectories)).ToArray();

                Console.WriteLine($"Searching for \"{keyword}\" in {string.Join(", ", fileExtensions)} files...\n");

                bool found = false;
                foreach (var file in filesToSearch)
                {
                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            if (!found)
                            {
                                found = true;
                            }
                            Console.WriteLine($"Keyword found in file: {file}");
                            Console.WriteLine($"Line {i + 1}: {lines[i]}");
                            Console.WriteLine();
                        }
                    }
                }

                if (!found)
                {
                    Console.WriteLine("No occurrences found.");
                }

                Console.WriteLine("Search complete.\n");
            }
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
    }

    class Settings
    {
        public string DirectoryPath { get; set; }
        public string[] FileExtensions { get; set; } = { "*.cs", "*.js", "*.css" };
    }
}
