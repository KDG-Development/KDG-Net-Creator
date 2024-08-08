namespace KDG.Zoho.Creator.Config
{
    public class Config
    {
        public string TokenUri { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public IEnumerable<string> Scopes { get; set; } = Enumerable.Empty<string>();
    }
}
