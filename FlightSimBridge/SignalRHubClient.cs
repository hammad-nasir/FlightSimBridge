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
        private SimConnectClient simConnectClient;
        public event Action<double> OnAltitudeReceived;
        private double throttleValue;

        private string connectionId;

        public SignalRHubClient(string hubUrl, string token)
        {
            string jwtToken = token.Trim('"');
            Console.WriteLine($"Token received: {jwtToken}");

            simConnectClient = new SimConnectClient(this);

            //connection = new HubConnectionBuilder()
            //.WithUrl(hubUrl, options =>
            //{
            //    options.AccessTokenProvider = () => Task.FromResult(jwtToken);
            //})
            //.WithAutomaticReconnect()
            //.Build();
            var connectionUrl = $"http://localhost:5233/flightsimhub?access_token={jwtToken}";

            connection = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.On<double, double>("ReceiveThrottle", (throttle1, throttle2) =>
            {
                Console.WriteLine($"Received Throttles on FlightSimBridge: {throttle1},  {throttle2}");
                simConnectClient.SendThrottle(throttle1, throttle2);

            });


            connection.On<bool>("ReceiveBrake", (brake) =>
            {
                Console.WriteLine($"Received Brake on FlightSimBridge: {brake}");
                simConnectClient.SendBrake(brake);

            });

            connection.On<double>("ReceiveFlap", (flap) =>
            {
                Console.WriteLine($"Received Flap on FlightSimBridge: {flap}");
                simConnectClient.SendFlap(flap);

            });

            connection.On<double>("ReceivePitch", (pitch) =>
            {
                Console.WriteLine($"Received Pitch on FlightSimBridge: {pitch}");
                simConnectClient.SendPitch(pitch);

            });

            connection.On<double>("ReceiveBank", (bank) =>
            {
                Console.WriteLine($"Received Pitch on FlightSimBridge: {bank}");
                simConnectClient.SendBank(bank);

            });

            connection.On<bool>("ReceiveAP", (ap) =>
            {
                simConnectClient.SetAutopilot(ap);

            });

            connection.On<AltHoldData>("ReceiveAltHold", (data) =>
            {
                simConnectClient.SetAutopilotAltHold(data);

            });

            connection.On<bool>("ReceiveSpeedHold", (ap) =>
            {
                simConnectClient.SetAutopilotSpeedHold(ap);

            });

            connection.On<bool>("ReceiveAprHold", (ap) =>
            {
                simConnectClient.SetAutopilotApprHold(ap);

            });

            connection.On<bool>("ReceiveAttHold", (ap) =>
            {
                simConnectClient.SetAutopilotAttHold(ap);

            });

            connection.On<bool>("ReceiveHdgHold", (ap) =>
            {
                simConnectClient.SetAutopilotHdgHold(ap);

            });

            connection.On<bool>("ReceiveVsHold", (ap) =>
            {
                simConnectClient.SetAutopilotVsHold(ap);

            });

            connection.On<bool>("ReceivePause", (pause) =>
            {
                simConnectClient.SetPauseState(pause);

            });


            connection.Closed += (exception) =>
            {
                Console.WriteLine($"SignalR hub connection closed: {exception?.Message}");
                return Task.CompletedTask;
            };
        }

        public async Task SendAltitudeAndSpeed(double altitude, double latitude, double longitude, double speed, double heading)
        {
            try
            {
                if (connection.State == HubConnectionState.Connected)
                {
                    // No need to send the connectionId since the JWT token is already in the request headers
                    await connection.SendAsync("SendAltitudeAndSpeed", altitude, latitude, longitude, speed, heading);
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