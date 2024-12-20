using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Garage61Data.Helpers
{
    public class TokenStorage
    {
        private readonly string _fileName;

        public TokenStorage(string fileName)
        {
            _fileName = fileName;
        }

        public Token LoadToken()
        {
            if (!File.Exists(_fileName)) return null;

            try
            {
                // Load the encrypted token data from the file
                var encryptedData = File.ReadAllBytes(_fileName);

                // Decrypt the token data using DPAPI
                var decryptedData = ProtectedData.Unprotect(
                    encryptedData,
                    null, // Optional entropy, should match the one used for encryption
                    DataProtectionScope.CurrentUser
                );

                var tokenJson = Encoding.UTF8.GetString(decryptedData);

                // Deserialize the JSON data into a Token object
                return JsonConvert.DeserializeObject<Token>(tokenJson);
            }
            catch (Exception ex)
            {
                // Log or handle the error, if necessary
                Console.WriteLine($"Error loading tokens: {ex.Message}");
                return null;
            }
        }

        public void StoreTokens(string accessToken, string refreshToken)
        {
            if (refreshToken == null && File.Exists(_fileName))
            {
                File.Delete(_fileName);
                return;
            }

            var token = new Token
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            var tokenJson = JsonConvert.SerializeObject(token);

            // Encrypt the token data using DPAPI
            var encryptedData = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(tokenJson),
                null, // Optional entropy, can be replaced for additional security
                DataProtectionScope.CurrentUser
            );

            // Save the encrypted token data to a file
            File.WriteAllBytes(_fileName, encryptedData);
        }

        #region Nested type: Token

        public class Token
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
        }

        #endregion
    }
}