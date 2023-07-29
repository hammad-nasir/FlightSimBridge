using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlightSimBridge
{
    public partial class Form1 : Form
    {
        private SimConnectClient simConnectClient;
        private SignalRHubClient signalRClient;

        private readonly string _token;

        public Form1(string token)
        {
            InitializeComponent();
            _token = token;

            // Initialize the SignalR client with the hub URL
            signalRClient = new SignalRHubClient("http://localhost:5233/flightsimhub", _token);

            // Initialize the SimConnect client
            simConnectClient = new SimConnectClient(signalRClient);

            // Subscribe to the PlaneInfoUpdated event to receive plane information
            simConnectClient.PlaneInfoUpdated += SimConnectClient_PlaneInfoUpdated;
        }

        private void SimConnectClient_PlaneInfoUpdated(PlaneInfo planeInfo)
        {
            // Update the UI with the received plane information
            // For example, updating the labels with latitude, longitude, and altitude
            Invoke(new Action(() =>
            {
                //latitudeLabel.Text = $"Latitude: {planeInfo.Latitude} degrees";
                //longitudeLabel.Text = $"Longitude: {planeInfo.Longitude} degrees";
                altitudeLabel.Text = $"Altitude: {planeInfo.Altitude} meters";

                signalRClient.SendAltitudeAndSpeed(planeInfo.Altitude);
            }));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Stop the SignalR client when the form is closing
            //signalRClient.Stop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            signalRClient.ConnectAsync();
        }
    }
}