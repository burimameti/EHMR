namespace EHMR.Domain.Interfaces
{
    public interface INavigationCoordinator
    {
        void RegisterHandler(Func<string, Task> handler);

        Task NavigateAsync(string route);
    }

    public class NavigationCoordinator : INavigationCoordinator
    {
        private Func<string, Task>? _handler;

        public void RegisterHandler(Func<string, Task> handler)
        {
            _handler=handler;
        }

        public Task NavigateAsync(string route)
        {
            if(_handler==null)
                return Task.CompletedTask;

            return _handler(route);
        }
    }
}