using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace ClassJsonEditor
{
    /// <summary>
    /// Used to define classes that can have a parent
    /// </summary>
    /// <typeparam name="TParent">Type that is parent</typeparam>
    public interface IHasParent<TParent> where TParent : class
    {
        [JsonIgnore]
        TParent Parent { get; set; }

        void OnParentChanging(TParent newParent)
        {
            Parent = newParent;
        }
    }

    /// <summary>
    /// Helper for classes that need a parent to child correlation
    /// </summary>
    /// <typeparam name="TParent">Parent type - one</typeparam>
    /// <typeparam name="TChild">Child type - many</typeparam>
    public class ObservableChildCollection<TParent, TChild> : ObservableCollection<TChild>
        where TChild : IHasParent<TParent>
        where TParent : class
    {
        readonly TParent parent;

        public ObservableChildCollection(TParent parent)
        {
            this.parent = parent;
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                if (item != null)
                    item.OnParentChanging(null);
            }

            base.ClearItems();
        }

        protected override void InsertItem(int index, TChild item)
        {
            if (item != null)
                item.OnParentChanging(parent);
            base.InsertItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];
            if (item != null)
                item.OnParentChanging(null);
            base.RemoveItem(index);
        }

        protected override void SetItem(int index, TChild item)
        {
            var oldItem = this[index];
            if (oldItem != null)
                oldItem.OnParentChanging(null);
            if (item != null)
                item.OnParentChanging(parent);
            base.SetItem(index, item);
        }
    }
}