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

        public async Task<Garage61Lap> GetLap(string lapId)
        {
            return await GetAsync<Garage61Lap>("api/v1/laps/" + lapId);
        }

        // Speed,LapDistPct,Lat,Lon,Brake,Throttle,RPM,SteeringWheelAngle,Gear,Clutch,ABSActive,DRSActive,LatAccel,LongAccel,VertAccel,Yaw,PositionType
        // 52.334805,0.00021398377,51.20764100644781,-1.6090673372733106,0,1,7412.37,-0.124688394,3,1,false,false,-5.3913393,2.6497703,9.028612,2.022161,3

        public async Task<List<Garage61TelemetryRow>> GetLapTelemetry(string lapId)
        {
            return await GetAsyncCsv("api/v1/laps/" + lapId + "/csv", columns => new Garage61TelemetryRow
            {
                Speed = double.Parse(columns[0].Trim()),
                LapDistPct = double.Parse(columns[1].Trim()),
                Lat = double.Parse(columns[2].Trim()),
                Lon = double.Parse(columns[3].Trim()),
                Brake = double.Parse(columns[4].Trim()),
                Throttle = double.Parse(columns[5].Trim()),
                RPM = double.Parse(columns[6].Trim()),
                SteeringWheelAngle = double.Parse(columns[7].Trim()),
                Gear = int.Parse(columns[8].Trim()),
                Clutch = double.Parse(columns[9].Trim()),
                ABSActive = bool.Parse(columns[10].Trim()),
                DRSActive = bool.Parse(columns[11].Trim()),
                LatAccel = double.Parse(columns[12].Trim()),
                LongAccel = double.Parse(columns[13].Trim()),
                VertAccel = double.Parse(columns[14].Trim()),
                Yaw = double.Parse(columns[15].Trim()),
                PositionType = int.Parse(columns[16].Trim())
            });
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
                var encodedParameters = new FormUrlEncodedContent(urlParameters).ReadAsStringAsync().Result;
                url = $"{url}?{encodedParameters}";
            }

            return await SendWithRetryAsync<T>(() => CreateRequest(HttpMethod.Get, url), cancellationToken);
        }

        private async Task<List<T>> GetAsyncCsv<T>(string url, Func<string[], T> mapFunction,
            Dictionary<string, string> urlParameters = null,
            CancellationToken cancellationToken = default)
        {
            if (urlParameters != null && urlParameters.Count > 0)
            {
                var encodedParameters = new FormUrlEncodedContent(urlParameters).ReadAsStringAsync().Result;
                url = $"{url}?{encodedParameters}";
            }

            return await SendWithRetryAsyncCsv(() => CreateRequest(HttpMethod.Get, url), cancellationToken,
                mapFunction);
        }

        private async Task<T> PostAsync<T>(string url, object body, CancellationToken cancellationToken = default)
        {
            return await SendWithRetryAsync<T>(() => CreateRequest(HttpMethod.Post, url, body), cancellationToken);
        }

        private async Task<T> SendWithRetryAsync<T>(Func<Task<HttpRequestMessage>> requestBuilder,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_refreshToken)) throw new ApiClientException("Refresh token is missing or null.");

            var tokenRefreshed = false;
            do
            {
                try
                {
                    var request = await requestBuilder();

                    var response = await _httpClient.SendAsync(request, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        // Prozessiere erfolgreiche Antwort
                        var json = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(json);
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized && tokenRefreshed == false)
                    {
                        _accessToken = null;
                        await RefreshToken();
                        tokenRefreshed = true;
                        continue;
                    }

                    response.EnsureSuccessStatusCode(); // Für andere Fehler
                }
                catch (Exception ex)
                {
                    Logging.Current.Error($"Garage61Data: error sending request: {ex.Message}");
                    throw new ApiClientException($"Request failed {ex.Message}");
                }
            } while (tokenRefreshed);

            throw new ApiClientException("Request failed");
        }

        private async Task<List<T>> SendWithRetryAsyncCsv<T>(Func<Task<HttpRequestMessage>> requestBuilder,
            CancellationToken cancellationToken, Func<string[], T> mapFunction)
        {
            if (string.IsNullOrEmpty(_refreshToken)) throw new ApiClientException("Refresh token is missing or null.");

            var tokenRefreshed = false;
            do
            {
                try
                {
                    var request = await requestBuilder();
                    var response = await _httpClient.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        // Prozessiere erfolgreiche Antwort
                        var json = await response.Content.ReadAsStringAsync();
                        return CsvParser.ParseCsvFromString(json, mapFunction);
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized && tokenRefreshed == false)
                    {
                        _accessToken = null;
                        await RefreshToken();
                        tokenRefreshed = true;
                        continue;
                    }

                    response.EnsureSuccessStatusCode(); // Für andere Fehler
                }
                catch (Exception ex)
                {
                    Logging.Current.Error($"Garage61Data: error sending request: {ex.Message}");
                    throw new ApiClientException($"Request failed {ex.Message}");
                }
            } while (tokenRefreshed);

            throw new ApiClientException("Request failed");
        }

        private async Task<HttpRequestMessage> CreateRequest(HttpMethod method, string url, object body = null)
        {
            if (string.IsNullOrEmpty(_accessToken)) await RefreshToken();

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

        public async Task StartOAuthFlow()
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

            await GetTokenAsync(result.Code);
        }


        private async Task GetTokenAsync(string authCode)
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
                var response = await httpClient.PostAsync(_tokenEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    // Fehlerbehandlung
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    throw new OAuth2Exception(
                        $"Tokenanfrage fehlgeschlagen: {response.StatusCode}, {errorDetails}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);

                _accessToken = tokenResponse.AccessToken;
                _refreshToken = tokenResponse.RefreshToken;

                _tokenStorage.StoreTokens(_accessToken, _refreshToken);
            }
            catch (Exception ex)
            {
                throw new OAuth2Exception($"Fehler bei der Tokenabholung: {ex.Message}");
            }
        }


        private async Task RefreshToken()
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
                var response = await httpClient.PostAsync(_tokenEndpoint, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    throw new OAuth2Exception($"Failed to refresh token: {response.StatusCode}, {errorDetails}");
                }

                var responseString = await response.Content.ReadAsStringAsync();
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

        public async Task<UserInfo> GetMe()
        {
            var userInfo = await GetAsync<UserInfo>("api/v1/me");

            Logging.Current.Info($"Garage61Data: updated userdata from garage61 {userInfo.Slug}");

            return userInfo;
        }

        public async Task<List<Garage61Lap>> GetLaps(Dictionary<string, string> parameters)
        {
            var laps = await GetAsync<Garage61ListRequest<Garage61Lap>>("api/v1/laps", parameters);

            Logging.Current.Info($"Garage61Data: fetched laps data (Count: {laps.Total})");

            return laps.Items;
        }

        public async Task<List<Garage61PlatformCar>> GetCars()
        {
            var cars = await GetAsync<Garage61ListRequest<Garage61PlatformCar>>("api/v1/cars");
            Logging.Current.Info($"Garage61Data: fetched available cars (Count: {cars.Total})");
            return cars.Items;
        }

        public async Task<List<Garage61PlatformTrack>> GetTracks()
        {
            var tracks = await GetAsync<Garage61ListRequest<Garage61PlatformTrack>>("api/v1/tracks");
            Logging.Current.Info($"Garage61Data: fetched available tracks (Count: {tracks.Total})");
            return tracks.Items;
        }

        #endregion
    }
}