using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
                this.RaisePropertyChanged(nameof(IsNull));
                this.RaisePropertyChanged(nameof(IsCollection));
                this.RaisePropertyChanged(nameof(IsPrimitive));
                this.RaisePropertyChanged(nameof(IsBool));
                this.RaisePropertyChanged(nameof(IsEnum));
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

        public bool IsDict
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

        public bool IsCollection
        {
            get
            {
                if (Objecto == null)
                {
                    return false;
                }

                return TypeChecker.IsCollection(this.Type);
            }
        }

        public bool IsNull => Objecto == null;

        #endregion

        public ClassViewTreeItem GetTopMostParent()
        {
            ClassViewTreeItem topMost;
            //TODO! This should call the top most
            if (this.Parent != null)
            {
                topMost = this.Parent;
                while (topMost.Parent != null)
                {
                    topMost = topMost.Parent;
                }
            }
            else
            {
                topMost = this;
            }

            return topMost;
        }

        public void Parse()
        {
            // If null, dont parse, no reason to
            if (Objecto == null)
                return;
            Items.Clear();
            //check if we can just do IDictionary on it
            dynamic dictionary = Objecto as System.Collections.IDictionary;

            if (dictionary != null)
            {
                foreach (dynamic item in dictionary)
                {
                    ClassViewTreeItem child = new ClassViewTreeItem(item.Key, item.Value.GetType(), item.Value);

                    Items.Add(child);
                    if (!TypeChecker.IsReallyPrimitive(item.Value.GetType()))
                    {
                        child.Parse();
                    }
                }
            }
            else
            {
                var collection = Objecto as System.Collections.ICollection;
                if (collection != null)
                {
                    int i = 0;
                    foreach (var item in collection)
                    {
                        ClassViewTreeItem child = new ClassViewTreeItem(i.ToString(), item.GetType(), item);

                        Items.Add(child);
                        if (!TypeChecker.IsReallyPrimitive(item.GetType()))
                        {
                            child.Parse();
                        }

                        i++;
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
            }

            this.RaisePropertyChanged();
        }

        public object GetAsObject()
        {
            if (IsPrimitive)
                return Objecto;

            ExpandoObject? obj = new ExpandoObject();
            PrepareGetAsObject(ref obj);
            return obj;
        }

        private void PrepareGetAsObject(ref ExpandoObject? expando)
        {
            if (expando == null)
            {
                throw new NoNullAllowedException();
            }

            string serialized = string.Empty;
            if (Items != null)
            {
                if (Items.Count > 0)
                {
                    foreach (ClassViewTreeItem item in Items)
                    {
                        if (item.IsPrimitive)
                        {
                            AddProperty(expando, item.FieldName, item.Objecto);
                        }
                        else
                        {
                            var newExpando = new ExpandoObject();
                            AddProperty(expando, item.FieldName, newExpando);
                            item.PrepareGetAsObject(ref newExpando);
                        }
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
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

    public static class ReflectionsHelper
    {
        public static IEnumerable<Type> GetCompatibleTypes(Type type)
        {
            var types = Assembly.GetAssembly(type).GetLoadableTypes().Where(x => x.IsAssignableTo(type));
            //TODO! Get hold of all assemblies 

            return types;
        }

        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        //TODO! Replace all usages of Activator.CreateInstance() with this!
        public static object ActivateInstance(this Type type)
        {
            var t = typeof(ReflectionsHelper);
            var met = t.GetMethods();
            var sel = met.Single(x => x.Name == nameof(ActivateInstance) && x.IsGenericMethod);
            var gen = sel.MakeGenericMethod(type);

            return gen.Invoke(null, new object?[] {null});
        }

        public static T ActivateInstance<T>(this T obj)
        {
            if (typeof(T).IsValueType || typeof(T) == typeof(string))
            {
                return default(T);
            }
            else
            {
                return Activator.CreateInstance<T>();
            }
        }
    }
}