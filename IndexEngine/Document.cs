using System;
using System.Collections.Generic;
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
        /// Creates a populated Document object.  GUID and timestamp are added automatically.
        /// </summary>
        /// <param name="title">Non-nullable title of the document.</param>
        /// <param name="description">Description of the document.</param>
        /// <param name="handle">Non-nullable URL or other handle to access the document on persistent storage (managed by the caller).</param>
        /// <param name="source">Source of the document (managed by the caller)></param>
        /// <param name="addedBy">Name of the user adding the document (managed by the caller).</param>
        /// <param name="data">Byte array data from the source document.</param>
        public Document(
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
            ReferenceGuid = Guid.NewGuid().ToString();
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
        public string ReferenceGuid { get; set; } 

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
                "  GUID        : " + ReferenceGuid;
            return ret;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
