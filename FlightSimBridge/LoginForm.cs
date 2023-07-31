using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlightSimBridge
{
    public partial class LoginForm : Form
    {
        private static string FlightSimWebAppUrl = "http://localhost:5233/";
        public LoginForm()
        {
            InitializeComponent();

        }

        private async void loginBtn_Click(object sender, EventArgs e)
        {
            string email = emailTxt.Text;
            string password = passwordTxt.Text;
            bool rememberMe = rememberMeChk.Checked;

            var token = await AuthenticateUser(email, password);

            if (token != null)
            {
                this.Hide();
                Form1 form1 = new Form1(token);
                form1.Closed += (s, args) => this.Close();
                form1.Show();
            }
            else
            {
                MessageBox.Show("Invalid email or password. Please try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

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

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://localhost:5233/register");
        }

        private void registerBtn_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://localhost:5233/register");
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        private void exitBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
