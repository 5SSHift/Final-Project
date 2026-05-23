namespace Wpf.Models
{
    /// <summary>Date introduse de utilizator în fereastra de înregistrare.</summary>
    public class RegisterModel
    {
        public string Username        { get; set; } = string.Empty;
        public string Email           { get; set; } = string.Empty;
        public string Password        { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role            { get; set; } = "Client";

        public string[] AvailableRoles { get; } = { "Client", "Employee", "Administrator" };

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(Username) || Username.Trim().Length < 3)
            { error = "Username trebuie să aibă minim 3 caractere."; return false; }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            { error = "Parola trebuie să aibă minim 6 caractere."; return false; }

            if (Password != ConfirmPassword)
            { error = "Parolele nu coincid."; return false; }

            error = string.Empty;
            return true;
        }
    }
}
