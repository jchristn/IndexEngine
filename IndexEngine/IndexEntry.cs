using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watson.ORM.Core;
using Newtonsoft.Json;

namespace Indexer
{
    /// <summary>
    /// An index entry mapping a document to a term.
    /// </summary>
    [Table("idxentries")]
    public class IndexEntry
    { 
        #region Public-Members

        /// <summary>
        /// Integer uniquely representing the index entry; typically used if stored in a database.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        [JsonIgnore]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Document term.  Supplied by the caller.
        /// </summary>
        [Column("term", false, DataTypes.Nvarchar, 256, true)]
        public string Term { get; set; } = null;

        /// <summary>
        /// Reference count for the term within the document.
        /// </summary>
        [Column("ref_count", false, DataTypes.Int, true)]
        public int ReferenceCount { get; set; } = 0;

        /// <summary>
        /// Document GUID.  Supplied by the caller.
        /// </summary>
        [Column("doc_guid", false, DataTypes.Nvarchar, 64, true)]
        public string DocumentGuid { get; set; } = null;
         
        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Creates an empty IndexEntry object.  Creation of the object structure is relegated to the application using the object.
        /// </summary>
        public IndexEntry()
        {

        }

        /// <summary>
        /// Create a populated IndexEntry object. 
        /// </summary>
        /// <param name="docGuid">Document GUID.  Supplied by the caller.</param>
        /// <param name="term">Document term.  Supplied by the caller.</param>
        /// <param name="count">Reference count for the term within the document.</param>
        public IndexEntry(string docGuid, string term, int count)
        {
            if (String.IsNullOrEmpty(docGuid)) throw new ArgumentNullException(nameof(docGuid));
            if (String.IsNullOrEmpty(term)) throw new ArgumentNullException(nameof(term));
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));

            DocumentGuid = docGuid;
            Term = term;
            ReferenceCount = count;
        } 

        #endregion

        #region Public-Methods
          
        #endregion

        #region Private-Methods
         
        #endregion
    }
}
