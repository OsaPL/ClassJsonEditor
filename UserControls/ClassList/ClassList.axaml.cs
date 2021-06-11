using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ClassJsonEditor.Models;
using ClassJsonEditor.ViewModels;

namespace ClassJsonEditor.UserControls
{
    public class ClassList : UserControl
    {
        public ClassList()
        {
            InitializeComponent();
        }

        private ClassRepresentation _selected;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private ClassListViewModel _context
        {
            get => (DataContext as ClassListViewModel);
        }

        private void InputElement_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var item = (e.AddedItems[0] as ClassRepresentation);
            if (item != null && _selected != item)
            {
                _selected = item;
                _context.OnSelect(_selected);
            }
        }
    }
}