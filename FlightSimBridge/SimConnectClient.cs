using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using Newtonsoft.Json;

namespace FlightSimBridge
{
    public enum DEFINITIONS
    {
        Struct1,
        ThrottleData
    }

    public enum DATA_REQUESTS
    {
        REQUEST_PLANE_INFO,
        REQUEST_THROTTLE_DATA
    }

    public enum MyGroups
    {
        GROUP0
    }

    public enum MyEvents
    {
        PAUSE_SET
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct PlaneInfo
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double Speed;
    }

    public struct ThrottleData
    {
        public double Throttle;
    }


    public class SimConnectClient
    {
        private const int WM_USER_SIMCONNECT = 0x0402;
        private SimConnect simconnect = null;
        private readonly SignalRHubClient signalRClient;
        private TaskCompletionSource<PlaneInfo> tcs = new TaskCompletionSource<PlaneInfo>();

        public event Action<PlaneInfo> PlaneInfoUpdated;

        public SimConnectClient(SignalRHubClient signalRClient)
        {
            this.signalRClient = signalRClient;

            try
            {
                InitializeSimConnect();
                SetPauseState(false);
                new Thread(ListenerThread).Start();
            }
            catch (COMException ex)
            {
                Console.WriteLine("Unable to connect to SimConnect: " + ex.Message);
            }
        }

        private void InitializeSimConnect()
        {
            simconnect = new SimConnect("Managed Data Request", IntPtr.Zero, WM_USER_SIMCONNECT, null, 0);
            simconnect.OnRecvOpen += Simconnect_OnRecvOpen;
            simconnect.OnRecvQuit += Simconnect_OnRecvQuit;
            simconnect.OnRecvSimobjectDataBytype += Simconnect_OnRecvSimobjectDataBytype;
            //simconnect.OnRecvEvent += Simconnect_OnRecvEvent;

            RegisterSimConnectDefinitions();
        }

        private void RegisterSimConnectDefinitions()
        {
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE ALTITUDE", "meters", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AIRSPEED INDICATED", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            simconnect.RegisterDataDefineStruct<PlaneInfo>(DEFINITIONS.Struct1);

            simconnect.AddToDataDefinition(DEFINITIONS.ThrottleData, "GENERAL ENG THROTTLE LEVER POSITION:1", "percent over 100", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<ThrottleData>(DEFINITIONS.ThrottleData);

            simconnect.MapClientEventToSimEvent(MyEvents.PAUSE_SET, "PAUSE_SET");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.PAUSE_SET, false);




        }

        private void ListenerThread()
        {
            while (true)
            {
                simconnect?.ReceiveMessage();
                RequestPlaneInfo();
                PlaneInfo planeInfo = WaitForPlaneInfoUpdate();
                signalRClient.SendAltitudeAndSpeed(planeInfo.Altitude, planeInfo.Latitude, planeInfo.Longitude, planeInfo.Speed);

                Thread.Sleep(1000);
            }
        }

        public void RequestPlaneInfo()
        {
            simconnect?.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_PLANE_INFO, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
        }

        public void SendThrottle(double throttle)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.ThrottleData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new ThrottleData { Throttle = throttle });
        }

        public void SetPauseState(bool pause)
        {
            try
            {
                uint pauseValue = pause ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.PAUSE_SET, pauseValue, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.DEFAULT);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }


        private PlaneInfo WaitForPlaneInfoUpdate()
        {
            if (tcs.Task.Wait(TimeSpan.FromSeconds(2)))
            {
                return tcs.Task.Result;
            }
            else
            {
                Console.WriteLine("Timeout while waiting for PlaneInfoUpdated event.");
                return new PlaneInfo();
            }
        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("Connected to Flight Simulator");
            RequestPlaneInfo();
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("Flight Simulator has exited");
            simconnect = null;
        }

        private void Simconnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {
            try
            {
                if (data.dwRequestID == (int)DATA_REQUESTS.REQUEST_PLANE_INFO)
                {
                    PlaneInfo receivedData = (PlaneInfo)data.dwData[0];
                    Console.WriteLine($"Plane Info - Latitude: {receivedData.Latitude}, Longitude: {receivedData.Longitude}, Altitude: {receivedData.Altitude}");
                    PlaneInfoUpdated?.Invoke(receivedData);
                }
                else
                {
                    Console.WriteLine($"Received unknown data of type {data.dwData[0].GetType()} with unknown request ID {data.dwRequestID}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occurred in Simconnect_OnRecvSimobjectDataBytype: ");
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("StackTrace: " + ex.StackTrace);
                if (ex.InnerException != null)
                {
                    Console.WriteLine("InnerException Message : " + ex.InnerException.Message);
                }
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