using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

        public void AddClass(Type type)
        {
            //Add class to the list
            ClassViewTreeItem item;
            try
            {
                var obj = Activator.CreateInstance(type);
                item = new ClassViewTreeItem(type.FullName, type, obj);
            }
            catch (Exception e)
            {
                //item = new ClassViewTreeItem(type.FullName, type, null);
                Console.WriteLine(e);
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