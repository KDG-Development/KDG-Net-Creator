using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using KDG.Connector;
using KDG.Zoho.Creator.Models;
using KDG.Zoho.Creator.Utilities;
using NodaTime;

namespace KDG.Zoho.Creator.Services
{
    public class ZohoConnector : KDG.Connector.Connector
    {
        public ZohoConnector(ILogger<ZohoConnector> logger, Newtonsoft.Json.JsonSerializer serializer, JsonSerializerSettings serializationSettings,Config.Config config,IClock clock)
        : base(CreatorBaseUrl,logger, serializer, serializationSettings)
        {
            _config = config;
            _logger = logger;
            _clock = clock;
        }
        protected const string CreatorBaseUrl = "https://creator.zoho.com/api/v2.1/";
        protected readonly ILogger<ZohoConnector> _logger;
        private readonly Config.Config _config;
        protected ZohoAccessToken? _currentToken;
        protected long? _tokenExpiration;
        protected IClock _clock;
        protected const int ReportLimit = 200;

        #region Helper Methods
        private KDG.Connector.Models.OAuthConfiguration GetOAuthConfiguration()
        {
            var config = new KDG.Connector.Models.OAuthConfiguration()
            {
                RefreshToken = _config.RefreshToken,
                ClientId = _config.ClientId,
                ClientSecret = _config.ClientSecret,
                TokenUri = _config.TokenUri,
            };

            return config;
        }
        protected override async Task<AuthenticationHeaderValue> GetAuthenticationHeaderValue()
        {
            var token = await GetAccessToken();
            return new System.Net.Http.Headers.AuthenticationHeaderValue("Zoho-oauthtoken", token);
        }
        protected override Task<KDG.Connector.Models.Response<RESPONSE>> Send<RESPONSE>(HttpMethod method, string path, KDG.Connector.Models.ApiParams config, bool logResponseData = true, string? baseUrlOverride = null)
        {
            return base.Send<RESPONSE>(method, path, config, logResponseData, baseUrlOverride);
        }
        public async Task<string> GetAccessToken()
        {
            var now = _clock.GetCurrentInstant().ToUnixTimeSeconds();
            if(_currentToken == null || _tokenExpiration < now)
            {
                var gen = AccessTokenGenerator();
                var token = await gen.getAccessToken();
                _tokenExpiration = now + (token.ExpiresIn / 2);
                _currentToken = token;
            }
            return _currentToken.AccessToken;
        }
        private AccessToken<ZohoAccessToken> AccessTokenGenerator()
        {
            var config = GetOAuthConfiguration();
            var token = new AccessToken<ZohoAccessToken>(
                new Uri(config.TokenUri),
                new Dictionary<string, string?>()
                {
                    [ReferenceValuesHelper.OAuth.RefreshTokenLabel] = config.RefreshToken,
                    [ReferenceValuesHelper.OAuth.ClientIdLabel] = config.ClientId,
                    [ReferenceValuesHelper.OAuth.ClientSecretLabel] = config.ClientSecret,
                    [ReferenceValuesHelper.OAuth.Scope] = String.Join(",", _config.Scopes.ToArray()),

                    [ReferenceValuesHelper.OAuth.GrantTypeLabel] = ReferenceValuesHelper.OAuth.RefreshTokenLabel,
                }
            );

            return token;
        }
        private string GenerateReportUrl(string url)
        {
            return _config.OwnerName + "/" + _config.AppName + "/report/" + url;
        }

        private string GenerateFormUrl(string form)
        {
            return _config.OwnerName + "/" + _config.AppName + "/form/" + form;
        }

        protected async Task<KDG.Connector.Models.Response<ReportResponse<A>>> _GetReport<A>(string report,int from, Dictionary<string,string?>? parameters)
        {
            var ps = parameters == null ? new Dictionary<string, string?>() : parameters;
            ps.Add("from", from.ToString());
            ps.Add("limit", ReportLimit.ToString());
            return await  Send<ReportResponse<A>>(HttpMethod.Get, GenerateReportUrl(report),new KDG.Connector.Models.ApiParams() {
                urlParams = ps
            });
        }

        protected async Task<IEnumerable<A>> _GetReportAll<A>(string report,int from, Dictionary<string, string?>? ps, string? token)
        {
            var parameters = ps == null ? new Dictionary<string, string?>() : ps;
            parameters.Add("max_records", ReportLimit.ToString());
            var response = await  Send<ReportResponse<A>>(HttpMethod.Get, GenerateReportUrl(report),new KDG.Connector.Models.ApiParams() {
                urlParams = parameters,
                headers = string.IsNullOrEmpty(token) ? null : new Dictionary<string, string>()
                {
                    { "record_cursor", token }
                }
            });
            var responseHeader = response.HttpResponseMessage.Headers.FirstOrDefault(x => x.Key == "record_cursor");
            if(responseHeader.Key == "record_cursor")
            {
                Console.WriteLine($"responseHeader.Value.First(): {responseHeader.Value.First()}");
                var t = await _GetReportAll<A>(report, from + ReportLimit,ps,responseHeader.Value.First());
                return response.Data.data.Concat(t);
            }
            return response.Data.data;
        }
        protected async Task<RecordResponse<T?>> GetRecordById<T>(string reportName, string id)
        {
            try
            {
                var url = GenerateReportUrl(reportName) + "/" + id;
                var response = await Send<RecordResponse<T?>>(HttpMethod.Get, url, new KDG.Connector.Models.ApiParams());
                return response.Data;
            }
            catch
            {
                return new RecordResponse<T?>(404, default(T?));
            }
        }
        #endregion
    }
}
