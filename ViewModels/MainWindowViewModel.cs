using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using ClassJsonEditor.Models;
using ClassJsonEditor.UserControls;
using MercsCodeBaseTest;
using ReactiveUI;

namespace ClassJsonEditor.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        
        public MainWindowViewModel()
        {
            Classes = new ClassViewModel(UpdateExample);
            
            var loaded = LoadClasses(null);
           
            List = new ClassListViewModel(loaded, representation =>  Classes.AddClass(representation.Type));
        }
        
        private IEnumerable<ClassRepresentation> LoadClasses(string[] paths)
        {
            string[] list = new[]
            {
                @"E:\Steam\steamapps\common\H3VR\h3vr_Data\Managed\Assembly-CSharp.dll",
                @"C:\Users\Osa-Master\RiderProjects\ClassJsonEditor\DummyLib.dll"
            };

            List<ClassRepresentation> classes = new List<ClassRepresentation>();

            foreach (string path in list)
            {
                Assembly dll;
                try
                {
                    // LoadFrom also loads all dependencies, probably not safe, but is good for now
                    dll = Assembly.LoadFrom(path);
                    classes.AddRange(dll.GetExportedTypes().Select(x => new ClassRepresentation(x)));
                    classes.Sort((x, y) => string.Compare(x.Type.Name, y.Type.Name));
                }
                catch (Exception e)
                {
                    // When we catch an exception, probably we need to load it shallowly
                    
                    // ReflectionOnlyLoadFrom load an assembly into the reflection-only context. Assemblies loaded into this context can be examined but not executed!
                    // Doesnt work in .net core, and System.Reflection.TypeLoader/Metadata is in development hell it seems, bummer
                    //var DLL = Assembly.ReflectionOnlyLoadFrom(list[1]);

                    // Using the TypeLoader from old experimental corefx repo.
                    // TODO! Include the code for this and use reference, when sln files start working again.
                    // THis allows is to use loaded types, without actually trying to instantiate it. This will make loading almost any dll possible, but only as models only (no actual functionality will be available)
                    // Cool, still requires tho separate reflection stuff to actually make use of that reflecion info we get, since CreateInstance and other built in ways WILL NOT WORK.
                    var loader = new System.Reflection.TypeLoader();
                    dll = loader.LoadFromAssemblyPath(path);
                    classes.AddRange(dll.GetExportedTypes().Select(x => new ClassRepresentation(x, true)));
                }
            }
            return classes;
        }

        public ClassListViewModel List { get; }
        
        public ClassViewModel Classes { get; }

        private string _example;
        public string Example {           
            get => _example;
            set
            {
                this.RaiseAndSetIfChanged(ref _example, value);
            }
        }

        // TODO! Make this a callback to have responsive ui on each action in the middle panel
        public void UpdateExample(object obj)
        {
            try
            {
                Example = Serializer.Serialize(obj, true, true);
            }
            catch (Exception e)
            {
                Example = e.ToString();
            }
        }

    }
}
