using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indexer
{
    /// <summary>
    /// A document that has been indexed by the index engine.
    /// </summary>
    public class Document
    {
        #region Constructor

        /// <summary>
        /// Creates an empty Document object.  Creation of the object structure is relegated to the application using the object.
        /// </summary>
        public Document()
        {

        }

        /// <summary>
        /// Creates a populated Document object.  
        /// </summary>
        /// <param name="guid">Globally unique identifier for the document.  If one is not supplied, IndexEngine will supply one.</param>
        /// <param name="title">Non-nullable title of the document.</param>
        /// <param name="description">Description of the document.</param>
        /// <param name="handle">Non-nullable URL or other handle to access the document on persistent storage (managed by the caller).</param>
        /// <param name="source">Source of the document (managed by the caller)></param>
        /// <param name="addedBy">Name of the user adding the document (managed by the caller).</param>
        /// <param name="data">Byte array data from the source document.</param>
        public Document(
            string guid,
            string title,
            string description,
            string handle,
            string source,
            string addedBy,
            byte[] data)
        {
            if (String.IsNullOrEmpty(title)) throw new ArgumentNullException("title");
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException("handle");

            Title = title;
            Description = description;
            Handle = handle;
            Source = source;
            AddedBy = addedBy;
            if (!String.IsNullOrEmpty(guid)) GUID = guid;
            else GUID = Guid.NewGuid().ToString();
            Added = DateTime.Now.ToUniversalTime();
            Data = data;
        }

        #endregion

        #region Public-Class-Members

        /// <summary>
        /// Integer uniquely representing the document; typically used if stored in a database.
        /// </summary>
        public int DocumentID { get; set; }

        /// <summary>
        /// Title of the document.  Supplied by the caller.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Free-form text description of the document.  Supplied by the caller.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// URL or other handle to use when attempting to reach the content.  Supplied by the caller.
        /// </summary>
        public string Handle { get; set; }

        /// <summary>
        /// Source of the content, i.e. YouTube, Vimeo, web, etc.  Supplied by the caller.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Free-form text describing who added the document.  Supplied by the caller.
        /// </summary>
        public string AddedBy { get; set; }

        /// <summary>
        /// GUID for this document.  Assigned by the index engine.
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// UTC timestamp when the document was added.  Assigned by the index engine.
        /// </summary>
        public DateTime Added { get; set; }

        /// <summary>
        /// Document contents in byte array form.  
        /// </summary>
        public byte[] Data { get; set; }

        #endregion

        #region Private-Class-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Create a human-readable string version of the object.
        /// </summary>
        /// <returns>String.</returns>
        public override string ToString()
        {
            string ret =
                "---" + Environment.NewLine +
                "  ID          : " + DocumentID + Environment.NewLine +
                "  Title       : " + Title + Environment.NewLine +
                "  Description : " + Description + Environment.NewLine +
                "  Source      : " + Source + Environment.NewLine +
                "  Handle      : " + Handle + Environment.NewLine +
                "  Added By    : " + AddedBy + Environment.NewLine +
                "  Added       : " + Added + Environment.NewLine +
                "  GUID        : " + GUID;
            return ret;
        }

        internal Dictionary<string, object> ToInsertDictionary()
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            ret.Add("title", Title);
            ret.Add("description", Description);
            ret.Add("handle", Handle);
            ret.Add("source", Source);
            ret.Add("added_by", AddedBy);
            ret.Add("guid", GUID);
            ret.Add("added", Added);
            return ret;
        }

        #endregion

        #region Private-Methods

        internal static Document FromDataRow(DataRow row)
        {
            if (row == null) throw new ArgumentNullException("row");
            Document doc = new Document();
            doc.DocumentID = Convert.ToInt32(row["id"]);
            doc.Title = row["title"].ToString();
            doc.Description = row["description"].ToString();
            doc.Handle = row["handle"].ToString();
            doc.Source = row["source"].ToString();
            doc.AddedBy = row["added_by"].ToString();
            doc.GUID = row["guid"].ToString();
            doc.Added = Convert.ToDateTime(row["added"]);
            return doc;
        }

        internal static List<Document> FromDataTable(DataTable table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (table.Rows == null || table.Rows.Count < 1) return new List<Document>();

            List<Document> ret = new List<Document>();
            foreach (DataRow curr in table.Rows)
            {
                Document doc = Document.FromDataRow(curr);
                if (doc != null) ret.Add(doc);
            }

            return ret;
        }

        #endregion
    }
}
