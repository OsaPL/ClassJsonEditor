using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ClassJsonEditor.ViewModels;
using MercsCodeBaseTest;
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

        private IBrush _oldBrush;

        private void ValueTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                var objItem = textBox?.DataContext as ClassViewTreeItem;
                var newVal = Serializer.Deserialize(textBox?.Text, objItem.Type);
                if (newVal != null)
                {
                    Dispatcher.UIThread.Post(new Action(async () =>
                    {
                        var currentBrush = textBox.Foreground.ToImmutable();
                        textBox.Foreground = new SolidColorBrush(Colors.Green);

                        await Task.Delay(4000);
                        // Field is being `fixed` by the user, rever to the pre-broken state
                        if (_oldBrush != null)
                        {
                            textBox.Foreground = _oldBrush;
                            _oldBrush = null;
                        }
                        else
                        {
                            textBox.Foreground = currentBrush;
                        }
                    }));

                    textBox.Parent.Focus();
                    objItem.Objecto = newVal;
                    objItem.Parse();
                    
                    _context.OnSelect(objItem.GetTopMostParent().GetAsObject());
                }
                else
                {
                    Dispatcher.UIThread.Post(new Action(async () =>
                    {
                        _oldBrush = textBox.Foreground.ToImmutable();
                        textBox.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                }
            }
        }

        private void Init_Button_Click(object? sender, RoutedEventArgs e)
        {
            var nullable = ((sender as Button)?.DataContext as ClassViewTreeItem);
            nullable.Objecto = Activator.CreateInstance(nullable.Type);
            nullable.Parse();

            _context.OnSelect(nullable.GetTopMostParent().GetAsObject());
        }

        private async void Add_Button_Click(object? sender, RoutedEventArgs e)
        {
            var collectionItem = ((sender as Button)?.DataContext as ClassViewTreeItem);

            if (collectionItem.IsDict)
            {
                throw new NotSupportedException("Dict support is a big one");
                // //TODO: Find out a better way, Im assuming all dicts will have their 1st generic argument the type of the key, and second the value type
                // Type[] arguments = collectionItem.Type.GetGenericArguments();
                // Type key = arguments[0];
                // var keyTypes = ReflectionsHelper.GetCompatibleTypes(key);
                //
                // Type value = arguments[1];
                // var valueTypes = ReflectionsHelper.GetCompatibleTypes(value);
                //
                // if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                // {
                //     var selectBox1 = new SelectListMsgBox() {Title = "Select Key Type..."};
                //     var selectedKey = await selectBox1.ShowListBox(desktop.MainWindow, keyTypes);
                //     var selectBox2 = new SelectListMsgBox(){Title = "Select Value Type..."};
                //     var selectedValue = await selectBox2.ShowListBox(desktop.MainWindow, valueTypes);
                //     
                //     //TODO! This WILL CRASH for string cause default(string) is null
                //     (collectionItem.Objecto as IDictionary).Add(((Type)selectedKey).ActivateInstance(),((Type)selectedValue).ActivateInstance());
                //     collectionItem.Parse();
                //     if (!@collectionItem.Parent.IsPrimitive)
                //     {
                //         //TODO! This should call the top most
                //         _context.OnSelect(@collectionItem.Parent.GetAsObject());
                //     }
                // }
            }
            else if (collectionItem.IsCollection)
            {
                //TODO: Find out a better way, Im assuming all collections will have their 1st generic argument the type of the items
                Type argument = collectionItem.Type.GetGenericArguments()[1];
                var types = ReflectionsHelper.GetCompatibleTypes(argument);
                if (Avalonia.Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    var selectBox = new SelectListMsgBox() {Title = "Select Element Type..."};
                    var selected = await selectBox.ShowListBox(desktop.MainWindow, types) as Type;
                    var newObj = Activator.CreateInstance(selected);

                    // Doing some reflection magic, to be able to call Add (ICollection doesnt implement Add, yet ICollection<T> does, that why I dont just do `as ICollection` cast
                    collectionItem.Type.GetMethod("Add").Invoke(collectionItem.Objecto, new[] {newObj});
                    collectionItem.Parse();
                    
                    _context.OnSelect(collectionItem.GetTopMostParent().GetAsObject());
                }
            }
        }

        private void EnumComboBox_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var t = e.AddedItems.First<object>();
            if (t != null)
            {
                ClassViewTreeItem @enum = ((sender as ComboBox)?.DataContext as ClassViewTreeItem);
                @enum.Objecto = t;
                _context.OnSelect(@enum.GetTopMostParent().GetAsObject());
            }
        }

        private void TreeView_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            var t = e.AddedItems.First<ClassViewTreeItem>();
            if (t != null)
            {
                _context.OnSelect(t.GetTopMostParent().GetAsObject());
            }
        }

        private void ToggleButton_OnChecked(object? sender, RoutedEventArgs e) => SetBooleanValue(sender, e, true);
        private void ToggleButton_OnUnchecked(object? sender, RoutedEventArgs e) => SetBooleanValue(sender, e, false);

        private void SetBooleanValue(object? sender, RoutedEventArgs e, bool bValue)
        {
            ClassViewTreeItem @bool = ((sender as CheckBox)?.DataContext as ClassViewTreeItem);
            if (@bool.Type == typeof(bool))
            {
                @bool.Objecto = bValue;
                _context.OnSelect(@bool.GetTopMostParent().GetAsObject());
            }
        }
    }
}