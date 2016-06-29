using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexer
{
    public class IndexEngine
    {
        #region Constructor

        public IndexEngine(string databaseFile)
        {
            if (String.IsNullOrEmpty(databaseFile))
            {
                throw new ArgumentNullException(databaseFile);
            }

            DatabaseFile = databaseFile;
            DatabaseWrapper = new SqliteWrapper(databaseFile);
            ProcessingThreadsCount = 0;
            DocumentsProcessing = new List<string>();

            // initialize tables
            if (!CreateDocumentTable()) throw new Exception("Unable to create docs table in " + DatabaseFile);
            if (!CreateIndexTable()) throw new Exception("Unable to create index table in " + DatabaseFile);
        }

        #endregion

        #region Public-Class-Members

        public string DatabaseFile { get; set; }
        public bool ConsoleDebug { get; set; }
        
        #endregion

        #region Private-Class-Members

        private SqliteWrapper DatabaseWrapper { get; set; }
        private int ProcessingThreadsCount { get; set; }
        private List<string> DocumentsProcessing { get; set; }

        #endregion

        #region Public-Methods

        public void SubmitDocument(Document curr)
        {
            if (curr == null) throw new ArgumentNullException("curr");
            Task.Run(() => ProcessSubmittedDocument(curr, null));
            return;
        }

        public void SubmitDocument(Document curr, List<string> tags)
        {
            if (curr == null) throw new ArgumentNullException("curr");
            Task.Run(() => ProcessSubmittedDocument(curr, tags));
            return;
        }

        public int GetProcessingThreadsCount()
        {
            return ProcessingThreadsCount;
        }

        public List<string> GetProcessingDocumentsList()
        {
            if (DocumentsProcessing == null || DocumentsProcessing.Count < 1) return null;
            List<string> ret = new List<string>();
            foreach (string curr in DocumentsProcessing)
            {
                ret.Add(curr);
            }
            return ret;
        }

        public List<string> RetrieveDocumentGuidsByTerms(List<string> terms, List<Tuple<string, string, string>> filters)
        {
            #region Check-for-Null-Values

            if (terms == null || terms.Count < 1) throw new ArgumentNullException("terms");

            #endregion

            #region Build-GUIDs-Query

            string query =
                "SELECT docs_guid FROM index_entries " +
                "WHERE " +
                "(";    // open paren

            int termsCount = 0;
            query += "  term IN (";
            foreach (string term in terms)
            {
                if (termsCount == 0) query += "'" + DatabaseWrapper.SanitizeString(term.ToLower()) + "'";
                else query += ",'" + DatabaseWrapper.SanitizeString(term.ToLower()) + "'";
                termsCount++;
            }
            query += ") ";

            if (filters != null && filters.Count > 0)
            {
                int filtersCount = 0;
                foreach (Tuple<string, string, string> filter in filters)
                {
                    query += "AND " + 
                        DatabaseWrapper.SanitizeString(filter.Item1) + " " +
                        DatabaseWrapper.SanitizeString(filter.Item2) + " " +
                        "'" + DatabaseWrapper.SanitizeString(filter.Item3) + "'";
                    filtersCount++;
                }
            }

            // end paren
            query += ")";

            #endregion

            #region Retrieve-Records

            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("RetrieveDocumentGuidsByTerms failure while retrieving document GUIDs by supplied terms");
                return null;
            }

            #endregion

            #region Return-List

            List<string> ret = new List<string>();
            if (result.Columns.Contains("docs_guid"))
            {
                foreach (DataRow curr in result.Rows)
                {
                    ret.Add(curr["docs_guid"].ToString().ToLower());
                }
            }

            return ret;

            #endregion
        }

        public List<Document> SearchIndex(List<string> terms, List<Tuple<string, string, string>> filters)
        {
            #region Check-for-Null-Values

            if (terms == null || terms.Count < 1) throw new ArgumentNullException("terms");

            #endregion

            #region Retrieve-GUIDs

            List<string> guids = RetrieveDocumentGuidsByTerms(terms, filters);
            if (guids == null || guids.Count < 1)
            {
                Log("SearchIndex no document GUIDs found for the supplied terms");
                return null;
            }

            #endregion

            #region Build-Documents-Query

            string query =
                "SELECT * FROM docs " +
                "WHERE " +
                "(";    // open paren
                
            int guidCount = 0;
            query += "  guid IN (";
            foreach (string guid in guids)
            {
                if (guidCount == 0) query += "'" + guid.ToLower() + "'";
                else query += ",'" + guid.ToLower() + "'";
                guidCount++;
            }
            query += ") ";

            if (filters != null && filters.Count > 0)
            {
                int filtersCount = 0;
                foreach (Tuple<string, string, string> filter in filters)
                {
                    query += "AND " + filter.Item1 + " " + filter.Item2 + " '" + filter.Item3 + "'";
                    filtersCount++;
                }
            }

            // end paren
            query += ")";

            #endregion

            #region Retrieve-Records

            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("SearchIndex failure while retrieving documents by supplied terms");
                return null;
            }

            #endregion

            #region Return-List

            List<Document> ret = DataTableToDocumentsList(result);
            return ret;

            #endregion
        }

        public void DeleteDocumentByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException("guid");
            string query = "DELETE FROM index_entries WHERE docs_guid = '" + DatabaseWrapper.SanitizeString(guid).ToLower() + "'";
            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("DeleteDocumentByGuid unable to execute query to delete from index entries table");
                return;
            }

            query = "DELETE FROM docs WHERE guid = '" + DatabaseWrapper.SanitizeString(guid).ToLower() + "'";
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("DeleteDocumentByGuid unable to execute query to delete from docs table");
                return;
            }

            return;
        }
        
        public void DeleteDocumentByHandle(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException("handle");
            Document curr = RetrieveDocumentByHandle(handle);
            if (curr == null)
            {
                Log("DeleteDocumentByHandle unable to find document with handle " + handle);
                return;
            }

            string query = "DELETE FROM index_entries WHERE docs_guid = '" + DatabaseWrapper.SanitizeString(curr.ReferenceGuid.ToLower()) + "'";
            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("DeleteDocumentByGuid unable to execute query to delete from index entries table");
                return;
            }

            query = "DELETE FROM docs WHERE guid = '" + DatabaseWrapper.SanitizeString(curr.ReferenceGuid).ToLower() + "'";
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("DeleteDocumentByGuid unable to execute query to delete from docs table");
                return;
            }

            return;
        }

        public Document RetrieveDocumentByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException("guid");
            string query = "SELECT * FROM docs WHERE docs_guid = '" + DatabaseWrapper.SanitizeString(guid.ToLower()) + "'";
            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("RetrieveDocumentByGuid unable to execute query to retrieve document " + guid + " from docs table");
                return null;
            }

            List<Document> retList = DataTableToDocumentsList(result);
            if (retList == null)
            {
                return null;
            }
            else
            {
                if (retList.Count > 1) Log("RetrieveDocumentByGuid multiple documents found with GUID " + guid + ", returning first");
                Document ret = new Document();
                foreach (Document curr in retList)
                {
                    ret = curr;
                    break;
                }
                return ret;
            }
        }
        
        public Document RetrieveDocumentByHandle(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException("handle");
            string query = "SELECT * FROM docs WHERE handle = '" + DatabaseWrapper.SanitizeString(handle.ToLower()) + "'";
            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("RetrieveDocumentByHandle unable to execute query to retrieve document handle " + handle + " from docs table");
                return null;
            }

            if (result == null || result.Rows.Count < 1)
            {
                return null;
            }

            List<Document> retList = DataTableToDocumentsList(result);
            if (retList == null)
            {
                return null;
            }
            else
            {
                if (retList.Count > 1) Log("RetrieveDocumentByHandle multiple documents found with handle " + handle + ", returning first");
                Document ret = new Document();
                foreach (Document curr in retList)
                {
                    ret = curr;
                    break;
                }
                return ret;
            }
        }

        public bool IsHandleIndexed(string handle)
        {
            if (String.IsNullOrEmpty("handle")) throw new ArgumentNullException("handle");
            string query = "SELECT guid FROM docs WHERE handle = '" + DatabaseWrapper.SanitizeString(handle.ToLower()) + "'";
            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("IsHandleIndexed unable to execute query to find handle");
                return false;
            }

            if (result == null || result.Rows == null || result.Rows.Count < 1) return false;
            return true;
        }

        public int GetReferenceCountByTerm(string term)
        {
            if (String.IsNullOrEmpty("term")) throw new ArgumentNullException("term");
            string query = "SELECT COUNT(*) AS num_entries FROM index_entries WHERE term = '" + DatabaseWrapper.SanitizeString(term.ToLower()) + "'";
            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("GetReferenceCountByTerm unable to execute query to find reference count for supplied term");
                return 0;
            }

            if (result == null || result.Rows == null || result.Rows.Count < 1) return 0;

            int num_entries = 0;
            if (result.Columns.Contains("num_entries"))
            {
                foreach (DataRow row in result.Rows)
                {
                    num_entries = Convert.ToInt32(row["num_entries"]);
                    break;
                }
            }
            else
            {
                Log("GetReferenceCountByTerm result table is missing required fields");
            }

            return num_entries;
        }
        
        #endregion

        #region Private-Methods

        private void ProcessSubmittedDocument(Document doc, List<string> tags)
        {
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;
            int termsRecorded = 0;
            int termsTotal = 0;
            bool listAdded = false;

            try
            {
                #region Setup

                ProcessingThreadsCount++;
                if (doc == null) throw new ArgumentNullException("doc");
                if (doc.Data == null || doc.Data.Length < 1) throw new ArgumentNullException("Data");

                DocumentsProcessing.Add(doc.Title);
                listAdded = true;

                Dictionary<string, int> terms = new Dictionary<string, int>();

                #endregion

                #region Process-Tags

                if (tags != null && tags.Count > 0)
                {
                    foreach (string curr in tags)
                    {
                        if (String.IsNullOrEmpty(curr)) continue;
                        if (curr.Length < 3) continue;

                        if (!terms.ContainsKey(curr.ToLower()))
                        {
                            terms.Add(curr.ToLower(), 1);
                        }
                        else
                        {
                            int refcount = terms[curr.ToLower()];
                            refcount = refcount + 1;
                            terms.Remove(curr.ToLower());
                            terms.Add(curr.ToLower(), refcount);
                        }
                    }
                }

                Log("ProcessSubmittedDocument finished processing tags");

                #endregion

                #region Process-Content

                string content = Encoding.UTF8.GetString(doc.Data);
                char[] delimiters = new char[] { '\r', '\n', ' ', '?', '\'', '\"', '.', ',', ';' };
                string[] termsRaw = content.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                List<string> termsAlphaOnly = new List<string>();

                if (termsRaw != null && termsRaw.Length > 0)
                {
                    foreach (string curr in termsRaw)
                    {
                        if (String.IsNullOrEmpty(curr)) continue;
                        if (curr.Length < 3) continue;

                        string currAlphaOnly = AlphaOnlyString(curr);
                        if (String.IsNullOrEmpty(currAlphaOnly)) continue;
                        termsAlphaOnly.Add(currAlphaOnly);
                    }

                    if (termsAlphaOnly != null && termsAlphaOnly.Count > 0)
                    {
                        foreach (string curr in termsAlphaOnly)
                        {
                            if (!terms.ContainsKey(curr.ToLower()))
                            {
                                terms.Add(curr.ToLower(), 1);
                            }
                            else
                            {
                                int refcount = terms[curr.ToLower()];
                                refcount = refcount + 1;
                                terms.Remove(curr.ToLower());
                                terms.Add(curr.ToLower(), refcount);
                            }
                        }
                    }
                }

                if (terms == null || terms.Count < 1)
                {
                    Log("ProcessSubmittedDocument no tags submitted or no terms found");
                    return;
                }

                Log("ProcessSubmittedDocument successfully extracted terms");

                #endregion

                #region Remove-Existing-Entries

                DeleteDocumentByGuid(doc.ReferenceGuid);
                Log("ProcessSubmittedDocument deleted document by GUID " + doc.ReferenceGuid);

                DeleteDocumentByHandle(doc.Handle);
                Log("ProcessSubmittedDocument deleted document by handle " + doc.Handle);

                #endregion

                #region Create-New-Document-Entry

                string query =
                    "INSERT INTO docs " +
                    "(title, description, handle, source, added_by, guid, added) " +
                    "VALUES " +
                    "(" +
                    "'" + DatabaseWrapper.SanitizeString(doc.Title) + "'," +
                    "'" + DatabaseWrapper.SanitizeString(doc.Description) + "'," +
                    "'" + DatabaseWrapper.SanitizeString(doc.Handle) + "'," +
                    "'" + DatabaseWrapper.SanitizeString(doc.Source) + "'," +
                    "'" + DatabaseWrapper.SanitizeString(doc.AddedBy) + "'," +
                    "'" + DatabaseWrapper.SanitizeString(doc.ReferenceGuid) + "'," +
                    "'" + doc.Added.ToString("MM/dd/yyyy HH:mm:ss") + "'" +
                    ")";

                DataTable result = null;
                if (!DatabaseWrapper.ExecuteQuery(query, out result))
                {
                    Log("ProcessSubmittedDocument unable to execute INSERT query for docs entry");
                    return;
                }

                Log("ProcessSubmittedDocument submitted docs entry");

                #endregion

                #region Create-New-Terms-Entries

                termsTotal = terms.Count;
                Log("ProcessSubmittedDocument detected " + termsTotal + " terms in document");

                int batchSize = 1000;    // we can make this configurable later
                Dictionary<string, int> tempDict = new Dictionary<string, int>();
                while (termsRecorded < termsTotal)
                {
                    if (tempDict.Count >= batchSize)
                    {
                        #region Drain-the-Batch

                        query =
                            "INSERT INTO index_entries (term, refcount, docs_guid) VALUES ";

                        int termsAdded = 0;
                        foreach (KeyValuePair<string, int> currKvp in tempDict)
                        {
                            if (termsAdded == 0)
                            {
                                query +=
                                    "('" + DatabaseWrapper.SanitizeString(currKvp.Key) + "'," +
                                    currKvp.Value + "," +
                                    "'" + DatabaseWrapper.SanitizeString(doc.ReferenceGuid) + "')";
                            }
                            else
                            {
                                query +=
                                    ",('" + DatabaseWrapper.SanitizeString(currKvp.Key) + "'," +
                                    currKvp.Value + "," +
                                    "'" + DatabaseWrapper.SanitizeString(doc.ReferenceGuid) + "')";
                            }

                            termsAdded++;
                        }
                        
                        result = null;
                        if (!DatabaseWrapper.ExecuteQuery(query, out result))
                        {
                            Log("ProcessSubmittedDocument unable to execute INSERT query for index terms entry");
                            return;
                        }
                        else
                        {
                            Log("ProcessSubmittedDocument processed " + termsRecorded + "/" + termsTotal + " terms");
                            tempDict = new Dictionary<string, int>();
                        }

                        #endregion
                    }
                    else
                    {
                        #region Add-to-Batch

                        string key = terms.Keys.ElementAt(termsRecorded);
                        int val = terms.Values.ElementAt(termsRecorded);
                        tempDict.Add(key, val);
                        termsRecorded++;

                        #endregion
                    }
                }

                #endregion

                #region Submit-Remaining-Terms

                if (tempDict != null && tempDict.Count >= 0)
                {
                    #region Drain-Remainder

                    query =
                        "INSERT INTO index_entries (term, refcount, docs_guid) VALUES ";

                    int termsAdded = 0;
                    foreach (KeyValuePair<string, int> currKvp in tempDict)
                    {
                        if (termsAdded == 0)
                        {
                            query +=
                                "('" + DatabaseWrapper.SanitizeString(currKvp.Key) + "'," +
                                currKvp.Value + "," +
                                "'" + DatabaseWrapper.SanitizeString(doc.ReferenceGuid) + "')";
                        }
                        else
                        {
                            query +=
                                ",('" + DatabaseWrapper.SanitizeString(currKvp.Key) + "'," +
                                currKvp.Value + "," +
                                "'" + DatabaseWrapper.SanitizeString(doc.ReferenceGuid) + "')";
                        }

                        termsAdded++;
                    }

                    result = null;
                    if (!DatabaseWrapper.ExecuteQuery(query, out result))
                    {
                        Log("ProcessSubmittedDocument unable to execute INSERT query for remaining index terms entry");
                        return;
                    }
                    else
                    {
                        Log("ProcessSubmittedDocument processed " + termsRecorded + "/" + termsTotal + " terms");
                        tempDict = new Dictionary<string, int>();
                    }

                    #endregion
                }

                #endregion

                return;
            }
            catch (Exception e)
            {
                LogException(e);
            }
            finally
            {
                ProcessingThreadsCount--;
                if (listAdded) DocumentsProcessing.Remove(doc.Title);
                endTime = DateTime.Now;
                TimeSpan ts = (endTime - startTime);

                decimal msTotal = Convert.ToDecimal(ts.TotalMilliseconds.ToString("F"));
                decimal msPerTerm = Convert.ToDecimal((msTotal / termsRecorded).ToString("F"));
                Log("ProcessSubmittedDocument processed " + termsRecorded + "/" + termsTotal + " terms (" + msTotal + "ms total, " + msPerTerm + "ms/term)");
            }
        }

        private bool CreateDocumentTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS docs " +
                "(" +
                "  docs_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "  title VARCHAR(256), " +
                "  description VARCHAR(1024), " +
                "  handle VARCHAR(256), " +
                "  source VARCHAR(32), " +
                "  added_by VARCHAR(32), " +
                "  guid VARCHAR(64), " +
                "  added VARCHAR(32) " + 
                ")";

            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("CreateDocumentTable unable to create docs table in " + DatabaseFile);
                return false;
            }

            query = "PRAGMA journal_mode = TRUNCATE";
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("CreateDocumentTable unable change pragma journal mode for docs table in " + DatabaseFile);
                return false;
            }

            return true;
        }

        private bool CreateIndexTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS index_entries " +
                "(" +
                "  index_entries_id INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "  term VARCHAR(256), " +
                "  refcount INTEGER, " +
                "  docs_guid VARCHAR(64) " +
                ")";

            DataTable result = null;
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("CreateIndexTable unable to create index table in " + DatabaseFile);
                return false;
            }

            query = "PRAGMA journal_mode = TRUNCATE";
            if (!DatabaseWrapper.ExecuteQuery(query, out result))
            {
                Log("CreateIndexTable unable change pragma journal mode for index table in " + DatabaseFile);
                return false;
            }

            return true;
        }
        
        private List<Document> DataTableToDocumentsList(DataTable table)
        {
            if (table == null || table.Rows == null || table.Rows.Count < 1) return null;

            List<Document> ret = new List<Document>();
            if (table.Columns.Contains("docs_id")
                && table.Columns.Contains("title")
                && table.Columns.Contains("description")
                && table.Columns.Contains("handle")
                && table.Columns.Contains("source")
                && table.Columns.Contains("added_by")
                && table.Columns.Contains("guid")
                && table.Columns.Contains("added")
                )
            {
                foreach (DataRow curr in table.Rows)
                {
                    Document doc = DataRowToDocument(curr);
                    if (doc != null) ret.Add(doc);
                }
            }
            else
            {
                Log("DataTableToDocumentsList supplied table is missing required fields");
            }

            return ret;
        }

        private Document DataRowToDocument(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            Document doc = new Document();
            doc.DocumentID = Convert.ToInt32(row["docs_id"]);
            doc.Title = row["title"].ToString();
            doc.Description = row["description"].ToString();
            doc.Handle = row["handle"].ToString();
            doc.Source = row["source"].ToString();
            doc.AddedBy = row["added_by"].ToString();
            doc.ReferenceGuid = row["guid"].ToString();
            doc.Added = Convert.ToDateTime(row["added"]);
            return doc;
        }

        private string AlphaOnlyString(string dirty)
        {
            if (String.IsNullOrEmpty(dirty)) return null;
            string clean = null;
            for (int i = 0; i < dirty.Length; i++)
            {
                int val = (int)(dirty[i]);

                if (
                    ((val > 64) && (val < 91))          // A...Z
                    || ((val > 96) && (val < 123))      // a...z
                    )
                {
                    clean += dirty[i];
                }
            }

            return clean;
        }

        private void Log(string message)
        {
            if (ConsoleDebug) Console.WriteLine(message);
        }

        private void LogException(Exception e)
        {
            Log("================================================================================");
            Log(" = Exception Type: " + e.GetType().ToString());
            Log(" = Exception Data: " + e.Data);
            Log(" = Inner Exception: " + e.InnerException);
            Log(" = Exception Message: " + e.Message);
            Log(" = Exception Source: " + e.Source);
            Log(" = Exception StackTrace: " + e.StackTrace);
            Log("================================================================================");
        }

        #endregion
    }
}
