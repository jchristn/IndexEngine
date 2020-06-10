using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SqliteHelper;

namespace Indexer
{
    /// <summary>
    /// IndexEngine is a lightweight document and text indexing platform written in C# targeted to both .NET Core and .NET Framework.  
    /// IndexEngine uses Sqlite as a storage repository for index data.  
    /// IndexEngine does NOT provide storage of the original documents.
    /// </summary>
    public class IndexEngine : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Method to invoke when logging.
        /// </summary>
        public Action<string> Logger = null;

        /// <summary>
        /// Set the maximum number of threads that can be instantiated to process documents.
        /// </summary>
        public int MaxIndexingThreads
        {
            get
            {
                return _MaxThreads;
            }
            set
            {
                if (value < 1) throw new ArgumentException("MaxThreads must be one or greater.");
                _MaxThreads = value;
            }
        }

        /// <summary>
        /// Get the number of threads currently processing documents.
        /// </summary>
        public int CurrentIndexingThreads
        {
            get
            {
                return _CurrentThreads;
            }
        }

        /// <summary>
        /// Get a list of strings containing the user-supplied GUIDs of each of the documents being processed.
        /// </summary>
        public IEnumerable<string> DocumentsIndexing
        {
            get
            {
                lock (_Lock)
                {
                    IEnumerable<string> ret = _DocumentsIndexing;
                    return ret;
                }
            }
        }

        /// <summary>
        /// Minimum character length for a term to be indexed.
        /// </summary>
        public int TermMinimumLength
        {
            get
            {
                return _TermMinimumLength;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException("TermMinimumLength must be greater than zero.");
                _TermMinimumLength = value;
            }
        }

