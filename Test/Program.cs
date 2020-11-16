using System;
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
                string filename = InputString("Filename", false);
                IndexEngine ie = new IndexEngine(filename); 
                ie.Logger = Logger;

                bool runForever = true;
                while (runForever)
                {
                    // Console.WriteLine("1234567890123456789012345678901234567890123456789012345678901234567890123456789");
                    string userInput = InputString("Command [? for help]", false);

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
                            ie.Add(doc);
                            break;

                        case "addfile async":
                            doc = BuildFileDocument();
                            ie.AddAsync(doc).Wait();
                            break;

                        case "addtext":
                            doc = BuildStringDocument();
                            ie.Add(doc);
                            break;

                        case "del":
                            guid = InputString("GUID", false);
                            ie.DeleteDocumentByGuid(guid);
                            break;

                        case "threads":
                            Console.WriteLine("Active document processing jobs: " + ie.CurrentIndexingThreads + "/" + ie.MaxIndexingThreads);
                            Console.WriteLine("Documents being processed:");
                            activeDocs = ie.DocumentsIndexing.ToList();
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
                            results = ie.Search(terms);
                            if (results == null || results.Count < 1)
                            {
                                Console.WriteLine("No results");
                            }
                            else
                            {
                                foreach (Document curr in results)
                                {
                                    Console.WriteLine(curr.ToJson());
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
                Console.WriteLine("Exception:" + Environment.NewLine + e.ToString()); 
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
            Console.WriteLine("--- Available Commands ---");
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
         
        static Document BuildFileDocument()
        {
            Console.Write("File: ");
            string file = Console.ReadLine();
            if (String.IsNullOrEmpty(file)) return null;

            byte[] data = File.ReadAllBytes(file);

            Document ret = new Document(
                InputString("GUID", true),
                InputString("Title", false),
                InputString("Description", false),
                file,
                InputString("Source", false),
                InputString("AddedBy", false),
                data);

            return ret;
        }

        static Document BuildStringDocument()
        {
            Document ret = new Document(
                InputString("GUID", true),
                InputString("Title", false),
                InputString("Description", false),
                "String input",
                InputString("Source", false),
                InputString("AddedBy", false),
                Encoding.UTF8.GetBytes(InputString("Data", false)));

            return ret;
        }

        static List<string> GatherTerms()
        {
            Console.WriteLine("Press ENTER on an empty line to end");
            List<string> ret = new List<string>();
            while (true)
            {
                string term = InputString("Term", true);
                if (!String.IsNullOrEmpty(term))
                {
                    ret.Add(term);
                    continue;
                }
                break;
            }
            return ret;
        }

        static string InputString(string prompt, bool allowNull)
        {
            Console.Write(prompt + ": ");
            while (true)
            {
                string input = Console.ReadLine();
                if (String.IsNullOrEmpty(input) && !allowNull) continue;
                return input;
            }
        }

        static void Logger(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
