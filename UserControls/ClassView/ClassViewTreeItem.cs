using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia.Data;
using Avalonia.Data.Converters;
using ClassJsonEditor.ViewModels;
using MercsCodeBaseTest;
using Newtonsoft.Json;
using ReactiveUI;

namespace ClassJsonEditor.UserControls
{
    //TODO! Replace all calls to prop getters to call to the corresponding private field
    //TODO! Cleanup the need to get Type seperately, yet in some places I use Objecto.GetType()
    //... not to mention that IsReallyPrimitive is called with both type and objecto, thats dumb
    public class ClassViewTreeItem : ViewModelBase, IHasParent<ClassViewTreeItem>
    {
        public ClassViewTreeItem()
        {
            Items = new ObservableChildCollection<ClassViewTreeItem, ClassViewTreeItem>(this);
        }

        public ClassViewTreeItem(string fieldname, Type type, object obj)
        {
            Items = new ObservableChildCollection<ClassViewTreeItem, ClassViewTreeItem>(this);

            FieldName = fieldname;
            Type = type;
            Objecto = obj;
        }

        #region Properties

        public ObservableChildCollection<ClassViewTreeItem, ClassViewTreeItem> Items { get; }

        public ClassViewTreeItem Parent { get; set; }

        private string _fieldName;

        public string FieldName
        {
            get => _fieldName;
            set { this.RaiseAndSetIfChanged(ref _fieldName, value); }
        }

        private Type _type;

        public Type Type
        {
            get => _type;
            set { this.RaiseAndSetIfChanged(ref _type, value); }
        }

        private object _objecto;

        public object Objecto
        {
            get => _objecto;
            set
            {
                if ((value?.GetType() != Type && typeof(string) != Type) && (IsPrimitive && value is null))
                    throw new NotSupportedException("Objecto type is different than type already set!");

                this.RaiseAndSetIfChanged(ref _objecto, value);
            }
        }

        public bool IsPrimitive
        {
            get
            {
                if (Objecto == null)
                {
                    return false;
                }

                var result = TypeChecker.IsReallyPrimitive(Objecto);
                return result;
            }
        }

        public bool IsEnum
        {
            get
            {
                if (Objecto == null)
                {
                    return false;
                }

                var result = Objecto.GetType().IsEnum;

                return result;
            }
        }

        // TODO! This should probably be parsed in Parse()
        private ObservableCollection<object> _enums;

        public ObservableCollection<object> Enums
        {
            get
            {
                if (IsEnum && _enums == null)
                    _enums = new ObservableCollection<object>(Enum.GetValues(Objecto.GetType()).Cast<object>());
                return _enums;
            }
            private set { this.RaiseAndSetIfChanged(ref _enums, value); }
        }
        
        public bool IsBool
        {
            get
            {
                if (Objecto == null)
                {
                    return false;
                }

                var result = Objecto is bool;

                return result;
            }
        }

        public bool IsCollection
        {
            get
            {
                if (Objecto == null)
                {
                    return false;
                }

                return TypeChecker.IsDictionary(this.Type);
            }
        }

