using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using ClassJsonEditor.Models;
using ClassJsonEditor.UserControls;
using MercsCodeBaseTest;
using ReactiveUI;

namespace ClassJsonEditor.ViewModels
{
    // Responsible for the top hierarchy on the view
    public class ClassViewModel : ViewModelBase
    {
        public ClassViewModel(Action<object> onSelectCallback)
        {
            _onSelectCallback = onSelectCallback;
            Items = new ObservableCollection<ClassViewTreeItem>();
        }
        
        private readonly Action<object> _onSelectCallback;

        public void AddClass(ClassRepresentation classRepresentation)
        {
            //Add class to the list
            ClassViewTreeItem item;
            try
            {
                object obj;
                if (classRepresentation.IsReflectionOnly)
                {
                    ReflectionsHelper.first = true;
                    obj = classRepresentation.Type.ActivateOnlyProperties();   
                    item = new ClassViewTreeItem(classRepresentation.Type.FullName, classRepresentation.Type, obj);
                    item.ReflectionOnlyMode = true;
                }
                else
                {
                    obj = Activator.CreateInstance(classRepresentation.Type);
                }
                item = new ClassViewTreeItem(classRepresentation.Type.FullName, classRepresentation.Type, obj);
            }
            // This will catch all ctor problems, means that probably we should load this with ReflectionMode
            catch (TargetInvocationException e)
            {
                classRepresentation.IsReflectionOnly = true;
                AddClass(classRepresentation);
                return;
            }
            item.Parse();
            Items.Add(item);
        }

        private ObservableCollection<ClassViewTreeItem> _items;

        public ObservableCollection<ClassViewTreeItem> Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        public void OnSelect(object selected)
        {
            _onSelectCallback(selected);
        }
    }
}