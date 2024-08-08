namespace KDG.Zoho.Creator.Services
{
    public class AccessToken<A>
    {
        private Uri Uri { get; }
        private Dictionary<string, string?> QueryParams { get; }
        public AccessToken(Uri url, Dictionary<string, string?> queryParams) // Constructor
        {
            Uri = url;
            QueryParams = queryParams;
        }

        private async Task<A> fetchNew()
        {
            using var client = new HttpClient();
            var uri = KDG.Connector.Utilities.Parameters.GenerateUri(Uri, QueryParams);
            var response = await client.PostAsync(uri, null);
            var contents = await response.Content.ReadAsStringAsync();
            var token = System.Text.Json.JsonSerializer.Deserialize<A>(contents);
            if(token == null)
            {
                throw new Exception("Cannot fetch new Access Token");
            }
            return token;
        }
        public async Task<A> getAccessToken()
        {
            var token = await fetchNew();
            return token;
        }
    }
}
