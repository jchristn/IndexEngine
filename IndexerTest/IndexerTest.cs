using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indexer;

namespace IndexerTest
{
    public class IndexerTest
    {
        static void Main(string[] args)
        {
            try
            {
                string filename = "";
                Console.Write("Filename: ");
                filename = Console.ReadLine();
                if (String.IsNullOrEmpty(filename)) return;
                IndexEngine ie = new IndexEngine("index");
                ie.ConsoleDebug = true;

                bool runForever = true;
                while (runForever)
                {
                    // Console.WriteLine("1234567890123456789012345678901234567890123456789012345678901234567890123456789");
                    Console.WriteLine("Commands: q cls addfile addtext del threads search");
                    string userInput = UserInputString("Command", false);

                    Document doc = new Document();
                    List<Document> results = new List<Document>();
                    List<string> terms = new List<string>();
                    List<string> activeDocs = new List<string>();
                    string guid = "";

                    switch (userInput.ToLower())
                    {
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
            if (data == null || data.Length < 1) return null;

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
