﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indexer;

namespace Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string filename = "";
                Console.Write("Filename: ");
                filename = Console.ReadLine();
                if (String.IsNullOrEmpty(filename)) return;
                IndexEngine ie = new IndexEngine(filename); 
                ie.ConsoleDebug = true;

                bool runForever = true;
                while (runForever)
                {
                    // Console.WriteLine("1234567890123456789012345678901234567890123456789012345678901234567890123456789");
                    string userInput = UserInputString("Command", false);

                    Document doc = new Document();
                    List<Document> results = new List<Document>();
                    List<string> terms = new List<string>();
                    List<string> activeDocs = new List<string>();
                    string guid = "";

                    switch (userInput.ToLower())
                    {
                        case "?":
                            Menu();
                            break;

                        case "q":
                        case "quit":
                            runForever = false;
                            break;

                        case "c":
                        case "cls":
                            Console.Clear();
                            break;

                        case "addfile":
                            doc = BuildFileDocument();
                            ie.SubmitDocument(doc);
                            break;

                        case "addtext":
                            doc = BuildStringDocument();
                            ie.SubmitDocument(doc);
                            break;

                        case "del":
                            guid = UserInputString("GUID", false);
                            ie.DeleteDocumentByGuid(guid);
                            break;

                        case "threads":
                            Console.WriteLine("Active document processing jobs: " + ie.GetProcessingThreadsCount());
                            Console.WriteLine("Documents:");
                            activeDocs = ie.GetProcessingDocumentsList();
                            if (activeDocs != null && activeDocs.Count > 0)
                            {
                                foreach (string curr in activeDocs) Console.WriteLine("  " + curr);
                            }
                            else
                            {
                                Console.WriteLine("(null)");
                            }
                            break;

                        case "search":
                            terms = GatherTerms();
                            if (terms == null || terms.Count < 1) break;
                            results = ie.SearchIndex(terms, null);
                            if (results == null || results.Count < 1)
                            {
                                Console.WriteLine("No results");
                            }
                            else
                            {
                                foreach (Document curr in results)
                                {
                                    Console.WriteLine(curr.ToString());
                                }
                            }
                            break;

                        case "exists":
                            Console.Write("Handle: ");
                            string handle = Console.ReadLine();
                            if (!String.IsNullOrEmpty(handle))
                            {
                                Console.WriteLine("Exists: " + ie.IsHandleIndexed(handle));
                            }
                            break;

                        default:
                            break;
                    }

                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
            finally
            {
                Console.WriteLine("");
                Console.WriteLine("Press ENTER to exit");
                Console.ReadLine();
            }
        }

        static void Menu()
        {
            Console.WriteLine("---");
            Console.WriteLine("  q               quit, exit this application");
            Console.WriteLine("  cls             clear the screen");
            Console.WriteLine("  addfile         add a file to the index");
            Console.WriteLine("  addtext         add text to the index");
            Console.WriteLine("  del             delete from the index by GUID");
            Console.WriteLine("  threads         display number of running index threads");
            Console.WriteLine("  search          search for documents by terms");
            Console.WriteLine("  exists          check if a document exists by its handle");
            Console.WriteLine("");
        }

        static void LogException(Exception e)
        {
            Console.WriteLine("================================================================================");
            Console.WriteLine(" = Exception Type: " + e.GetType().ToString());
            Console.WriteLine(" = Exception Data: " + e.Data);
            Console.WriteLine(" = Inner Exception: " + e.InnerException);
            Console.WriteLine(" = Exception Message: " + e.Message);
            Console.WriteLine(" = Exception Source: " + e.Source);
            Console.WriteLine(" = Exception StackTrace: " + e.StackTrace);
            Console.WriteLine("================================================================================");
        }

        static Document BuildFileDocument()
        {
            Console.Write("File: ");
            string file = Console.ReadLine();
            if (String.IsNullOrEmpty(file)) return null;

            byte[] data = File.ReadAllBytes(file);

            Document ret = new Document(
                UserInputString("Title", false),
                UserInputString("Description", false),
                file,
                UserInputString("Source", false),
                UserInputString("AddedBy", false),
                data);

            return ret;
        }

        static Document BuildStringDocument()
        {
            Document ret = new Document(
                UserInputString("Title", false),
                UserInputString("Description", false),
                "String input",
                UserInputString("Source", false),
                UserInputString("AddedBy", false),
                Encoding.UTF8.GetBytes(UserInputString("Data", false)));

            return ret;
        }

        static List<string> GatherTerms()
        {
            Console.WriteLine("Press ENTER on an empty line to end");
            List<string> ret = new List<string>();
            while (true)
            {
                string term = UserInputString("Term", true);
                if (!String.IsNullOrEmpty(term))
                {
                    ret.Add(term);
                    continue;
                }
                break;
            }
            return ret;
        }

        static string UserInputString(string prompt, bool allowNull)
        {
            Console.Write(prompt + ": ");
            while (true)
            {
                string input = Console.ReadLine();
                if (String.IsNullOrEmpty(input) && !allowNull) continue;
                return input;
            }
        }
    }
}
