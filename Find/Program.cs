using System;
using System.IO;
using System.Linq;

namespace FileSearchApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Ask for the directory to search in
            Console.Write("Enter the directory to search in: ");
            string directoryPath = Console.ReadLine();

            // Ask for the keyword to search for
            Console.Write("Enter the keyword to search for: ");
            string keyword = Console.ReadLine();

            if (string.IsNullOrEmpty(directoryPath) || string.IsNullOrEmpty(keyword))
            {
                Console.WriteLine("Invalid directory or keyword. Exiting application.");
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Directory does not exist. Exiting application.");
                return;
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
    }
}
