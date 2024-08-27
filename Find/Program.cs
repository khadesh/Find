using System;
using System.IO;

namespace FileSearchApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string settingsFilePath = "program.settings";
            string directoryPath = null;

            if (File.Exists(settingsFilePath))
            {
                directoryPath = File.ReadAllText(settingsFilePath);
                Console.WriteLine($"Using last used directory: {directoryPath}");
            }

            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                Console.Write("Enter the directory to search in: ");
                directoryPath = Console.ReadLine();

                if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                {
                    Console.WriteLine("Invalid directory. Exiting application.");
                    return;
                }

                SaveLastUsedDirectory(settingsFilePath, directoryPath);
            }

            // Ask for the keyword to search for or change directory
            Console.Write("Enter the keyword to search for (or type 'd' to change directory): ");
            string keyword = Console.ReadLine();

            if (string.IsNullOrEmpty(keyword))
            {
                Console.WriteLine("Invalid keyword. Exiting application.");
                return;
            }

            if (keyword.ToLower() == "d")
            {
                Console.Write("Enter the new directory to search in: ");
                directoryPath = Console.ReadLine();

                if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
                {
                    Console.WriteLine("Invalid directory. Exiting application.");
                    return;
                }

                SaveLastUsedDirectory(settingsFilePath, directoryPath);

                // Ask for the keyword to search for
                Console.Write("Enter the keyword to search for: ");
                keyword = Console.ReadLine();

                if (string.IsNullOrEmpty(keyword))
                {
                    Console.WriteLine("Invalid keyword. Exiting application.");
                    return;
                }
            }

            // Get all .cs files in the directory and its subdirectories
            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);

            Console.WriteLine($"Searching for \"{keyword}\" in .cs files...\n");

            foreach (var file in csFiles)
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"Keyword found in file: {file}");
                        Console.WriteLine($"Line {i + 1}: {lines[i]}");
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("Search complete.");
        }

        static void SaveLastUsedDirectory(string filePath, string directory)
        {
            File.WriteAllText(filePath, directory);
        }
    }
}
