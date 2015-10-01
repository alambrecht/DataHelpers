using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace DataHelpers
{
    public static class Helper
    {
        /// <summary>
        /// Convert a list of items to a DataTable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            var props = TypeDescriptor.GetProperties(typeof(T));
            using (var table = new DataTable())
            {
                for (var i = 0; i < props.Count; i++)
                {
                    var prop = props[i];
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }

                var values = new object[props.Count];

                foreach (var item in data)
                {
                    for (var i = 0; i < values.Length; i++)
                    {
                        values[i] = props[i].GetValue(item);
                    }
                    table.Rows.Add(values);
                }
                return table;
            }
        }

        /// <summary>
        /// Convert a list of items to CSV
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <returns></returns>
        public static string ToCsv<T>(this IEnumerable<T> items) where T : class
        {
            var csvBuilder = new StringBuilder();
            var properties = typeof(T).GetProperties();
            foreach (var line in items.Select(item => string.Join(",", properties.Select(p => ToCsvValue(p.GetValue(item, null))).ToArray())))
            {
                csvBuilder.AppendLine(line);
            }
            return csvBuilder.ToString();
        }

        private static string ToCsvValue<T>(T item)
        {
            if (item == null) return "\"\"";

            if (item is string)
            {
                return string.Format("\"{0}\"", item.ToString().Replace("\"", "\\\""));
            }
            double dummy;
            return string.Format(double.TryParse(item.ToString(), out dummy) ? "{0}" : "\"{0}\"", item);
        }

        /// <summary>
        /// Serialize an object to binary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string SerializeToBinary<T>(this T data)
        {
            var serializer = new BinaryFormatter();
            var memorystream = new MemoryStream();
            serializer.Serialize(memorystream, data);
            return Convert.ToBase64String(memorystream.ToArray());
        }

        /// <summary>
        /// Deserialize an object from binary
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stringData"></param>
        /// <returns></returns>
        public static T DeSerializeFromBinary<T>(this string stringData)
        {
            var serializer = new BinaryFormatter();
            var m = new MemoryStream(Convert.FromBase64String(stringData));
            return (T)serializer.Deserialize(m);
        }

        /// <summary>
        /// Serialize an object to XML
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string SerializeToXML<T>(this T obj)
        {
            var ser = new XmlSerializer(obj.GetType(), "");
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            using (var writer = new StringWriter())
            {
                ser.Serialize(new XmlTextWriterFull(writer), obj, ns);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Deserialize an object from XML
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xml"></param>
        /// <returns></returns>
        public static T DeserializeFromXML<T>(this string xml)
        {
            var ser = new XmlSerializer(typeof(T));
            object obj;
            using (var stringReader = new StringReader(xml))
            {
                using (XmlReader xmlReader = XmlReader.Create(stringReader))
                {
                    obj = ser.Deserialize(xmlReader);
                }
            }
            return (T)obj;
        }

        /// <summary>
        /// SQL Bulk copy, if using Entity pass in (SqlConnection)ctx.database.connection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="data"></param>
        public static void SqlBulkCopy<T>(this SqlConnection connection, IList<T> data)
        {
            connection.SqlBulkCopy(typeof(T).Name, data);
        }

        /// <summary>
        /// SQL Bulk copy, if using Entity pass in (SqlConnection)ctx.database.connection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="tableName">Database table name</param>
        /// <param name="data"></param>
        public static void SqlBulkCopy<T>(this SqlConnection connection, string tableName, IList<T> data)
        {
            var myData = data.ToDataTable();
            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.BulkCopyTimeout = 240;
                bulkCopy.DestinationTableName = "[" + tableName + "]";
                connection.Open();
                try
                {
                    foreach (DataColumn col in myData.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }
                    bulkCopy.WriteToServer(myData);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        /// <summary>
        /// Shuffle a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="rnd"></param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rnd)
        {
            var elements = source.ToArray();
            for (var i = 0; i < elements.Length; i++)
            {
                var swapIndex = i + rnd.Next(elements.Length - i);
                var tmp = elements[i];
                yield return elements[swapIndex];
                elements[swapIndex] = tmp;
            }
        }

        /// <summary>
        /// Traverse a collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="childSelector"></param>
        /// <returns>Collection with parent</returns>
        public static IEnumerable<T> Traverse<T>(this IEnumerable<T> items, Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }

        /// <summary>
        /// Split a collection into smaller collections
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="groupBy"></param>
        /// <returns></returns>
        public static List<List<T>> Split<T>(this List<T> source, int groupBy)
        {
            var count = source.Count();
            return source.Select((x, i) => new { value = x, index = i }).GroupBy(x => x.index / (int)Math.Ceiling(count / (double)groupBy)).Select(x => x.Select(z => z.value).ToList()).ToList();
        }

        /// <summary>
        /// Clone an object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }

    public class XmlTextWriterFull : XmlTextWriter
    {
        public XmlTextWriterFull(TextWriter sink)
            : base(sink)
        {
        }

        public override void WriteEndElement()
        {
            WriteFullEndElement();
        }
    }

    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }
    /// <summary>
    /// Provides logical sorting for linq
    /// </summary>
    public sealed class OrderComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a, b);
        }
    }
}
