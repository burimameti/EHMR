namespace EHMR.Domain.Interfaces
{
    public interface IAuthStateService
    {
        public string UserName
        {
            get; set;
        }

        bool IsAuthenticated
        {
            get;
        }

        IReadOnlyList<string> Roles
        {
            get;
        }

        IReadOnlyList<string> Permissions
        {
            get;
        }

        IReadOnlyList<string> Modules
        {
            get;
        }

        string? AccessToken
        {
            get;
        }

        event EventHandler? AuthStateChanged;

        void SetState(
            string token,
            IEnumerable<string> roles,
            IEnumerable<string> permission, IEnumerable<string>? modules
     );

        void Clear();
    }
}