        public bool IsNull
        {
            get
            {
                if (Objecto == null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        #endregion

        public void Parse()
        {
            // If null, dont parse, no reason to
            if (Objecto == null)
                return;
            Items.Clear();
            //check if we can just do IDictionary on it
            dynamic enumerable = Objecto as System.Collections.IDictionary;

            if (enumerable != null)
            {
                foreach (dynamic item in enumerable)
                {
                    var child = new ClassViewTreeItem(item.Key, item.Value.GetType(), item.Value);

                    if (TypeChecker.IsReallyPrimitive(item.Value.GetType()))
                    {
                        child = new ClassViewTreeItem(item.Key, item.Value.GetType(), item.Value);
                        Items.Add(child);
                    }
                    else
                    {
                        child.Parse();
                        Items.Add(child);
                    }
                }
            }
            else
            {
                if (!TypeChecker.IsReallyPrimitive(Objecto))
                {
                    var type = Objecto.GetType();
                    // Need to check if the type is actually a type or a RuntimeType
                    List<object> fieldValues = type
                        .GetFields()
                        .Select(field => field.GetValue(Objecto))
                        .ToList();

                    List<string> fieldnames = type
                        .GetFields()
                        .Select(field => field.Name)
                        .ToList();
                    List<Type> fieldTypes = Type
                        .GetFields()
                        .Select(field => field.FieldType)
                        .ToList();

                    using (var e1 = fieldValues.GetEnumerator())
                    using (var e2 = fieldnames.GetEnumerator())
                    using (var e3 = fieldTypes.GetEnumerator())
                    {
                        while (e1.MoveNext() && e2.MoveNext() && e3.MoveNext())
                        {
                            var item1 = e1.Current;
                            var item2 = e2.Current;
                            var item3 = e3.Current;

                            var child = new ClassViewTreeItem(item2, item3, item1);

                            if (TypeChecker.IsReallyPrimitive(item3))
                            {
                                Items.Add(child);
                            }
                            else
                            {
                                child.Parse();
                                Items.Add(child);
                            }

                            //create new treeitems
                        }
                    }
                }
            }

            this.RaisePropertyChanged();
        }

        public object GetAsObject()
        {
            if (IsPrimitive)
                return Objecto;
            
            object obj = PrepareGetAsObject();
            string str = Serializer.Serialize(obj, false, true);
            //string pattern = "((\"\\w+\"): {\\n\\s *\\2:\\s *.*\\n\\s *})";
            //string pattern = @"(([^{""]*""[a-zA-Z0-9]+.*""):{\2:([^}]+)})";
            string pattern = @"(""([^""]*)""\s*:\s*{""\2""\s*:(\s*""?[a-zA-Z0-9-_.,]+""?)})";

            //replace whole match with "\""+C2+"\""+":"+"\""+C3+"\"
            var maczer = Regex.Match(str, pattern);
            var newstr = Regex.Replace(str, pattern, m => string.Format(
                @"""{0}"":{1}",
                m.Groups[2].Value,
                m.Groups[3].Value));

            var tmp = JsonConvert.DeserializeObject(newstr);

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            var temp = JsonConvert.DeserializeObject(Serializer.Serialize(tmp, true, true), Type, settings);

            return temp;
        }

        private object PrepareGetAsObject()
        {
            dynamic expando = new ExpandoObject();
            string serialized = string.Empty;
            if (Items != null)
            {
                if (Items.Count > 0)
                {
                    foreach (ClassViewTreeItem item in Items)
                    {
                        var obj = item.PrepareGetAsObject();
                        if (obj == null)
                        {
                            AddProperty(expando, item.FieldName, null);
                        }
                        else
                        {
                            AddProperty(expando, item.FieldName, obj);
                        }
                    }
                }
                else
                {
                    if (Objecto == null)
                    {
                        AddProperty(expando, FieldName, null);
                    }
                    else
                    {
                        AddProperty(expando, FieldName, Objecto);
                    }
                }
            }
            else
            {
                if (Objecto == null)
                {
                    AddProperty(expando, FieldName, null);
                }
                else
                {
                    AddProperty(expando, FieldName, Objecto);
                }
            }

            return expando;
        }

        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            // ExpandoObject supports IDictionary so we can extend it like this
            var expandoDict = expando as IDictionary<string, object>;
            if (expandoDict.ContainsKey(propertyName))
                expandoDict[propertyName] = propertyValue;
            else
                expandoDict.Add(propertyName, propertyValue);
        }

        public override string ToString()
        {
            if (Objecto == null && Type == null)
            {
                return FieldName + " : " + "null" + " : " + "null";
            }
            else if (Objecto == null)
            {
                return FieldName + " : " + Type + " : " + "null";
            }
            else if (Type == null)
            {
                return FieldName + " : " + "null" + " : " + Objecto.ToString();
            }
            else
            {
                if (Items != null)
                {
                    return FieldName + " : " + Type + " : ";
                }

                string str = Regex.Replace(Objecto.ToString(), @"\s+", "");
                return FieldName + " : " + Type + " : " + str;
            }
        }
    }
}