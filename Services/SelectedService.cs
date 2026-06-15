using EHMR.Domain.Interfaces;

namespace EHMR.Services
{
    public class SelectedItemService<T> : ISelectedItemService<T>
    {
        private T? _selectedItem;

        // Property implementation
        public T? SelectedItem
        {
            get => _selectedItem;
            set => _selectedItem=value;
        }

        // Optional helper methods
        public T? GetSelectedItem() => _selectedItem;

        public void SetSelectedItem(T item) => _selectedItem=item;
    }
}