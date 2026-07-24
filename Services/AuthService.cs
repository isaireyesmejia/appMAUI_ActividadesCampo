using System.Text;
using System.Text.Json;
using agaverosActividades.Models;
using agaverosActividades.Constants;

namespace agaverosActividades.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<LogeoModel?> LoginAsync(string usuario, string password)
        {
            var loginModel = new LoginModel
            {
                VchLogin = usuario,
                VchPassword = password
            };

            string json = JsonSerializer.Serialize(loginModel, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(ApiEndpoints.Login, content);
            }
            catch (HttpRequestException)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();

            try
            {
                var resultados = JsonSerializer.Deserialize<List<LogeoModel>>(body, _jsonOptions);
                return resultados?.Count > 0 ? resultados[0] : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}