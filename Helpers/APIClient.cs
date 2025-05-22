using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Garage61Data.Exceptions;
using Garage61Data.Models;
using Newtonsoft.Json;
using SimHub;
using SimHub.Plugins;

namespace Garage61Data.Helpers
{
    public class ApiClient
    {
        private readonly string _authEndpoint = "https://garage61.net/app/account/oauth";

        private readonly string _clientId = BuildConstants.GARAGE61_CLIENT_ID;
        private readonly string _garage61Api = "https://garage61.net/";
        private readonly HttpClient _httpClient;
        private readonly string _redirectUri = "http://localhost:8037/";
        private readonly string _tokenEndpoint = "https://garage61.net/api/oauth/token";
        private readonly TokenStorage _tokenStorage;

        private string _accessToken;
        private string _refreshToken;

        public void Logout()
        {
            _accessToken = null;
            _refreshToken = null;
            _tokenStorage.StoreTokens(null, null);
        }

        #region ApiClient

        public ApiClient(IPlugin plugin)
        {
            _tokenStorage =
                new TokenStorage(PluginManager.GetInstance().GetCommonStoragePath(plugin.GetType().Name + ".token"));
            var token = _tokenStorage.LoadToken();
            if (token != null)
            {
                _accessToken = token.AccessToken;
                _refreshToken = token.RefreshToken;
            }

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_garage61Api)
            };
        }

        private async Task<T> GetAsync<T>(string url, Dictionary<string, string> urlParameters = null,
            CancellationToken cancellationToken = default)
        {
            if (urlParameters != null && urlParameters.Count > 0)
            {
                var encodedParameters = await new FormUrlEncodedContent(urlParameters).ReadAsStringAsync().ConfigureAwait(false);
                url = $"{url}?{encodedParameters}";
            }

            return await SendWithRetryAsync<T>(ct => CreateRequest(HttpMethod.Get, url, null, ct), cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> PostAsync<T>(string url, object body, CancellationToken cancellationToken = default)
        {
            return await SendWithRetryAsync<T>(ct => CreateRequest(HttpMethod.Post, url, body, ct), cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> SendWithRetryAsync<T>(Func<CancellationToken, Task<HttpRequestMessage>> requestBuilder,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_refreshToken)) throw new ApiClientException("Refresh token is missing or null.");

            var tokenRefreshed = false;
            do
            {
                try
                {
                    var request = await requestBuilder(cancellationToken).ConfigureAwait(false);

                    var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        // Process successful response
                        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return JsonConvert.DeserializeObject<T>(json);
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized && tokenRefreshed == false)
                    {
                        _accessToken = null;
                        await RefreshToken(cancellationToken).ConfigureAwait(false);
                        tokenRefreshed = true;
                        continue;
                    }

                    response.EnsureSuccessStatusCode(); // For other errors
                }
                catch (Exception ex)
                {
                    Logging.Current.Error($"Garage61Data: error sending request: {ex.Message}");
                    throw new ApiClientException($"Request failed {ex.Message}");
                }
            } while (tokenRefreshed);

            throw new ApiClientException("Request failed");
        }

        private async Task<HttpRequestMessage> CreateRequest(HttpMethod method, string url, object body = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_accessToken)) await RefreshToken(cancellationToken).ConfigureAwait(false);

            var request = new HttpRequestMessage(method, url)
            {
                Content = body != null
                    ? new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
                    : null
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return request;
        }

        #endregion


        #region OAuth

        public async Task StartOAuthFlow(CancellationToken cancellationToken = default)
        {
            var redirectListener = new OAuth2RedirectListener(_redirectUri);

            var state = Guid.NewGuid().ToString("N");
            var url =
                $"{_authEndpoint}?client_id={_clientId}&redirect_uri={_redirectUri}&response_type=code&state={state}&scope=driving_data";

            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                throw new OAuth2Exception($"could not open browser msg: {ex.Message}");
            }

            var result = await redirectListener.WaitForRedirectAsync();

            if (result.State != state) throw new OAuth2Exception("Invalid State");

            await GetTokenAsync(result.Code, cancellationToken).ConfigureAwait(false);
        }


        private async Task GetTokenAsync(string authCode, CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient();

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", authCode },
                { "redirect_uri", _redirectUri },
                { "client_id", _clientId }
            };

            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await httpClient.PostAsync(_tokenEndpoint, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    // Error handling
                    var errorDetails = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new OAuth2Exception(
                        $"Token request failed: {response.StatusCode}, {errorDetails}");
                }

                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);

                _accessToken = tokenResponse.AccessToken;
                _refreshToken = tokenResponse.RefreshToken;

                _tokenStorage.StoreTokens(_accessToken, _refreshToken);
            }
            catch (Exception ex)
            {
                throw new OAuth2Exception($"Error during token retrieval: {ex.Message}");
            }
        }


        private async Task RefreshToken(CancellationToken cancellationToken = default)
        {
            using var httpClient = new HttpClient();


            if (string.IsNullOrEmpty(_refreshToken)) throw new OAuth2Exception("Refresh token is missing or null.");

            var parameters = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", _refreshToken },
                { "client_id", _clientId }
            };

            var content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await httpClient.PostAsync(_tokenEndpoint, content, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    throw new OAuth2Exception($"Failed to refresh token: {response.StatusCode}, {errorDetails}");
                }

                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);

                _accessToken = tokenResponse.AccessToken;
                _refreshToken = tokenResponse.RefreshToken;
                _tokenStorage.StoreTokens(_accessToken, _refreshToken);
            }
            catch (Exception ex)
            {
                throw new OAuth2Exception($"error refreshing token: {ex.Message}");
            }
        }

        #endregion


        #region Api Methods

        public async Task<UserInfo> GetMe(CancellationToken cancellationToken = default)
        {
            var userInfo = await GetAsync<UserInfo>("api/v1/me", null, cancellationToken).ConfigureAwait(false);

            Logging.Current.Info($"Garage61Data: updated userdata from garage61 {userInfo.Slug}");

            return userInfo;
        }

        public async Task<List<Garage61Lap>> GetLaps(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
        {
            var laps = await GetAsync<Garage61ListRequest<Garage61Lap>>("api/v1/laps", parameters, cancellationToken).ConfigureAwait(false);

            Logging.Current.Info($"Garage61Data: fetched laps data (Count: {laps.Total})");

            return laps.Items;
        }

        public async Task<List<Garage61PlatformCar>> GetCars(CancellationToken cancellationToken = default)
        {
            var cars = await GetAsync<Garage61ListRequest<Garage61PlatformCar>>("api/v1/cars", null, cancellationToken).ConfigureAwait(false);
            Logging.Current.Info($"Garage61Data: fetched available cars (Count: {cars.Total})");
            return cars.Items;
        }

        public async Task<List<Garage61PlatformTrack>> GetTracks(CancellationToken cancellationToken = default)
        {
            var tracks = await GetAsync<Garage61ListRequest<Garage61PlatformTrack>>("api/v1/tracks", null, cancellationToken).ConfigureAwait(false);
            Logging.Current.Info($"Garage61Data: fetched available tracks (Count: {tracks.Total})");
            return tracks.Items;
        }

        #endregion
    }
}