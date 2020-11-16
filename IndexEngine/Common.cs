using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection; 
using System.Security.Cryptography; 
using System.Text; 
using Newtonsoft.Json;

namespace Indexer
{
    /// <summary>
    /// Commonly-used methods.
    /// </summary>
    public static class Common
    {
        #region Input

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static bool InputBoolean(string question, bool yesDefault)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Console.Write(question);

            if (yesDefault) Console.Write(" [Y/n]? ");
            else Console.Write(" [y/N]? ");

            string userInput = Console.ReadLine();

            if (String.IsNullOrEmpty(userInput))
            {
                if (yesDefault) return true;
                return false;
            }

            userInput = userInput.ToLower();

            if (yesDefault)
            {
                if (
                    (String.Compare(userInput, "n") == 0)
                    || (String.Compare(userInput, "no") == 0)
                   )
                {
                    return false;
                }

                return true;
            }
            else
            {
                if (
                    (String.Compare(userInput, "y") == 0)
                    || (String.Compare(userInput, "yes") == 0)
                   )
                {
                    return true;
                }

                return false;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string InputString(string question, string defaultAnswer, bool allowNull)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            while (true)
            {
                Console.Write(question);

                if (!String.IsNullOrEmpty(defaultAnswer))
                {
                    Console.Write(" [" + defaultAnswer + "]");
                }

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (!String.IsNullOrEmpty(defaultAnswer)) return defaultAnswer;
                    if (allowNull) return null;
                    else continue;
                }

                return userInput;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static int InputInteger(string question, int defaultAnswer, bool positiveOnly, bool allowZero)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            while (true)
            {
                Console.Write(question);
                Console.Write(" [" + defaultAnswer + "] ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    return defaultAnswer;
                }

                int ret = 0;
                if (!Int32.TryParse(userInput, out ret))
                {
                    Console.WriteLine("Please enter a valid integer.");
                    continue;
                }

                if (ret == 0)
                {
                    if (allowZero)
                    {
                        return 0;
                    }
                }

                if (ret < 0)
                {
                    if (positiveOnly)
                    {
                        Console.WriteLine("Please enter a value greater than zero.");
                        continue;
                    }
                }

                return ret;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static List<string> InputStringList(string question, bool allowEmpty)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            List<string> ret = new List<string>();

            while (true)
            {
                Console.Write(question);

                Console.Write(" ");

                string userInput = Console.ReadLine();

                if (String.IsNullOrEmpty(userInput))
                {
                    if (ret.Count < 1 && !allowEmpty) continue;
                    return ret;
                }

                ret.Add(userInput);
            }
        }

        #endregion

        #region Serialization

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string SerializeJson(object obj, bool pretty)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (obj == null) return null;
            string json;

            if (pretty)
            {
                json = JsonConvert.SerializeObject(
                  obj,
                  Newtonsoft.Json.Formatting.Indented,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                  });
            }
            else
            {
                json = JsonConvert.SerializeObject(obj,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc
                  });
            }

