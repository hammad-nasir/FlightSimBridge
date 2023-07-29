using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using Newtonsoft.Json;

namespace FlightSimBridge
{
    public enum DEFINITIONS
    {
        Struct1,
    }

    public enum DATA_REQUESTS
    {
        REQUEST_PLANE_INFO,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct PlaneInfo
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
    }

    public class SimConnectClient
    {
        private const int WM_USER_SIMCONNECT = 0x0402;
        private SimConnect simconnect = null;
        private readonly SignalRHubClient signalRClient;

        public event Action<PlaneInfo> PlaneInfoUpdated; // Event to notify about updates / pub/sub events

        public SimConnectClient(SignalRHubClient signalRClient)
        {
            this.signalRClient = signalRClient;

            try
            {
                simconnect = new SimConnect("Managed Data Request", IntPtr.Zero, WM_USER_SIMCONNECT, null, 0);

                //This line adds an event handler to the OnRecvOpen event of the SimConnect instance.
                //The OnRecvOpen event is triggered when the client application successfully connects to Flight Simulator.
                simconnect.OnRecvOpen += Simconnect_OnRecvOpen;

                simconnect.OnRecvQuit += Simconnect_OnRecvQuit;

                //This line adds an event handler to the OnRecvSimobjectDataBytype event of the SimConnect instance.
                //The OnRecvSimobjectDataBytype event is triggered when requested data is received from Flight Simulator.
                simconnect.OnRecvSimobjectDataBytype += Simconnect_OnRecvSimobjectDataBytype;

                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE ALTITUDE", "meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.RegisterDataDefineStruct<PlaneInfo>(DEFINITIONS.Struct1);

                RequestPlaneInfo();
            }
            catch (COMException ex)
            {
                Console.WriteLine("Unable to connect to SimConnect: " + ex.Message);
                return;
            }
            new Thread(ListenerThread).Start();
        }

        public void RequestPlaneInfo()
        {
            Console.WriteLine("Requesting plane info...");
            simconnect?.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_PLANE_INFO, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
            Console.WriteLine("Requested plane info.");
        }

        private void ListenerThread()
        {
            while (true)
            {
                simconnect?.ReceiveMessage();
                RequestPlaneInfo(); // Move the request inside the loop

                // Wait for the PlaneInfoUpdated event to be raised with the latest data
                // This event will be triggered by Simconnect_OnRecvSimobjectDataBytype method
                // after receiving the requested data from Flight Simulator.
                // The PlaneInfoUpdated event should have the updated latitude, longitude, and altitude values.
                PlaneInfo planeInfo = WaitForPlaneInfoUpdate();

                // Notify the SignalRHubClient with the new data
                signalRClient.SendAltitudeAndSpeed(planeInfo.Altitude);

                Thread.Sleep(1000); // Request every second
            }
        }


        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("Connected to Flight Simulator");
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("Flight Simulator has exited");
            simconnect = null;
        }

        private void Simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            Console.WriteLine($"Received sim object data: {JsonConvert.SerializeObject(data)}");

