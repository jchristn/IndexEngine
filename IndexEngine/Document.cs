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
    /// A document that has been indexed by the index engine.
    /// </summary>
    [Table("docs")]
    public class Document
    { 
        #region Public-Members

        /// <summary>
        /// Integer uniquely representing the document; typically used if stored in a database.
        /// </summary>
        [Column("id", true, DataTypes.Int, false)]
        [JsonIgnore]
        public int Id { get; set; } = 0;

        /// <summary>
        /// Title of the document.  Supplied by the caller.
        /// </summary>
        [Column("title", false, DataTypes.Nvarchar, 256, true)]
        public string Title { get; set; } = null;

        /// <summary>
        /// Free-form text description of the document.  Supplied by the caller.
        /// </summary>
        [Column("description", false, DataTypes.Nvarchar, 1024, true)]
        public string Description { get; set; } = null;

        /// <summary>
        /// URL or other handle to use when attempting to reach the content.  Supplied by the caller.
        /// </summary>
        [Column("handle", false, DataTypes.Nvarchar, 256, true)]
        public string Handle { get; set; } = null;

        /// <summary>
        /// Source of the content, i.e. YouTube, Vimeo, web, etc.  Supplied by the caller.
        /// </summary>
        [Column("source", false, DataTypes.Nvarchar, 32, true)]
        public string Source { get; set; } = null;

        /// <summary>
        /// Free-form text describing who added the document.  Supplied by the caller.
        /// </summary>
        [Column("added_by", false, DataTypes.Nvarchar, 32, true)]
        public string AddedBy { get; set; } = null;

        /// <summary>
        /// GUID for this document.  Assigned by the index engine.
        /// </summary>
        [Column("guid", false, DataTypes.Nvarchar, 64, false)]
        public string GUID { get; set; } = null;

        /// <summary>
        /// UTC timestamp when the document was added.  Assigned by the index engine.
        /// </summary>
        [Column("added", false, DataTypes.DateTime, false)]
        public DateTime Added { get; set; } = DateTime.Now.ToUniversalTime();

        /// <summary>
        /// Document contents in byte array form.  
        /// </summary>
        public byte[] Data { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

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
            if (String.IsNullOrEmpty(title)) throw new ArgumentNullException(nameof(title));
            if (String.IsNullOrEmpty(handle)) throw new ArgumentNullException(nameof(handle));

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

        #region Public-Methods
          
        /// <summary>
        /// Retrieve a JSON string of the object.
        /// </summary>
        /// <returns>JSON string.</returns>
        public string ToJson()
        {
            return Common.SerializeJson(this, true);
        }

        #endregion

        #region Private-Methods
         
        #endregion
    }
}