            return json;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static T DeserializeJson<T>(string json)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (String.IsNullOrEmpty(json)) throw new ArgumentNullException(nameof(json));
            return JsonConvert.DeserializeObject<T>(json);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static T DeserializeJson<T>(byte[] data)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (data == null || data.Length < 1) throw new ArgumentNullException(nameof(data));
            return DeserializeJson<T>(Encoding.UTF8.GetString(data));
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static T CopyObject<T>(object o)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (o == null) return default(T);
            string json = SerializeJson(o, false);
            T ret = DeserializeJson<T>(json);
            return ret;
        }

        #endregion

        #region Conversion
         
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static IList<T> DataTableToList<T>(this DataTable table, Dictionary<string, string> mappings) where T : new()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            IList<PropertyInfo> properties = typeof(T).GetProperties().ToList();
            IList<T> result = new List<T>();

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow)row, properties, mappings);
                result.Add(item);
            }

            return result;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                if (row[property.Name] is System.DBNull) continue;
                property.SetValue(item, row[property.Name], null);
            }
            return item;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties, Dictionary<string, string> mappings) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                if (mappings.ContainsKey(property.Name))
                    property.SetValue(item, row[mappings[property.Name]], null);
            }
            return item;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static List<dynamic> DataTableToListDynamic(DataTable dt)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            List<dynamic> ret = new List<dynamic>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                dynamic dyn = new ExpandoObject();
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)dyn;
                    dic[col.ColumnName] = curr[col];
                }
                ret.Add(dyn);
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static dynamic DataTableToDynamic(DataTable dt)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            dynamic ret = new ExpandoObject();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    var dic = (IDictionary<string, object>)ret;
                    dic[col.ColumnName] = curr[col];
                }

                return ret;
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static List<Dictionary<string, object>> DataTableToListDictionary(DataTable dt)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                Dictionary<string, object> currDict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    currDict.Add(col.ColumnName, curr[col]);
                }

                ret.Add(currDict);
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static Dictionary<string, object> DataTableToDictionary(DataTable dt)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();
            if (dt == null || dt.Rows.Count < 1) return ret;

            foreach (DataRow curr in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    ret.Add(col.ColumnName, curr[col]);
                }

                return ret;
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static byte[] StreamToBytes(Stream input)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (!input.CanRead) throw new InvalidOperationException("Input stream is not readable");

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;

                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static List<string> CsvToStringList(string csv)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (String.IsNullOrEmpty(csv))
            {
                return null;
            }

            List<string> ret = new List<string>();

            string[] array = csv.Split(',');

            if (array != null && array.Length > 0)
            {
                foreach (string curr in array)
                {
                    if (String.IsNullOrEmpty(curr)) continue;
                    ret.Add(curr.Trim());
                }
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string StringListToCsv(List<string> strings)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (strings == null || strings.Count < 1) return null;

            int added = 0;
            string ret = "";

            foreach (string curr in strings)
            {
                if (added == 0)
                {
                    ret += curr;
                }
                else
                {
                    ret += "," + curr;
                }

                added++;
            }

            return ret;
        }

        #endregion

        #region Misc

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string StringRemove(string original, string remove)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (String.IsNullOrEmpty(original)) return null;
            if (String.IsNullOrEmpty(remove)) return original;

            int index = original.IndexOf(remove);
            string ret = (index < 0)
                ? original
                : original.Remove(index, remove.Length);

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string Line(int count, string fill)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (count < 1) return "";

            string ret = "";
            for (int i = 0; i < count; i++)
            {
                ret += fill;
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string RandomString(int num_char)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            string ret = "";
            if (num_char < 1) return null;
            int valid = 0;
            Random random = new Random((int)DateTime.Now.Ticks);
            int num = 0;

            for (int i = 0; i < num_char; i++)
            {
                num = 0;
                valid = 0;
                while (valid == 0)
                {
                    num = random.Next(126);
                    if (((num > 47) && (num < 58)) ||
                        ((num > 64) && (num < 91)) ||
                        ((num > 96) && (num < 123)))
                    {
                        valid = 1;
                    }
                }
                ret += (char)num;
            }

            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static double TotalMsFrom(DateTime start)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                DateTime end = DateTime.Now;
                return TotalMsBetween(start, end);
            }
            catch (Exception)
            {
                return -1;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static double TotalMsBetween(DateTime start, DateTime end)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                start = start.ToUniversalTime();
                end = end.ToUniversalTime();
                TimeSpan total = end - start;
                return total.TotalMilliseconds;
            }
            catch (Exception)
            {
                return -1;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static bool IsLaterThanNow(DateTime? dt)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                DateTime curr = Convert.ToDateTime(dt);
                return Common.IsLaterThanNow(curr);
            }
            catch (Exception)
            {
                return false;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static bool IsLaterThanNow(DateTime dt)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (DateTime.Compare(dt, DateTime.Now) > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Crypto

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string Md5(byte[] data)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (data == null) return null;

            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(data);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            string ret = sb.ToString();
            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string Md5(string data)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (String.IsNullOrEmpty(data)) return null;

            MD5 md5 = MD5.Create();
            byte[] dataBytes = System.Text.Encoding.ASCII.GetBytes(data);
            byte[] hash = md5.ComputeHash(dataBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            string ret = sb.ToString();
            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string Md5(Stream stream)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            if (stream == null || !stream.CanRead) return null;

            MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(stream);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
            string ret = sb.ToString();
            return ret;
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string Md5File(string filename)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("X2"));
                    string ret = sb.ToString();
                    return ret;
                }
            }
        }

        #endregion

        #region File

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static string ReadTextFile(string filename)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                return File.ReadAllText(@filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static byte[] ReadBinaryFile(string filename, int from, int len)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                if (len < 1) return null;
                if (from < 0) return null;

                byte[] ret = new byte[len];
                using (BinaryReader reader = new BinaryReader(new FileStream(filename, System.IO.FileMode.Open)))
                {
                    reader.BaseStream.Seek(from, SeekOrigin.Begin);
                    reader.Read(ret, 0, len);
                }

                return ret;
            }
            catch (Exception)
            {
                return null;
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static byte[] ReadBinaryFile(string filename)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            try
            {
                return File.ReadAllBytes(@filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}