using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace StockScanningResultMonitor
{

    public class StockScanningResultMonitor
    {
        private string stockSymbolPattern = @"- (\b[A-Z]+\b): (\d+)";
        private string indicatorLead = @"(?i)- (EMAS|AROON|MACD)";
        private string crossPattern = @"(?i)CROSS.+?(ABOVE|BELOW|UNDER)";

        public void PerformAnalysis(string directoryPath)
        {
            List<string> files = GetFilesRecursively(directoryPath);

            foreach (var file in files)
            {
                var lines = File.ReadAllLines(file);
                if (file.Contains("favorites"))
                {
                    continue;
                }

                foreach (var line in lines)
                {
                    Match stockSymbolMatch = Regex.Match(line, stockSymbolPattern);
                    if (stockSymbolMatch.Success)
                    {
                        string stockSymbol = stockSymbolMatch.Groups[1].Value;
                        string rating = stockSymbolMatch.Groups[2].Value;

                        Console.WriteLine("Stock: " + stockSymbol);
                        Console.WriteLine("Rating: " + rating);
                    }

                    MatchCollection indicatorMatch = Regex.Matches(line, indicatorLead);
                    if (indicatorMatch.Count > 0)
                    {
                        Console.WriteLine("Cross Indicators:");
                        foreach (Match match in indicatorMatch)
                        {
                            string indicator = match.Groups[1].Value;

                            Console.WriteLine("- " + indicator);
                        }
                    }

                    MatchCollection crossMatches = Regex.Matches(line, crossPattern);
                    if (crossMatches.Count > 0)
                    {
                        Console.WriteLine("Cross type:");
                        foreach (Match match in crossMatches)
                        {
                            string crossType = match.Groups[1].Value;

                            Console.WriteLine("- " + crossType);
                        }
                    }
                }
            }
        }

        private List<string> GetFilesRecursively(string folderPath)
        {
            List<string> files = new List<string>();

            try
            {
                files.AddRange(Directory.GetFiles(folderPath));

                foreach (string subDirectory in Directory.GetDirectories(folderPath))
                {
                    files.AddRange(GetFilesRecursively(subDirectory));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting files in folder: {ex.Message}");
            }

            return files;
        }

        private string ReadFileContent(string filePath)
        {
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    return sr.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return string.Empty;
            }
        }
    }

}
