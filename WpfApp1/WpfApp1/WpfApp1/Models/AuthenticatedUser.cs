namespace WpfApp1.Models
{
    public sealed class AuthenticatedUser
    {
        public int Id { get; }
        public string Login { get; }
        public string Role { get; }
        public string FullName { get; }

        public AuthenticatedUser(int id, string login, string role, string fullName)
        {
            Id = id;
            Login = login;
            Role = role;
            FullName = fullName;
        }

        public string DisplayCaption
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(FullName))
                {
                    return FullName.Trim();
                }

                return Login;
            }
        }

        public string RoleCaption
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Role))
                {
                    return "—";
                }

                return Role.Trim();
            }
        }
    }
}
