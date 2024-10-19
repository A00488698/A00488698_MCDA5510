using System;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using System.Linq;

namespace Assignment1
{
    public class DirWalker
    {
        private string outputFilePath;
        private string dataPath;
        private string logFilePath;
        private int totalRows = 0;
        private int totalValidRows = 0;
        private int totalSkippedRows = 0;
        private int totalSkippedSpecialRows = 0;
        private int totalSkippedHeaderRows = 0;
        private int incompleteRows=0;
        public DirWalker(string outputFilePath, string logFilePath, string dataPath)
        {
            this.outputFilePath = outputFilePath;
            this.logFilePath = logFilePath;
            this.dataPath = dataPath;
        }

        public void Walk(string path)
        {
           
                var stopwatch = Stopwatch.StartNew();
            var logMessages = new List<string>();
            try
            {
                string[] directories = Directory.GetDirectories(path);
                if (directories == null) return;


                foreach (string dirPath in directories)
                {
                    Walk(dirPath);
                    //LogTime($"Time taken to read directory: {dirPath} - {stopwatch.ElapsedMilliseconds} ms", logMessages, this.logFilePath);
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("The file or directory cannot be found.");
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("The file or directory cannot be found.");
            }
            catch (DriveNotFoundException)
            {
                Console.WriteLine("The drive specified in 'path' is invalid.");
            }
            catch (PathTooLongException)
            {
                Console.WriteLine("'path' exceeds the maxium supported path length.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("You do not have permission to create this file.");
            }
            catch (IOException e) when ((e.HResult & 0x0000FFFF) == 32)
            {
                Console.WriteLine("There is a sharing violation.");
            }
            catch (IOException e) when ((e.HResult & 0x0000FFFF) == 80)
            {
                Console.WriteLine("The file already exists.");
            }
            catch (IOException e)
            {
                Console.WriteLine($"An exception occurred:\nError code: " +
                                  $"{e.HResult & 0x0000FFFF}\nMessage: {e.Message}");
            }
            // deal with csv in path
            string[] files = Directory.GetFiles(path, "*.csv");
            foreach (string filePath in files)
            {
                ProcessFile(filePath, logMessages);

            }

            stopwatch.Stop();

            if (path == dataPath) 
            {
                // LogTime($"Time taken to read directory: {path} - {stopwatch.ElapsedMilliseconds} ms", logMessages, this.logFilePath);

                LogSummary(stopwatch.ElapsedMilliseconds, logMessages);
            }
        }

        private void ProcessFile(string filePath, List<string> logMessages)
        {
            try
            {
                using (TextFieldParser parser = new TextFieldParser(filePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        char[] specialChars = { '!', '*', '#', '/', '\\' };
                        if (totalRows == 0)
                        {
                            string header = "First Name,Last Name,Street Number,Street,City,Province,Country,Postal Code,Phone Number,Email Address,date";
                            File.WriteAllText(outputFilePath, header + Environment.NewLine);
                        }
                        if (fields[0] == "First Name")
                        {
                            totalSkippedRows++;
                            totalRows++;
                            totalSkippedHeaderRows++;
                            LogTime($"Skipped each header in file: {filePath}", logMessages, this.logFilePath);
                            continue;

                        }
                        bool IsSpecialChar(string[] field)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                if (specialChars.Any(c => field[i].Contains(c)))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }

                        if (Array.Exists(fields, string.IsNullOrEmpty)) 
                        {
                            totalSkippedRows++;
                            totalRows++;
                            incompleteRows++;
                            LogTime($"Skipped incomplete record in file: {filePath}", logMessages, this.logFilePath);

                            /*int j = 0;
                            foreach (string i in fields) {
                                j++;
                                System.Console.WriteLine(i);
                                System.Console.WriteLine(j);
                            }
                            System.Console.WriteLine(fields.Length);*/
                            //  Console.WriteLine(fields[0] +","+ fields[1] + "," + fields[2] + "," + fields[3] + "," + fields[4] + ","  + fields[5] + "," + fields[6] + "," + fields[7] + "," + fields[8] + "," + fields[9] + ","  + fields.Length);
                            continue;
                        }
                        if (IsSpecialChar(fields))
                        {
                            totalSkippedRows++;
                            totalSkippedSpecialRows++;
                            totalRows++;
                            LogTime($"Skipped special record in file: {filePath}", logMessages, this.logFilePath);
                            continue;
                        }
                       
                        // add date
                        string dateColumn = DateTime.Now.ToString("yyyy/MM/dd");
                        string outputLine = string.Join(",", fields) + "," + dateColumn;
                        File.AppendAllText(outputFilePath, outputLine + Environment.NewLine);
                        totalValidRows++;
                        totalRows++;
                    }
                }
            }
            catch (Exception ex)
            {
                LogTime($"Error processing file {filePath}", logMessages, this.logFilePath);
            }
        }

        private void LogTime(string message, List<string> logMessages, string logFilePath)
        {
            //Console.WriteLine(message);
            logMessages.Add(message); 

            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(message); 
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Error: Access to log file denied. Check permissions for {logFilePath}.");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }
               private void LogSummary(long totalTime, List<string> logMessages)
        {
            string summaryMessage = $"Total execution time: {totalTime} ms{Environment.NewLine}" +
                 $"Total rows: {totalRows}{Environment.NewLine}" +
     $"Total valid rows: {totalValidRows}{Environment.NewLine}" +
     $"Total skipped rows: {totalSkippedRows}{Environment.NewLine}" +
     $"Header rows: {totalSkippedHeaderRows}{Environment.NewLine}" +
     $"Special rows: {totalSkippedSpecialRows}{Environment.NewLine}"+
     $"Incomplete rows: {incompleteRows}{Environment.NewLine}";
            Console.WriteLine(summaryMessage);
            logMessages.Add(summaryMessage);
            LogTime(summaryMessage, logMessages, this.logFilePath);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string dataPath = @"\VSprojects\ProgAssign1\Sample Data"; 
            string outputFilePath = @"\VSprojects\ProgAssign1\Output\output.csv"; 
            string logFilePath = @"\VSprojects\ProgAssign1\Logs\log.txt"; 

            if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)); 
            }

            if (!Directory.Exists(logFilePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)); 
            }

            DirWalker dirWalker = new DirWalker(outputFilePath,logFilePath,dataPath);

            dirWalker.Walk(dataPath); 
        }
    }
}
