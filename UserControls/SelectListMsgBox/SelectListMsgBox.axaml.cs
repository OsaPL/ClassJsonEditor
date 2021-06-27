using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ClassJsonEditor.UserControls
{
    public class SelectListMsgBox : Window
    {
        public SelectListMsgBox()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private TaskCompletionSource<object> _tcs;

        public Task<object> ShowListBox(Window parent, IEnumerable<object> list, PixelPoint point)
        {
            Position = point;
            return ShowListBox(parent, list);
        }

        public Task<object> ShowListBox(Window parent, IEnumerable<object> list)
        {
            DataContext = list;
            _tcs = new TaskCompletionSource<object>();
            if (parent != null)
                ShowDialog(parent);
            else Show();
            return _tcs.Task;
        }

        private async void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            _tcs.TrySetResult(e.AddedItems[0]);
            await _tcs.Task;
            Close();
        }
    }
}