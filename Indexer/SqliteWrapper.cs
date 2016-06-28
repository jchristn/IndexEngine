using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Data.Sqlite;

namespace Indexer
{
    class SqliteWrapper
    {
        #region Constructor

        public SqliteWrapper(string databaseFile)
        {
            if (String.IsNullOrEmpty(databaseFile)) throw new ArgumentNullException(databaseFile);
            DatabaseFile = databaseFile;
            DatabaseConnectionString = "Data Source=" + DatabaseFile + ";Version=3;";
            CreateSuccess = CreateDatabase(DatabaseFile);
            if (!CreateSuccess) throw new Exception("Unable to create or open database file " + databaseFile);
        }

        #endregion

        #region Public-Class-Members
        
        public bool ConsoleDebug { get; set; }
        public string DatabaseFile { get; set; }
        public string DatabaseConnectionString { get; set; }

        #endregion

        #region Private-Class-Members

        private bool CreateSuccess { get; set; }

        #endregion

        #region Public-Methods

        public bool ExecuteQuery(string query, out DataTable result)
        {
            result = new DataTable();

            #region Check-for-Null-Values

            if (String.IsNullOrEmpty(DatabaseFile)) throw new ArgumentNullException("DatabaseFile");
            if (!CreateSuccess) throw new Exception("Unable to create or open database file " + DatabaseFile);
            if (String.IsNullOrEmpty(DatabaseConnectionString)) throw new ArgumentNullException("No database connection string specified or configured");
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException("query");

            #endregion
            
            #region Process

            using (SqliteConnection conn = new SqliteConnection(DatabaseConnectionString))
            {
                conn.Open();

                using (SqliteCommand cmd = new SqliteCommand(query, conn))
                {
                    using (SqliteDataReader rdr = cmd.ExecuteReader())
                    {
                        result.Load(rdr);
                        return true;
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Private-Methods

        private bool CreateDatabase(string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");
            using (SqliteConnection conn = new SqliteConnection("Data Source=" + name + ";Version=3;"))
            {
                conn.Open();
                if (!File.Exists(name))
                {
                    Log("CreateDatabase creating file " + name);
                    SqliteConnection.CreateFile(name);
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }

        public string SanitizeString(string dirty)
        {
            if (String.IsNullOrEmpty(dirty)) return null;
            string clean = "";

            // null, below ASCII range, above ASCII range
            for (int i = 0; i < dirty.Length; i++)
            {
                if (((int)(dirty[i]) == 0) ||    // null
                    ((int)(dirty[i]) < 32) ||    // below ASCII range
                    ((int)(dirty[i]) > 126)      // above ASCII range
                    )
                {
                    continue;
                }
                else
                {
                    clean += dirty[i];
                }
            }

            clean = clean.Replace("'", "''");
            clean = clean.Replace("\"", "\"\"");
            clean = clean.Replace("/*", "");
            clean = clean.Replace("*/", "");
            clean = clean.Replace("--", "");
            return clean;
        }

        public bool SanitizeString(string dirty, out string clean)
        {
            clean = null;
            
            if (String.IsNullOrEmpty(dirty)) return true;

            // null, below ASCII range, above ASCII range
            for (int i = 0; i < dirty.Length; i++)
            {
                if (((int)(dirty[i]) == 0) ||    // null
                    ((int)(dirty[i]) < 32) ||    // below ASCII range
                    ((int)(dirty[i]) > 126)      // above ASCII range
                    )
                {
                    continue;
                }
                else
                {
                    clean += dirty[i];
                }
            }

            clean = clean.Replace("'", "''");
            clean = clean.Replace("\"", "\"\"");
            clean = clean.Replace("/*", "");
            clean = clean.Replace("*/", "");
            clean = clean.Replace("--", "");
            return true;
        }

        private void Log(string message)
        {
            if (ConsoleDebug) Console.WriteLine(message);
        }
        
        #endregion
    }
}
