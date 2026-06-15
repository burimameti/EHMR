namespace EHMR.Domain.Interfaces
{
    public interface ISelectedItemService<T>
    {
        T? SelectedItem
        {
            get; set;
        }
    }
}
