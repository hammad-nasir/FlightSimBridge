using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlightSimBridge
{
    internal static class Program
    {
        private static string FlightSimWebAppUrl = "https://connectcockpit.com/";

        [STAThread]
        static async Task Main()
        {
        //    System.Net.ServicePointManager.ServerCertificateValidationCallback +=
        //(sender, cert, chain, sslPolicyErrors) => true;


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Authenticate the user and obtain the JWT token
            //var token = await AuthenticateUser("a@test.com", "123456");
            //if (token != null)
            //{
            //    // Start the FlightSimBridge and pass the token to Form1
            //    Application.Run(new Form1(token));
            //    //MessageBox.Show(token);
            //}
            //else
            //{
            //    MessageBox.Show("Authentication failed.");
            //}

            LoginForm loginForm = new LoginForm();
            Application.Run(loginForm);
        }

        private static async Task<string> AuthenticateUser(string email, string password)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(FlightSimWebAppUrl);

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
                var response = await httpClient.PostAsync("api/auth/login", content);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Read the response content (JWT token)
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseContent);
                    return tokenResponse.data;
                }

                // Return null if authentication failed
                return null;
            }
        }

        public class TokenResponse
        {
            public string data { get; set; }
            public bool success { get; set; }
            public string message { get; set; }
  
        }
    }
}