            if (data.dwRequestID == (int)DATA_REQUESTS.REQUEST_PLANE_INFO)
            {
                PlaneInfo receivedData = (PlaneInfo)data.dwData[0];
                Console.WriteLine($"Plane Info - Latitude: {receivedData.Latitude}, Longitude: {receivedData.Longitude}, Altitude: {receivedData.Altitude}");

                if (PlaneInfoUpdated != null)
                {
                    Console.WriteLine("PlaneInfoUpdated event is not null, invoking...");
                    PlaneInfoUpdated?.Invoke(receivedData); // Invoke the event when new data arrives
                }
                else
                {
                    Console.WriteLine("PlaneInfoUpdated event is null.");
                }
            }
            else
            {
                Console.WriteLine("Unknown request ID: " + data.dwRequestID);
            }
        }


        private double GetLatitudeFromSimConnect()
        {
            try
            {
                // Request the latitude data from SimConnect
                simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_PLANE_INFO, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                // Wait for the PlaneInfoUpdated event to be raised with the latest data
                // This event will be triggered by SimConnect_OnRecvSimobjectDataBytype method
                // after receiving the requested data from Flight Simulator.
                // The PlaneInfoUpdated event should have the latitude value.
                // For example, if PlaneInfoUpdated is of type PlaneInfo:
                PlaneInfo planeInfo = WaitForPlaneInfoUpdate();

                // Return the latitude value
                return planeInfo.Latitude;
            }
            catch (COMException ex)
            {
                Console.WriteLine("Error getting latitude from SimConnect: " + ex.Message);
                return 0.0; // Or any default value based on your needs
            }
        }

        private double GetLongitudeFromSimConnect()
        {
            try
            {
                // Request the longitude data from SimConnect
                simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_PLANE_INFO, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                // Wait for the PlaneInfoUpdated event to be raised with the latest data
                // This event will be triggered by SimConnect_OnRecvSimobjectDataBytype method
                // after receiving the requested data from Flight Simulator.
                // The PlaneInfoUpdated event should have the longitude value.
                // For example, if PlaneInfoUpdated is of type PlaneInfo:
                PlaneInfo planeInfo = WaitForPlaneInfoUpdate();

                // Return the longitude value
                return planeInfo.Longitude;
            }
            catch (COMException ex)
            {
                Console.WriteLine("Error getting longitude from SimConnect: " + ex.Message);
                return 0.0; // Or any default value based on your needs
            }
        }

        private double GetAltitudeFromSimConnect()
        {
            try
            {
                // Request the altitude data from SimConnect
                simconnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_PLANE_INFO, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);

                // Wait for the PlaneInfoUpdated event to be raised with the latest data
                // This event will be triggered by SimConnect_OnRecvSimobjectDataBytype method
                // after receiving the requested data from Flight Simulator.
                // The PlaneInfoUpdated event should have the altitude value.
                // For example, if PlaneInfoUpdated is of type PlaneInfo:
                PlaneInfo planeInfo = WaitForPlaneInfoUpdate();

                // Return the altitude value
                return planeInfo.Altitude;
            }
            catch (COMException ex)
            {
                Console.WriteLine("Error getting altitude from SimConnect: " + ex.Message);
                return 0.0; // Or any default value based on your needs
            }
        }

        private TaskCompletionSource<PlaneInfo> tcs = new TaskCompletionSource<PlaneInfo>();

        private PlaneInfo WaitForPlaneInfoUpdate()
        {
            // This method will wait for the PlaneInfoUpdated event to be raised.
            // We'll use a TaskCompletionSource to handle the synchronization.

            // TaskCompletionSource will be set when the PlaneInfoUpdated event is raised,
            // and the PlaneInfo data will be available in tcs.Task.Result.
            // The task will complete and return the PlaneInfo data to the caller.

            try
            {
                // Wait until the PlaneInfoUpdated event is triggered and the task is completed.
                // Timeout after 5 seconds (adjust this based on your specific scenario).
                if (tcs.Task.Wait(TimeSpan.FromSeconds(5)))
                {
                    // The task completed successfully within the timeout.
                    return tcs.Task.Result;
                }
                else
                {
                    // The task did not complete within the timeout.
                    // You can handle this case accordingly, e.g., throw an exception or return default values.
                    Console.WriteLine("Timeout while waiting for PlaneInfoUpdated event.");
                    return new PlaneInfo();
                }
            }
            catch (AggregateException ex)
            {
                // Handle any exceptions that might have occurred during the waiting process.
                // You can handle this case accordingly, e.g., throw an exception or return default values.
                Console.WriteLine("Error while waiting for PlaneInfoUpdated event: " + ex.Message);
                return new PlaneInfo();
            }
        }





        ~SimConnectClient()
        {
            simconnect?.Dispose();
        }
    }
}


//This class first defines the structure of the data we want from Flight Simulator.
//In this case, we are only interested in latitude, longitude, and altitude, all of which are 64-bit floats (FLOAT64).
//We register this structure with SimConnect by calling simconnect.AddToDataDefinition for each field, then simconnect.RegisterDataDefineStruct for the structure as a whole.

//The RequestPlaneInfo method requests the data we just defined.
//The Simconnect_OnRecvSimobjectDataBytype handler will be called whenever the requested data is available.