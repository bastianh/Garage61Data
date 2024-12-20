using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Garage61Data.Helpers
{
    public class OAuth2RedirectListener
    {
        private readonly HttpListener _httpListener;

        public OAuth2RedirectListener(string redirectUri)
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(redirectUri);
        }

        public async Task<OAuth2Response> WaitForRedirectAsync()
        {
            _httpListener.Start();

            try
            {
                var context = await _httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                var queryParams = request.QueryString;

                const string responseString = "<html><body>You can close that window now.</body></html>";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                _httpListener.Stop();

                return new OAuth2Response
                {
                    Code = queryParams["code"],
                    State = queryParams["state"]
                };
            }
            finally
            {
                _httpListener.Stop();
            }
        }

        public void Stop()
        {
            _httpListener.Stop();
        }

        #region Nested type: OAuth2Response

        public class OAuth2Response
        {
            public string Code { get; set; }
            public string State { get; set; }
        }

        #endregion
    }
}