using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MercsCodeBaseTest
{
    public static class Utilities
    {
        /// <summary>
        /// Gets a template of an object. You can send: 
        /// - not null, initialized object
        /// - null object, with its desired type as second param
        /// </summary>
        /// <param name="objecto"></param>
        /// <param name="typo"></param>
        /// <returns>Initialized object, ready for deserialization if needed</returns>
        public static object GetTemplate(object objecto, Type typo = null)
        {
            dynamic toReturn = objecto;
            //If we dont get a type, extract from objecto. This will throw exception only if objecto AND typo is null, thus wrong usage.
            if (typo == null)
            {
                try
                {
                    typo = objecto.GetType();
                }
                catch (NullReferenceException ex)
                {
                    throw new InvalidOperationException(
                        "You need to provide a not-null value object, or a Type object");
                }
            }

            if (!TypeChecker.IsReallyPrimitive(typo))
            {
                //We start by creating a ExpandoObject to be our base.
                toReturn = new ExpandoObject();

                PropertyInfo[] fields = typo.GetProperties();

                List<object> fieldValues = new List<object>();
                List<string> fieldnames = new List<string>();
                List<Type> fieldTypes = new List<Type>();

                foreach (PropertyInfo item in fields)
                {
                    if (objecto != null)
                    {
                        //If we did sent in an objecto with values, we can pass  them if needed.
                        fieldValues.Add(item.GetValue(objecto));
                    }
                    else
                    {
                        fieldValues.Add(null);
                    }

                    fieldnames.Add(item.Name);
                    fieldTypes.Add(item.PropertyType);
                }

                using (List<object>.Enumerator e1 = fieldValues.GetEnumerator())
                using (List<string>.Enumerator e2 = fieldnames.GetEnumerator())
                using (List<Type>.Enumerator e3 = fieldTypes.GetEnumerator())
                {
                    while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
                    {
                        object fieldV = e1.Current;
                        string fieldN = e2.Current;
                        Type fieldT = e3.Current;
                        PropertyInfo propInfo = typo.GetProperty(fieldN);
                        object obj;
                        if (fieldV != null && CheckSetters(propInfo))
                        {
                            //No reason to initialize it, if it already has a value. (If output will not be desired, change this)
                            obj = fieldV;
                        }
                        else
                        {
                            obj = TryInitializing(fieldT, propInfo);
                        }

                        //Did we contruct a template for it?
                        if (obj != null)
                        {
                            AddProperty(toReturn, fieldN, obj);
                        }
                    }
                }
            }
            else
            {
                //Is a primitive, without propertiesInfo, value.
                toReturn = TryInitializing(typo, null);
            }

            return toReturn;
        }

        public static object TryInitializing(Type fieldT, PropertyInfo propInfo)
        {
            //We dont want to initialize any delegates
            if (!fieldT.IsSubclassOf(typeof(MulticastDelegate)))
            {
                // The setter exists and is public.
                if (CheckSetters(propInfo))
                {
                    if (TypeChecker.IsReallyPrimitive(fieldT) || TypeChecker.IsEnumerable(fieldT))
                    {
                        //Initialization to make sure values are serialized
                        dynamic newobj;
                        if (fieldT == typeof(string))
                        {
                            //Apparently, string doesnt have any constructor avaible from Activator instance 
                            newobj = "";
                        }
                        else if (fieldT.IsArray)
                        {
                            //If it is an array, we need to send a length
                            newobj = Activator.CreateInstance(typeof(object[]), 1);
                            newobj[0] = GetTemplate(null, fieldT.GetElementType());
                        }
                        else if (fieldT.GetInterface(nameof(IEnumerable)) != null)
                        {
                            //TODO: If dictonary shows up, this will throw an exception. No entities atm use dicts right now, so it should be safe for now. 
                            newobj = Activator.CreateInstance(typeof(List<object>));
                            newobj.Add(GetTemplate(null, fieldT.GetGenericArguments().FirstOrDefault()));
                        }
                        else
                        {
                            //If nothing above applies, just use a default one
                            newobj = Activator.CreateInstance(fieldT);
                        }

                        if (newobj == null)
                        {
                            newobj = Activator.CreateInstance(Nullable.GetUnderlyingType(fieldT));
                        }

                        return newobj;
                    }
                    else
                    {
                        //TODO: Find a way to check for looping hierarchy (for example two way Parent\Child relation) 
                        //  and check for it here!
                        return GetTemplate(null, fieldT);
                    }
                }
            }

            return null;
        }

        //Checks for setters for property field
        public static bool CheckSetters(PropertyInfo propInfo)
        {
            //If null is sent, it is not a property field
            if (propInfo == null)
            {
                return true;
            }
            else if (propInfo.CanWrite && propInfo.GetSetMethod( /*nonPublic*/ true).IsPublic)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            IDictionary<string, object> expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }
    }

    public static class TypeChecker
    {
        public static bool IsNullable(Type type)
        {
            //if (!type.IsValueType) return true; // ref-type
            if (Nullable.GetUnderlyingType(type) != null) return true; // Nullable<T>
            return false; // value-type
        }

        public static bool IsEnumerable(Type type)
        {
            if (type.GetInterface(nameof(IEnumerable)) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Is it a primitive (enough for us) type?
        /// Extracts Type from obj.
        /// </summary>
        public static bool IsReallyPrimitive(object obj)
        {
            try
            {
                Type type = obj.GetType();
                //TODO! if needed add more types that will be used as primitives
                if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
                {
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                return true;
            }
        }

        /// <summary>
        /// Is it a primitive (enough for us) type?
        /// </summary>
        public static bool IsReallyPrimitive(Type type)
        {
            if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                return true;
            }

            //Is a nullable primitive type?
            Type isNullablePrim = Nullable.GetUnderlyingType(type);
            if (isNullablePrim != null)
            {
                if (isNullablePrim.IsPrimitive)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if type is any type of integer type
        /// Extracts Type from obj.
        /// </summary>
        public static bool IsIntegerType(this object o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Check if type is any type of integer type
        /// </summary>
        public static bool IsIntegerType(this Type o)
        {
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsFloatingType(this Type o)
        {
            switch (Type.GetTypeCode(o))
            {
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDictionary(this Type o)
        {
            bool result = o.GetInterfaces()
                .Where(i => i.IsGenericType)
                .Any(i => i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            return result;
        }

        public static List<Type> FindDerivedTypes(Assembly assembly, Type baseType, string ournamespace)
        {
            var list = TypeChecker.GetTypesInNamespace(assembly, ournamespace);
            List<Type> types = new List<Type>();
            foreach (Type typeinnamespace in list)
            {
                if (baseType.IsAssignableFrom(typeinnamespace))
                {
                    types.Add(typeinnamespace);
                }
            }

            return types;
        }

        //TODO! Use this when checking for a list 
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return true;
            }

            /* If this is a list, use the Count property for efficiency. 
             * The Count property is O(1) while IEnumerable.Count() is O(N). */
            var collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                return collection.Count < 1;
            }

            return !enumerable.Any();
        }

        /*
         * Use it like this:
         Type[] typelist = GetTypesInNamespace(Assembly.GetExecutingAssembly(), "MyNamespace");
            for (int i = 0; i < typelist.Length; i++)
            {
                Console.WriteLine(typelist[i].Name);
            }
         */
        public static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return
                assembly.GetTypes()
                    .Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
                    .ToArray();
        }
    }

    //TODO! Optimize this shit, like really man this sucks
    public static class Randomizer
    {
        public static double GetRandDouble(double min, double max, int precision = 2)
        {
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                double randomvalue = min - 1;
                while (!(randomvalue > min && randomvalue < max))
                {
                    byte[] rno = new byte[17];
                    rg.GetBytes(rno);
                    randomvalue = BitConverter.ToDouble(rno, 0);
                    randomvalue = Math.Round(randomvalue, precision);
                }

                return randomvalue;
            }
        }

        public static int GetRandInt(int min, int max)
        {
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                int randomvalue = min - 1;
                int range = max - min;
                while (!(randomvalue > min && randomvalue < max))
                {
                    byte[] rno = new byte[5];
                    rg.GetBytes(rno);
                    randomvalue = (BitConverter.ToInt32(rno, 0) % range) + min;
                }

                return randomvalue;
            }
        }

        public static string GetRandString(int length)
        {
            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider())
            {
                string randomvalue = string.Empty;
                while (randomvalue.Length != length)
                {
                    int lenghtbytes = 26 + length * 2 + 1;
                    byte[] rno = new byte[lenghtbytes];
                    rg.GetBytes(rno);
                    randomvalue =
                        StringHelper.ReplaceNameable(Encoding.ASCII.GetString(rno).Replace("?", ""), string.Empty);
                }

                return randomvalue;
            }
        }

        public static bool GetRandBool()
        {
            if (GetRandInt(0, 10000) % 2 == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static object GetRandEnum(Type enumType)
        {
            var EnumCount = Enum.GetNames(enumType).Length;
            var randomnumber = GetRandInt(0, 100 * EnumCount) % EnumCount;
            var randomvalue = Enum.Parse(enumType, randomnumber.ToString());
            return randomvalue;
        }

        // Generate Perli noise map by using defined octaves and frequencys amount
        // Biggest performance hit comes from Sizes and Octaves (both O(n^2))
        public static double[,] GetPerlinNoise(int sizeX, int sizeY, int octaves, int freq)
        {
            Perlin perlin = new Perlin();
            int[] newp = new int[256];
            for (int p = 0; p < newp.Length; p++)
            {
                newp[p] = GetRandInt(0, 255);
            }

            perlin.Permutation = newp;
            //TODO: Better way to generate new permutation table

            double[,] array = new double[sizeX, sizeY];
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    array[i, j] = perlin.OctavePerlin((1 / (double) (sizeX)) * (double) (i),
                        (1 / (double) (sizeY)) * (double) (j), 0, octaves, freq);
                    //array[i, j] = perlin.perlin((1 / (double)(sizeX)) * (double)(i), (1 / (double)(sizeY)) * (double)(j),0);
                }
            }

            return array;
        }

        public static double[,,] GetPerlinNoise(int sizeX, int sizeY, int sizeZ, int octaves, int freq)
        {
            throw new NotImplementedException();
        }
    }

    public static class Serializer
    {
        //Serializing with output formatting options
        public static string Serialize(object ourobject, bool pretty = false, bool full = false)
        {
            JsonSerializerSettings setting;
            if (full)
            {
                setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include,
                    DefaultValueHandling = DefaultValueHandling.Include,
                };
            }
            else
            {
                setting = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                };
            }


            string serialized = string.Empty;

            if (pretty)
            {
                serialized = JsonConvert.SerializeObject(
                    ourobject,
                    Newtonsoft.Json.Formatting.Indented,
                    setting);
            }
            else
            {
                serialized = JsonConvert.SerializeObject(
                    ourobject,
                    Newtonsoft.Json.Formatting.None,
                    setting);
            }

            return serialized;
        }

        public static object Deserialize(string ourobject)
        {
            object serialized;

            serialized = JsonConvert.DeserializeObject(ourobject);

            return serialized;
        }
    }

    public static class Logger
    {
        public enum ELogflag
        {
            Info,
            Warning,
            Critical,
            Custom
        }

        public static class Log
        {
            private static List<String> logdata = new List<string>();

            public static void LogMessage(string engineName, string msg, ELogflag flag, string title = "")
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("[" + DateTime.Now + "]");
                sb.Append("[" + engineName + "]");
                switch (flag)
                {
                    case ELogflag.Info:
                        sb.Append("[Info]");
                        break;
                    case ELogflag.Warning:
                        sb.Append("[Warning]");
                        break;
                    case ELogflag.Critical:
                        sb.Append("[Critical]");
                        break;
                    case ELogflag.Custom:
                        title = title.ToUpper();
                        sb.Append("[" + title + "]");
                        break;
                    default:
                        sb.Append("[UNKNOWN]");
                        break;
                }

                sb.Append(msg);
                WriteLog(engineName + "log.txt", sb.ToString());
                logdata.Add(sb.ToString());
            }

            public static bool WriteLog(string strFileName, string strMessage)
            {
                try
                {
                    var path = Path.GetTempPath();
                    FileStream objFilestream =
                        new FileStream(string.Format("{0}\\{1}", Path.GetTempPath(), strFileName), FileMode.Append,
                            FileAccess.Write);
                    StreamWriter objStreamWriter = new StreamWriter((Stream) objFilestream);
                    objStreamWriter.WriteLine(strMessage);
                    objStreamWriter.Close();
                    objFilestream.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            public static void ClearLogData()
            {
                logdata.Clear();
            }
        }
    }

    public static class FileHelper
    {
        public static object LoadFromFile(string filepath)
        {
            Type type = null;
            return LoadFromFile(filepath, out type);
        }

        public static object LoadFromFile(string filepath, out Type objtype)
        {
            System.IO.FileInfo file = new System.IO.FileInfo(filepath);
            string[] loaded = System.IO.File.ReadAllLines(file.FullName, Encoding.UTF8);
            string joined = string.Join("\r\n", loaded.Skip(1).ToArray());
            object newobj = JsonConvert.DeserializeObject(joined, Type.GetType(loaded[0]));

            objtype = Type.GetType(loaded[0]);
            return newobj;
        }

        public static bool SaveToFile(string filepath, object tosave)
        {
            try
            {
                System.IO.FileInfo file = new System.IO.FileInfo(filepath);
                if (!System.IO.File.Exists(filepath)) //if file exists
                {
                    file.Directory.Create();
                }

                string output = tosave.GetType() + "\r\n" + Serializer.Serialize(tosave, true);
                System.IO.File.WriteAllLines(filepath, new[] {output}, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void SaveAsPGM(string filepath, double[,] tosave)
        {
            int h = tosave.GetLength(0);
            int w = tosave.GetLength(1);
            string ppm = $"P2\n{h} {w}\n255\n";
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    ppm += (int) (tosave[i, j] * 255) + " ";
                }

                ppm += "\n";
            }

            System.IO.File.WriteAllText(filepath, ppm, Encoding.ASCII);
        }
    }

    public static class StringHelper
    {
        private static readonly Regex sWhitespace = new Regex(@"\s+");

        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }

        private static readonly Regex sNonPrintable = new Regex(@"\p{C}+");

        public static string ReplaceNonPrintable(string input, string replacement)
        {
            return sNonPrintable.Replace(input, replacement);
        }

        private static readonly Regex sNameable = new Regex(@"[^a-zA-Z0-9-_]+");

        public static string ReplaceNameable(string input, string replacement)
        {
            return sNameable.Replace(input, replacement);
        }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(Point a, Point b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Point a, Point b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return obj is Point b ? (this.X == b.X && this.Y == b.Y) : false;
        }

        public static Point Parse(string str)
        {
            var cords = str.Split(',');
            return new Point(Double.Parse(cords[0]), Double.Parse(cords[1]));
        }

        public static double DistanceFrom(Point start, Point end)
        {
            var a = Math.Abs(end.X - start.X);
            var b = Math.Abs(end.Y - start.Y);
            return Math.Sqrt(a + b);
        }
    }

    public class PointInt
    {
        public int X { get; set; }
        public int Y { get; set; }

        public PointInt(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static bool operator ==(PointInt a, PointInt b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(PointInt a, PointInt b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return obj is PointInt b ? (this.X == b.X && this.Y == b.Y) : false;
        }

        public static PointInt Parse(string str)
        {
            var cords = str.Split(',');
            return new PointInt(Int32.Parse(cords[0]), Int32.Parse(cords[1]));
        }

        public static double DistanceFrom(PointInt start, PointInt end)
        {
            var a = Math.Abs(end.X - start.X);
            var b = Math.Abs(end.Y - start.Y);
            return Math.Sqrt(a + b);
        }
    }

    /// <summary>
    /// Used to define classes that can have a parent
    /// </summary>
    /// <typeparam name="TParent">Type that is parent</typeparam>
    public interface IHasParent<TParent> where TParent : class
    {
        [JsonIgnore]
        TParent Parent { get; set; }

        void OnParentChanging(TParent newParent)
        {
            Parent = newParent;
        }
    }

    /// <summary>
    /// Helper for classes that need a parent to child correlation
    /// </summary>
    /// <typeparam name="TParent">Parent type - one</typeparam>
    /// <typeparam name="TChild">Child type - many</typeparam>
    public class ChildCollection<TParent, TChild> : ObservableCollection<TChild>
        where TChild : IHasParent<TParent>
        where TParent : class
    {
        readonly TParent parent;

        public ChildCollection(TParent parent)
        {
            this.parent = parent;
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                if (item != null)
                    item.OnParentChanging(null);
            }

            base.ClearItems();
        }

        protected override void InsertItem(int index, TChild item)
        {
            if (item != null)
                item.OnParentChanging(parent);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item != null)
                item.OnParentChanging(null);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TChild item)
        {
            var oldItem = this[index];
            if (oldItem != null)
                oldItem.OnParentChanging(null);
            if (item != null)
                item.OnParentChanging(parent);
            base.SetItem(index, item);
        }
    }
}