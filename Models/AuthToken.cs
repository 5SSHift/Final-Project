namespace Wpf.Models
{
    public class AuthToken
    {
        public string   Raw       { get; init; } = string.Empty;
        public int      UserId    { get; init; }
        public string   Username  { get; init; } = string.Empty;
        public string   Role      { get; init; } = string.Empty;
        public DateTime IssuedAt  { get; init; }
        public DateTime ExpiresAt { get; init; }

        public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        public bool IsValid   => !IsExpired;
    }
}
