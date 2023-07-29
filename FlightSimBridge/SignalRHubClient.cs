using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightSimBridge
{
    public class SignalRHubClient
    {
        private readonly HubConnection connection;
        public event Action<double> OnAltitudeReceived;

        private string connectionId;

        public SignalRHubClient(string hubUrl, string token)
        {
            string jwtToken = token.Trim('"');
            Console.WriteLine($"Token received: {jwtToken}");
            connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(jwtToken);
            })
            .WithAutomaticReconnect()
            .Build();
         
            connection.Closed += (exception) =>
            {
                Console.WriteLine($"SignalR hub connection closed: {exception?.Message}");
                return Task.CompletedTask;
            };
        }

        public async Task SendAltitudeAndSpeed(double altitude)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    // No need to send the connectionId since the JWT token is already in the request headers
                    await connection.SendAsync("SendAltitudeAndSpeed", altitude);
                    Console.WriteLine($"Altitude sent to hub: {altitude}");

                    Console.WriteLine("SignalR hub is connected.");
                }
                else
                {
                    Console.WriteLine("SignalR hub connection is not active.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending altitude to hub: {ex.Message}");
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connected to SignalR hub.");

                connectionId = connection.ConnectionId;
                Console.WriteLine($"Connection ID: {connectionId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to SignalR hub: {ex.Message}");
            }
        }
    }

}