using DevOpsProject.Shared.Models;
using System.Text;
using System.Text.Json;

namespace DevOpsProject.Shared.Clients
{
    public class CommunicationControlHttpClient
    {
        private readonly HttpClient _httpClient;

        public CommunicationControlHttpClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendHiveControlCommandAsync(string scheme, string ip, int port, string path, MoveHiveMindCommand command)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = ip,
                Port = port,
                Path = $"{path}/command"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(command), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uriBuilder.Uri, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        public async Task<string> NotifyHiveMindAboutAddedInterference(string scheme, string ip, int port, string path, SetInterferenceToHiveCommand command)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = ip,
                Port = port,
                Path = $"{path}/interference"
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(command), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(uriBuilder.Uri, jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }

        public async Task<string> NotifyHiveMindAboutDeletedInterference(string scheme, string ip, int port, string path, Guid interferenceId)
        {
            var uriBuilder = new UriBuilder
            {
                Scheme = scheme,
                Host = ip,
                Port = port,
                Path = $"{path}/interference/{interferenceId}"
            };

            var response = await _httpClient.DeleteAsync(uriBuilder.Uri);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            return null;
        }
    }
}
