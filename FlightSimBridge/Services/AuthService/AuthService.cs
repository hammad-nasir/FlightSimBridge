using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FlightSimBridge.Services.AuthService
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;

        public AuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5233/"); // Replace with your FlightSimWebApp URL
        }

        public async Task<string> AuthenticateUser(string email, string password)
        {
            // Create a model to send the user's email and password for authentication
            var authModel = new
            {
                Email = email,
                Password = password
            };

            // Convert the model to JSON
            var jsonPayload = JsonConvert.SerializeObject(authModel);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Send a POST request to the FlightSimWebApp login endpoint
            var response = await _httpClient.PostAsync("api/auth/login", content);

            // Check if the response is successful
            if (response.IsSuccessStatusCode)
            {
                // Read the response content (JWT token)
                var token = await response.Content.ReadAsStringAsync();
                return token;
            }

            // Return null if authentication failed
            return null;
        }
    }
}
