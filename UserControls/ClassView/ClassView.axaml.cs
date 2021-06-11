using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using ClassJsonEditor.ViewModels;
using static ClassJsonEditor.AvaloniaHelpers;

namespace ClassJsonEditor.UserControls
{
    public class ClassView : UserControl
    {
        private ClassViewModel _context
        {
            get => (DataContext as ClassViewModel);
        }

        public ClassView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void FieldTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void ValueTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //Validate
                //throw new System.NotImplementedException();
            }
        }

        private void Init_Button_Click(object? sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Add_Button_Click(object? sender, RoutedEventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void EnumComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var t = e.AddedItems.First<object>();
            if (t != null)
            {
                ClassViewTreeItem @enum = ((sender as ComboBox)?.DataContext as ClassViewTreeItem);
                @enum.Objecto = t;
                if (! @enum.Parent.IsPrimitive)
                {
                    _context.OnSelect(@enum.Parent.GetAsObject());
                }
            }
        }

        private void TreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var t = e.AddedItems.First<ClassViewTreeItem>();
            if (t != null)
            {
                if (!t.IsPrimitive)
                {
                    _context.OnSelect(t.GetAsObject());
                }
            }
        }
    }
}