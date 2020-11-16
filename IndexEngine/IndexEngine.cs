using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Watson.ORM.Sqlite;
using Watson.ORM.Core;

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

        private string _DbFilename = null;
        private DatabaseSettings _DbSettings = null;
        private WatsonORM _ORM = null;

        private int _MaxThreads = 32;
        private int _CurrentThreads = 0;
        private readonly object _Lock = new object();
        private List<string> _DocumentsIndexing = new List<string>();
        private int _TermMinimumLength = 3;

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
            _DbFilename = databaseFile;
            _DbSettings = new DatabaseSettings(_DbFilename);
            _ORM = new WatsonORM(_DbSettings);
            _CurrentThreads = 0;

            _ORM.InitializeDatabase();
            _ORM.InitializeTable(typeof(Document));
            _ORM.InitializeTable(typeof(IndexEntry));

            _ORM.Query("PRAGMA journal_mode = TRUNCATE");
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
        public List<Document> Search(List<string> terms, int? indexStart, int? maxResults, DbExpression filter)
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

            DbExpression e = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.GUID)), DbOperators.In, guids);
            List<Document> ret = _ORM.SelectMany<Document>(indexStart, maxResults, e);
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
        public List<string> GetDocumentGuidsByTerms(List<string> terms, int? indexStart, int? maxResults, DbExpression filter)
        {
            if (terms == null || terms.Count < 1) throw new ArgumentNullException(nameof(terms));

            List<string> ret = new List<string>();
            DbExpression e = new DbExpression(_ORM.GetColumnName<IndexEntry>(nameof(IndexEntry.Term)), DbOperators.In, terms);
            if (filter != null) e.PrependAnd(filter);

            List<IndexEntry> entries = _ORM.SelectMany<IndexEntry>(indexStart, maxResults, e);
            if (entries != null && entries.Count > 0)
            {
                ret = entries.Select(entry => entry.DocumentGuid).ToList();
                ret = ret.Distinct().ToList();
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
            DbExpression eIndexEntries = new DbExpression(_ORM.GetColumnName<IndexEntry>(nameof(IndexEntry.DocumentGuid)), DbOperators.Equals, guid);
            DbExpression eDocs = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.GUID)), DbOperators.Equals, guid);
            _ORM.DeleteMany<IndexEntry>(eIndexEntries);
            _ORM.DeleteMany<Document>(eDocs);
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

            DbExpression eIndexEntries = new DbExpression(_ORM.GetColumnName<IndexEntry>(nameof(IndexEntry.DocumentGuid)), DbOperators.Equals, curr.GUID);
            DbExpression eDocs = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.GUID)), DbOperators.Equals, curr.GUID);
            _ORM.DeleteMany<IndexEntry>(eIndexEntries);
            _ORM.DeleteMany<Document>(eDocs);
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
            DbExpression e = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.GUID)), DbOperators.Equals, guid);
            Document doc = _ORM.SelectFirst<Document>(e);
            if (doc == null)
            {
                Log("document with GUID " + guid + " not found");
                return null;
            }
            return doc;
        }

        /// <summary>
        /// Get a document by its handle.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <returns>Document.</returns>
        public Document GetDocumentByHandle(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException(nameof(handle));
            DbExpression e = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.Handle)), DbOperators.Equals, handle);
            Document doc = _ORM.SelectFirst<Document>(e);
            if (doc == null)
            {
                Log("document with handle " + handle + " not found");
                return null;
            }
            return doc;
        }

        /// <summary>
        /// Check if a document has been indexed by its handle.
        /// </summary>
        /// <param name="handle">Handle.</param>
        /// <returns>True if the document exists in the index.</returns>
        public bool IsHandleIndexed(string handle)
        {
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException(nameof(handle));
            DbExpression e = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.Handle)), DbOperators.Equals, handle);
            return _ORM.Exists<Document>(e);
        }

        /// <summary>
        /// Check if a document has been indexed by its GUID.
        /// </summary>
        /// <param name="guid">GUID</param>
        /// <returns>True if the document exists in the index.</returns>
        public bool IsGuidIndexed(string guid)
        {
            if (String.IsNullOrEmpty(guid)) throw new ArgumentNullException(nameof(guid));
            DbExpression e = new DbExpression(_ORM.GetColumnName<Document>(nameof(Document.GUID)), DbOperators.Equals, guid);
            return _ORM.Exists<Document>(e);
        }

        /// <summary>
        /// Get the number of references for a given term.
        /// </summary>
        /// <param name="term">Term.</param>
        /// <returns>Reference count.</returns>
        public long GetTermReferenceCount(string term)
        {
            if (String.IsNullOrEmpty(term)) throw new ArgumentNullException(nameof(term));
            DbExpression e = new DbExpression(_ORM.GetColumnName<IndexEntry>(nameof(IndexEntry.Term)), DbOperators.Equals, term.ToLower());
            List<IndexEntry> entries = _ORM.SelectMany<IndexEntry>(e);
            if (entries != null && entries.Count > 0)
            {
                return entries.Sum(entry => entry.ReferenceCount);
            }
            else
            {
                return 0;
            }
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
                _ORM.Dispose();

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

                doc = _ORM.Insert<Document>(doc);
                Log(header + "created document entry");

                #endregion

                #region Create-New-Terms-Entries

                Log(header + "creating " + terms.Count + " index entries, please be patient");
                foreach (KeyValuePair<string, int> term in terms)
                {
                    IndexEntry entry = new IndexEntry(doc.GUID, term.Key, term.Value);
                    entry = _ORM.Insert<IndexEntry>(entry);
                    termsRecorded++;
                }

                #endregion

                return;
            }
            catch (TaskCanceledException)
            {
                Log(header + "cancellation requested");
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

                Log(header + "finished; " + termsRecorded + " terms [" + msTotal + "ms total, " + msPerTerm + "ms/term]");
            }
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

        #region Private-Static

        private static char[] _TermDelimiters = new char[]
            {
                '!',
                '\"',
                '#',
                '$',
                '%',
                '&',
                '\'',
                '(',
                ')',
                '*',
                '+',
                ',',
                '-',
                '.',
                '/',
                ':',
                ';',
                '<',
                '=',
                '>',
                '?',
                '@',
                '[',
                '\\',
                ']',
                '^',
                '_',
                '`',
                '{',
                '|',
                '}',
                '~',
                ' ',
                '\'',
                '\"',
                '\u001a',
                '\r',
                '\n',
                '\t'
            };

        private static List<string> _IgnoreWords = new List<string>
        {
            "a",
            "about",
            "above",
            "after",
            "again",
            "against",
            "aint",
            "ain't",
            "all",
            "also",
            "am",
            "an",
            "and",
            "any",
            "are",
            "arent",
            "aren't",
            "as",
            "at",
            "be",
            "because",
            "been",
            "before",
            "being",
            "below",
            "between",
            "both",
            "but",
            "by",
            "cant",
            "can't",
            "cannot",
            "could",
            "couldnt",
            "couldn't",
            "did",
            "didnt",
            "didn't",
            "do",
            "does",
            "doesnt",
            "doesn't",
            "doing",
            "dont",
            "don't",
            "down",
            "during",
            "each",
            "few",
            "for",
            "from",
            "further",
            "had",
            "hadnt",
            "hadn't",
            "has",
            "hasnt",
            "hasn't",
            "have",
            "havent",
            "haven't",
            "having",
            "he",
            "hed",
            "he'd",
            "he'll",
            "hes",
            "he's",
            "her",
            "here",
            "heres",
            "here's",
            "hers",
            "herself",
            "him",
            "himself",
            "his",
            "how",
            "hows",
            "how's",
            "i",
            "id",
            "i'd",
            "i'll",
            "im",
            "i'm",
            "ive",
            "i've",
            "if",
            "in",
            "into",
            "is",
            "isnt",
            "isn't",
            "it",
            "its",
            "it's",
            "its",
            "itself",
            "lets",
            "let's",
            "me",
            "more",
            "most",
            "mustnt",
            "mustn't",
            "my",
            "myself",
            "no",
            "nor",
            "not",
            "of",
            "off",
            "on",
            "once",
            "only",
            "or",
            "other",
            "ought",
            "our",
            "ours",
            "ourselves",
            "out",
            "over",
            "own",
            "same",
            "shall",
            "shant",
            "shan't",
            "she",
            "she'd",
            "she'll",
            "shes",
            "she's",
            "should",
            "shouldnt",
            "shouldn't",
            "so",
            "some",
            "such",
            "than",
            "that",
            "thats",
            "that's",
            "the",
            "their",
            "theirs",
            "them",
            "themselves",
            "then",
            "there",
            "theres",
            "there's",
            "these",
            "they",
            "theyd",
            "they'd",
            "theyll",
            "they'll",
            "theyre",
            "they're",
            "theyve",
            "they've",
            "this",
            "those",
            "thou",
            "though",
            "through",
            "to",
            "too",
            "under",
            "until",
            "unto",
            "up",
            "very",
            "was",
            "wasnt",
            "wasn't",
            "we",
            "we'd",
            "we'll",
            "were",
            "we're",
            "weve",
            "we've",
            "werent",
            "weren't",
            "what",
            "whats",
            "what's",
            "when",
            "whens",
            "when's",
            "where",
            "wheres",
            "where's",
            "which",
            "while",
            "who",
            "whos",
            "who's",
            "whose",
            "whom",
            "why",
            "whys",
            "why's",
            "with",
            "wont",
            "won't",
            "would",
            "wouldnt",
            "wouldn't",
            "you",
            "youd",
            "you'd",
            "youll",
            "you'll",
            "youre",
            "you're",
            "youve",
            "you've",
            "your",
            "yours",
            "yourself",
            "yourselves"
        };

        #endregion
    }
}
