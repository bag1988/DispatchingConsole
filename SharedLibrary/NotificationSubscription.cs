namespace SharedLibrary
{
    public class NotificationSubscription
    {
        public string? Endpoint { get; set; }
        public string? P256dh { get; set; }
        public string? Auth { get; set; }
        public int CountError { get; set; } = 0;
        public string? IpClient { get; set; }
        public string? UserAgent { get; set; }
        public string? Host { get; set; }
    }
}
