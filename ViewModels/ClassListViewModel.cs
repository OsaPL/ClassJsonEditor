using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ClassJsonEditor.Models;

namespace ClassJsonEditor.ViewModels
{
    public class ClassListViewModel
    {
        public ClassListViewModel(IEnumerable<ClassRepresentation> items, Action<ClassRepresentation> callback)
        {
            Items = new ObservableCollection<ClassRepresentation>(items);
            _onSelectCallback = callback;
        }

        private readonly Action<ClassRepresentation> _onSelectCallback;

        public ObservableCollection<ClassRepresentation> Items { get; }

        public void OnSelect(ClassRepresentation selected)
        {
            _onSelectCallback(selected);
        }
    }
}