        /// <summary>
        /// Delimiters to use when identifying terms in a document.  
        /// </summary>
        public char[] TermDelimiters
        {
            get
            {
                return _TermDelimiters;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(TermDelimiters));
                _TermDelimiters = value;
            }
        }

        /// <summary>
        /// List of words to ignore when indexing.
        /// </summary>
        public List<string> IgnoreWords
        {
            get
            {
                return _IgnoreWords;
            }
            set
            {
                if (value == null)
                {
                    _IgnoreWords = new List<string>();
                }
                else
                {
                    _IgnoreWords = value;
                }
            }
        }

        #endregion

        #region Private-Members

        private string _Header = "[IndexEngine] ";
        private CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private CancellationToken _Token;
        private string _DatabaseFilename = null;
        private DatabaseClient _Database = null;
        private int _MaxThreads = 32;
        private int _CurrentThreads = 0;
        private readonly object _Lock = new object();
        private List<string> _DocumentsIndexing = new List<string>();
        private int _TermMinimumLength = 3;
        private char[] _TermDelimiters = new char[] { '\r', '\n', ' ', '?', '\'', '\"', '.', ',', ';' };
        private List<string> _IgnoreWords = new List<string>
        {
            "a",
            "an",
            "and",
            "the"
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the IndexEngine.
        /// </summary>
        /// <param name="databaseFile">Database filename to use.  If this file does not exist, it will be created for you.</param>
        public IndexEngine(string databaseFile)
        {
            if (String.IsNullOrEmpty(databaseFile)) throw new ArgumentNullException(databaseFile);

            _Token = _TokenSource.Token;
            _DatabaseFilename = databaseFile;
            _Database = new DatabaseClient(databaseFile);
            _CurrentThreads = 0;
             
            CreateDocumentTable();
            CreateIndexEntriesTable();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Tear down IndexEngine and dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Add a document to the index.
        /// </summary>
        /// <param name="document">Document.</param>
        public void Add(Document document)
        {
            Add(document, null);
        }

        /// <summary>
        /// Add a document to the index with tags.
        /// </summary>
        /// <param name="document">Document.</param>
        /// <param name="tags">Tags.</param>
        public void Add(Document document, List<string> tags)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            lock (_Lock)
            {
                _DocumentsIndexing.Add(document.GUID); 
            }

            Task.Run(() => AddDocumentToIndex(document, tags), _Token);
        }

        /// <summary>
        /// Add a document to the index asynchronously.
        /// </summary>
        /// <param name="document">Document.</param>
        /// <returns>Task.</returns>
        public async Task AddAsync(Document document)
        {
            await AddAsync(document, null);
        }

        /// <summary>
        /// Add a document to the index with tags, asynchronously.
        /// </summary>
        /// <param name="document">Document.</param>
        /// <param name="tags">Tags.</param>
        /// <returns>Task.</returns>
        public async Task AddAsync(Document document, List<string> tags)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            lock (_Lock)
            {
                _DocumentsIndexing.Add(document.GUID);
            }

            await Task.Run(() => AddDocumentToIndex(document, tags), _Token); 
        }

        /// <summary>
        /// Search the index.
        /// </summary>
        /// <param name="terms">Search terms.</param>
        /// <returns>List of documents.</returns>
        public List<Document> Search(List<string> terms)
        {
            return Search(terms, null, null, null);
        }

        /// <summary>
        /// Search the index.
        /// </summary>
        /// <param name="terms">Search terms.</param>
        /// <param name="indexStart">Index of results from which to begin returning records.</param>
        /// <param name="maxResults">Maximum number of records to return.</param>
        /// <returns>List of documents.</returns>
        public List<Document> Search(List<string> terms, int? indexStart, int? maxResults)
        {
            return Search(terms, indexStart, maxResults, null);
        }

        /// <summary>
        /// Search the index.
        /// </summary>
        /// <param name="terms">Search terms.</param>       
        /// <param name="indexStart">Index of results from which to begin returning records.</param>
        /// <param name="maxResults">Maximum number of records to return.</param>
        /// <param name="filter">Database filters.</param> 
        /// <returns>List of documents.</returns>
        public List<Document> Search(List<string> terms, int? indexStart, int? maxResults, Expression filter)
        {
            if (terms == null || terms.Count < 1) throw new ArgumentNullException(nameof(terms));

            #region Retrieve-Document-GUIDs

            List<string> guids = GetDocumentGuidsByTerms(terms, indexStart, maxResults, filter);
            if (guids == null || guids.Count < 1)
            {
                Log("no document GUIDs found for the supplied terms");
                return new List<Document>();
            }

            #endregion

            #region Retrieve-and-Return

            Expression e = new Expression("guid", Operators.In, guids);
            DataTable result = _Database.Select("docs", indexStart, maxResults, null, e, null);
             
            List<Document> ret = new List<Document>();
            if (result != null && result.Rows.Count > 0) ret = Document.FromDataTable(result);
            Log("returning " + ret.Count + " documents for search query");
            return ret;

            #endregion
        }

        /// <summary>
        /// Get document GUIDs that contain supplied terms.
        /// </summary>
        /// <param name="terms">List of terms.</param> 
        /// <returns>List of document GUIDs.</returns>
        public List<string> GetDocumentGuidsByTerms(List<string> terms)
        {
            return GetDocumentGuidsByTerms(terms, null, null, null);
        }

        /// <summary>
        /// Get document GUIDs that contain supplied terms.
        /// </summary>
        /// <param name="terms">List of terms.</param>
        /// <param name="indexStart">Index of results from which to begin returning records.</param>
        /// <param name="maxResults">Maximum number of records to return.</param>
        /// <param name="filter">Database filters.</param>
        /// <returns>List of document GUIDs.</returns>
        public List<string> GetDocumentGuidsByTerms(List<string> terms, int? indexStart, int? maxResults, Expression filter)
        { 
            if (terms == null || terms.Count < 1) throw new ArgumentNullException(nameof(terms));

            List<string> returnFields = new List<string>
            {
                "docs_guid"
            };

            List<string> ret = new List<string>();

            Expression e = new Expression("term", Operators.In, terms);
            if (filter != null) e.PrependAnd(filter);

            DataTable result = _Database.Select("index_entries", indexStart, maxResults, returnFields, e, null);
            if (result != null && result.Rows != null && result.Rows.Count > 0 && result.Columns.Contains("docs_guid"))
            {
                foreach (DataRow curr in result.Rows)
                {
                    ret.Add(curr["docs_guid"].ToString().ToLower());
                }
            }

            Log("returning " + ret.Count + " document GUIDs for terms query");
            return ret; 
        }

        /// <summary>
        /// Delete document by its GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        public void DeleteDocumentByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            Log("deleting document GUID " + guid);
            Expression eIndexEntries = new Expression("docs_guid", Operators.Equals, guid);
            Expression eDocs = new Expression("guid", Operators.Equals, guid);
            _Database.Delete("index_entries", eIndexEntries);
            _Database.Delete("docs", eDocs);
            return;
        }
        
        /// <summary>
        /// Delete document by its handle.
        /// </summary>
        /// <param name="handle">Handle.</param>
        public void DeleteDocumentByHandle(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException(nameof(handle));
            Log("deleting documents with handle " + handle);
            Document curr = GetDocumentByHandle(handle);
            if (curr == null) return;

            Expression eIndexEntries = new Expression("docs_guid", Operators.Equals, curr.GUID);
            Expression eDocs = new Expression("guid", Operators.Equals, curr.GUID);
            _Database.Delete("index_entries", eIndexEntries);
            _Database.Delete("docs", eDocs);
            return;
        }

        /// <summary>
        /// Get a document by its GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <returns>Document.</returns>
        public Document GetDocumentByGuid(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            Expression e = new Expression("guid", Operators.Equals, guid);
            DataTable result = _Database.Select("docs", null, null, null, e, null);
            if (result == null) 
            {
                Log("document with GUID " + guid + " not found");
                return null;
            }

            List<Document> retList = Document.FromDataTable(result);
            if (retList == null || retList.Count < 1)
            {
                return null;
            }
            else
            {
                Log("returning document with GUID " + retList[0].GUID);
                return retList[0]; 
            }
        }
        
        /// <summary>
        /// Get a document by its handle.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <returns>Document.</returns>
        public Document GetDocumentByHandle(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException(nameof(handle));
            Expression e = new Expression("handle", Operators.Equals, handle);
            DataTable result = _Database.Select("docs", null, null, null, e, null);
            if (result == null || result.Rows.Count < 1)
            {
                Log("document with handle " + handle + " not found");
                return null;
            }

            List<Document> ret = Document.FromDataTable(result);
            if (ret == null || ret.Count < 1)
            {
                return null;
            }
            else
            {
                Log("returning document with handle " + ret[0].Handle);
                return ret[0];
            }
        }

        /// <summary>
        /// Check if a document has been indexed by its handle.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <returns>True if the document exists in the index.</returns>
        public bool IsHandleIndexed(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException(nameof(handle));
            Expression e = new Expression("handle", Operators.Equals, handle);
            DataTable result = _Database.Select("docs", null, null, null, e, null);
            if (result == null || result.Rows.Count < 1) return false;
            return true;
        }

        /// <summary>
        /// Check if a document has been indexed by its GUID.
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>True if the document exists in the index.</returns>
        public bool IsGuidIndexed(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            Expression e = new Expression("guid", Operators.Equals, guid);
            DataTable result = _Database.Select("docs", null, null, null, e, null);
            if (result == null || result.Rows.Count < 1) return false;
            return true;
        }

        /// <summary>
        /// Get the number of references for a given term.
        /// </summary>
        /// <param name="term">Term.</param>
        /// <returns>Reference count.</returns>
        public long GetTermReferenceCount(string term)
        {
            if (String.IsNullOrEmpty(term)) throw new ArgumentNullException(nameof(term));
            string query = "SELECT COUNT(*) AS num_entries FROM index_entries WHERE term = '" + _Database.SanitizeString(term.ToLower()) + "'";
            DataTable result = _Database.Query(query);
            if (result != null && result.Rows != null && result.Rows.Count > 0 && result.Columns.Contains("num_entries"))
            {
                return Convert.ToInt64(result.Rows[0]["num_entries"]);
            }
            else
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Backup the database to the specified filename.
        /// </summary>
        /// <param name="destination">Destination filename.</param>
        public void Backup(string destination)
        {
            if (String.IsNullOrEmpty(destination)) throw new ArgumentNullException(nameof(destination));
            _Database.Backup(destination);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Tear down IndexEngine and dispose of resources.
        /// </summary>
        /// <param name="disposing">True if disposing of resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Log("disposing");

                _TokenSource.Cancel();

                lock (_Lock)
                {
                    _DocumentsIndexing = null;
                }
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task AddDocumentToIndex(Document doc, List<string> tags)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            string header = "[" + doc.GUID + "] ";
            Log(header + "beginning processing");
             
            DateTime startTime = DateTime.Now;
            DateTime endTime = DateTime.Now;
            int termsRecorded = 0;
            int termsTotal = 0; 
             
            try
            {
                #region Setup

                _CurrentThreads++;
                Dictionary<string, int> terms = new Dictionary<string, int>();

                #endregion

                #region Process-Tags

                if (tags != null && tags.Count > 0)
                {
                    Log(header + "processing tags");

                    foreach (string curr in tags)
                    {
                        if (String.IsNullOrEmpty(curr)) continue;
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

                Log(header + "finished processing tags");

                #endregion

                #region Process-Content

                Log(header + "processing content");

                string content = Encoding.UTF8.GetString(doc.Data);
                
                string[] termsRaw = content.Split(_TermDelimiters, StringSplitOptions.RemoveEmptyEntries);
                List<string> termsAlphaOnly = new List<string>();

                if (termsRaw != null && termsRaw.Length > 0)
                {
                    foreach (string curr in termsRaw)
                    {
                        if (String.IsNullOrEmpty(curr)) continue;
                        if (curr.Length < _TermMinimumLength) continue;
                        if (_IgnoreWords.Contains(curr.ToLower())) continue;

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

                    Log(header + "extracted terms");
                }
                else
                {
                    Log(header + "no terms found");
                }
                 
                #endregion

                #region Remove-Existing-Entries

                DeleteDocumentByGuid(doc.GUID);
                Log(header + "deleting existing documents with GUID " + doc.GUID);

                DeleteDocumentByHandle(doc.Handle);
                Log(header + "deleting existing documents with handle " + doc.Handle);

                #endregion

                #region Create-New-Document-Entry

                _Database.Insert("docs", doc.ToInsertDictionary());
                Log(header + "created document database entry");

                #endregion

                #region Create-New-Terms-Entries

                termsTotal = terms.Count;
                Log(header + "detected " + termsTotal + " terms in document");

                string query = "";
                string guid = _Database.SanitizeString(doc.GUID);
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
                            if (String.IsNullOrEmpty(currKvp.Key)) continue;

                            if (termsAdded == 0)
                            {
                                query +=
                                    "('" + _Database.SanitizeString(currKvp.Key).ToLower() + "'," +
                                    currKvp.Value + "," +
                                    "'" + guid + "')";
                            }
                            else
                            {
                                query +=
                                    ",('" + _Database.SanitizeString(currKvp.Key).ToLower() + "'," +
                                    currKvp.Value + "," +
                                    "'" + guid + "')";
                            }

                            termsAdded++;
                        }

                        _Database.Query(query);

                        Log(header + "recorded " + termsRecorded + "/" + termsTotal + " terms");
                        tempDict = new Dictionary<string, int>();

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

                if (tempDict != null && tempDict.Count > 0)
                {
                    #region Drain-Remaining

                    query =
                        "INSERT INTO index_entries (term, refcount, docs_guid) VALUES ";

                    int termsAdded = 0;
                    foreach (KeyValuePair<string, int> currKvp in tempDict)
                    {
                        if (String.IsNullOrEmpty(currKvp.Key)) continue;

                        if (termsAdded == 0)
                        {
                            query +=
                                "('" + _Database.SanitizeString(currKvp.Key).ToLower() + "'," +
                                currKvp.Value + "," +
                                "'" + guid + "')";
                        }
                        else
                        {
                            query +=
                                ",('" + _Database.SanitizeString(currKvp.Key).ToLower() + "'," +
                                currKvp.Value + "," +
                                "'" + guid + "')";
                        }

                        termsAdded++;
                    }

                    _Database.Query(query);
                    Log(header + "recorded " + termsRecorded + "/" + termsTotal + " terms");
                    tempDict = new Dictionary<string, int>();

                    #endregion
                }

                #endregion

                return;
            }
            catch (OperationCanceledException)
            {
                Log(header + "cancellation requested");
            }
            catch (Exception e)
            { 
                Log(header + "exception encountered: " + Environment.NewLine + e.ToString());
                throw;
            }
            finally
            {
                _CurrentThreads--;

                lock (_Lock)
                { 
                    if (_DocumentsIndexing != null 
                        && _DocumentsIndexing.Count > 0
                        && _DocumentsIndexing.Contains(doc.GUID))
                    {
                        _DocumentsIndexing.Remove(doc.GUID);
                    }
                }

                endTime = DateTime.Now;
                TimeSpan ts = (endTime - startTime);

                decimal msTotal = Convert.ToDecimal(ts.TotalMilliseconds.ToString("F"));
                decimal msPerTerm = 0;
                if (termsRecorded > 0) msPerTerm = Convert.ToDecimal((msTotal / termsRecorded).ToString("F"));

                Log(header + "finished; " + termsRecorded + "/" + termsTotal + " terms [" + msTotal + "ms total, " + msPerTerm + "ms/term]");
            }
        }

        private void CreateDocumentTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS docs " +
                "(" +
                "  id            INTEGER PRIMARY KEY AUTOINCREMENT, " + 
                "  title         VARCHAR(256)   COLLATE NOCASE, " +
                "  description   VARCHAR(1024)  COLLATE NOCASE, " +
                "  handle        VARCHAR(256)   COLLATE NOCASE, " +
                "  source        VARCHAR(32)    COLLATE NOCASE, " +
                "  added_by      VARCHAR(32)    COLLATE NOCASE, " +
                "  guid          VARCHAR(64)    COLLATE NOCASE, " +
                "  added         VARCHAR(32) " + 
                ")";

            _Database.Query(query);
            
            query = "PRAGMA journal_mode = TRUNCATE";
            _Database.Query(query); 
        }

        private void CreateIndexEntriesTable()
        {
            string query =
                "CREATE TABLE IF NOT EXISTS index_entries " +
                "(" +
                "  id           INTEGER PRIMARY KEY AUTOINCREMENT, " +
                "  term         VARCHAR(256) COLLATE NOCASE, " +
                "  refcount     INTEGER, " +
                "  docs_guid    VARCHAR(64) COLLATE NOCASE" +
                ")";

            _Database.Query(query);
            query = "PRAGMA journal_mode = TRUNCATE";
            _Database.Query(query);
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

        private void Log(string msg)
        {
            Logger?.Invoke(_Header + msg);
        }
         
        #endregion
    }
}
