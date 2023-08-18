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
        ThrottleData,
        BrakeData,
        FlapData,
        PlanePitchData,
        PlaneBankData,
        AutopilotData,
        PlaneTargetAltitudeData,
        PlaneTargetSpeedData,
        PlaneTargetHeadingData,
        PlaneTargetVSData
    }

    public enum DATA_REQUESTS
    {
        REQUEST_PLANE_INFO,
        REQUEST_THROTTLE_DATA,

    }

    public enum MyGroups
    {
        GROUP0
    }

    public enum MyEvents
    {
        PAUSE_SET,
        PARKING_BRAKES,
        AP_SET,
        AP_ALT_HOLD,
        AP_AIRSPEED_HOLD,
        AP_APR_HOLD,
        AP_ATT_HOLD,
        AP_HDG_HOLD,
        AP_SPD_VAR_SET

    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    public struct PlaneInfo
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double Speed;
        public double Heading;
        public double ElapsedSeconds;
        public int AutoPilot;
    }

    public struct ThrottleData
    {
        public double Throttle1;
        public double Throttle2;
    }

    public struct BrakeData
    {
        public double BrakeRightPosition;
        public double BrakeLeftPosition;
    }

    public struct FlapData
    {
        public double Flap;
    }

    public struct PlanePitchData
    {
        public double PlanePitchDegrees;
    }

    public struct PlaneBankData
    {
        public double PlaneBankDegrees;
    }

    public struct PlaneTargetAltitudeData
    {
        public double PlaneTargetAltitude;
    }

    public struct PlaneTargetSpeedData
    {
        public double PlaneTargetSpeed;
    }

    public struct PlaneTargetHeadingData
    {
        public double PlaneTargetHeading;
    }

    public struct PlaneTargetVSData
    {
        public double PlaneTargetVS;
    }

    public struct AutopilotData 
    {
        public int Autopilot;
    }


    public class SimConnectClient
    {
        private const int WM_USER_SIMCONNECT = 0x0402;
        private SimConnect simconnect = null;
        private readonly SignalRHubClient signalRClient;
        private TaskCompletionSource<PlaneInfo> tcs = new TaskCompletionSource<PlaneInfo>();

        private double previousElapsedSeconds = -1;

        public event Action<PlaneInfo> PlaneInfoUpdated;

        public SimConnectClient(SignalRHubClient signalRClient)
        {
            this.signalRClient = signalRClient;

            try
            {
                InitializeSimConnect();
                SetAutopilot(false);
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
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ELAPSED_SECONDS", "seconds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT MASTER", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);

            simconnect.RegisterDataDefineStruct<PlaneInfo>(DEFINITIONS.Struct1);

            simconnect.AddToDataDefinition(DEFINITIONS.ThrottleData, "GENERAL ENG THROTTLE LEVER POSITION:1", "percent over 100", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.ThrottleData, "GENERAL ENG THROTTLE LEVER POSITION:2", "percent over 100", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            //simconnect.AddToDataDefinition(DEFINITIONS.ThrottleData, "TURB ENG JET THRUST:1", "pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            //simconnect.AddToDataDefinition(DEFINITIONS.ThrottleData, "TURB ENG JET THRUST:2", "pounds", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<ThrottleData>(DEFINITIONS.ThrottleData);
            simconnect.AddToDataDefinition(DEFINITIONS.BrakeData, "BRAKE RIGHT POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.AddToDataDefinition(DEFINITIONS.BrakeData, "BRAKE LEFT POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            //simconnect.AddToDataDefinition(DEFINITIONS.BrakeData, "SPOILERS ARMED", "bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<BrakeData>(DEFINITIONS.BrakeData);
            simconnect.AddToDataDefinition(DEFINITIONS.FlapData, "FLAPS HANDLE PERCENT", "percent over 100", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<FlapData>(DEFINITIONS.FlapData);
            simconnect.AddToDataDefinition(DEFINITIONS.PlanePitchData, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<PlanePitchData>(DEFINITIONS.PlanePitchData);
            simconnect.AddToDataDefinition(DEFINITIONS.PlaneBankData, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<PlaneBankData>(DEFINITIONS.PlaneBankData);

            simconnect.AddToDataDefinition(DEFINITIONS.PlaneTargetAltitudeData, "AUTOPILOT ALTITUDE LOCK VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<PlaneTargetAltitudeData>(DEFINITIONS.PlaneTargetAltitudeData);

            simconnect.AddToDataDefinition(DEFINITIONS.PlaneTargetSpeedData, "AUTOPILOT AIRSPEED HOLD VAR", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<PlaneTargetSpeedData>(DEFINITIONS.PlaneTargetSpeedData);

            simconnect.AddToDataDefinition(DEFINITIONS.PlaneTargetHeadingData, "AUTOPILOT HEADING LOCK DIR", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<PlaneTargetHeadingData>(DEFINITIONS.PlaneTargetHeadingData);

            simconnect.AddToDataDefinition(DEFINITIONS.PlaneTargetVSData, "AUTOPILOT VERTICAL HOLD VAR", "feet/minute", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect.RegisterDataDefineStruct<PlaneTargetVSData>(DEFINITIONS.PlaneTargetVSData);

            //simconnect.AddToDataDefinition(DEFINITIONS.AutopilotData, "AUTOPILOT MASTER", "Bool", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            //simconnect.RegisterDataDefineStruct<AutopilotData>(DEFINITIONS.AutopilotData);


            simconnect.MapClientEventToSimEvent(MyEvents.AP_SET, "AP_MASTER");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_SET, false);

            simconnect.MapClientEventToSimEvent(MyEvents.AP_ALT_HOLD, "AP_ALT_HOLD");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_ALT_HOLD, false);

            simconnect.MapClientEventToSimEvent(MyEvents.AP_AIRSPEED_HOLD, "AP_AIRSPEED_HOLD");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_AIRSPEED_HOLD, false);

            simconnect.MapClientEventToSimEvent(MyEvents.AP_APR_HOLD, "AP_APR_HOLD");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_APR_HOLD, false);

            simconnect.MapClientEventToSimEvent(MyEvents.AP_ATT_HOLD, "AP_ATT_HOLD");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_ATT_HOLD, false);

            simconnect.MapClientEventToSimEvent(MyEvents.AP_HDG_HOLD, "AP_HDG_HOLD");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_HDG_HOLD, false);

            simconnect.MapClientEventToSimEvent(MyEvents.AP_SPD_VAR_SET, "AP_SPD_VAR_SET");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.AP_SPD_VAR_SET, false);


            simconnect.MapClientEventToSimEvent(MyEvents.PAUSE_SET, "PAUSE_SET");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.PAUSE_SET, false);

            simconnect.MapClientEventToSimEvent(MyEvents.PARKING_BRAKES, "PARKING_BRAKES");
            simconnect.AddClientEventToNotificationGroup(MyGroups.GROUP0, MyEvents.PARKING_BRAKES, false);

            




        }

        private void ListenerThread()
        {
            while (true)
            {
                simconnect?.ReceiveMessage();
                RequestPlaneInfo();
                PlaneInfo planeInfo = WaitForPlaneInfoUpdate();
                signalRClient.SendAltitudeAndSpeed(planeInfo.Altitude, planeInfo.Latitude, planeInfo.Longitude, planeInfo.Speed, planeInfo.Heading);

                Thread.Sleep(100);
            }
        }

        public void RequestPlaneInfo()
        {
            simconnect?.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_PLANE_INFO, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
        }

        public void SendThrottle(double throttle1, double throttle2)
        {
            //simconnect?.SetDataOnSimObject(DEFINITIONS.ThrottleData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new ThrottleData { Throttle = throttle });
            simconnect?.SetDataOnSimObject(DEFINITIONS.ThrottleData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new ThrottleData { Throttle1 = throttle1, Throttle2 = throttle2 });
        }

        public void SendBrake(double rightBrakePosition, double leftBrakePosition)
        {
            simconnect?.SetDataOnSimObject(
                DEFINITIONS.BrakeData,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new BrakeData
                {
                    BrakeRightPosition = rightBrakePosition,
                    BrakeLeftPosition = leftBrakePosition
                });
        }

        //public void SendBrake(bool brake)
        //{
        //    simconnect?.SetDataOnSimObject(DEFINITIONS.BrakeData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new BrakeData { Brake = brake });
        //}

        public void SendFlap(double flap)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.FlapData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new FlapData { Flap = flap });
        }

        public void SendPitch(double pitch)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.PlanePitchData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PlanePitchData { PlanePitchDegrees = pitch });
        }

        public void SendBank(double bank)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.PlaneBankData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PlaneBankData { PlaneBankDegrees = bank });
        }

        public void SendTargetAltitude(double alt)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.PlaneTargetAltitudeData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PlaneTargetAltitudeData { PlaneTargetAltitude = alt });
        }

        public void SendTargetSpeed(double spd)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.PlaneTargetSpeedData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PlaneTargetSpeedData { PlaneTargetSpeed = spd });
        }

        public void SendTargetHeading(double hdg)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.PlaneTargetHeadingData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PlaneTargetHeadingData { PlaneTargetHeading = hdg });
        }

        public void SendTargetVS(double vs)
        {
            simconnect?.SetDataOnSimObject(DEFINITIONS.PlaneTargetVSData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PlaneTargetVSData { PlaneTargetVS = vs });
        }



        public void SetAutopilot(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_SET, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }


        public void SetAutopilotAltHold(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_ALT_HOLD, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }

        public void SetAutopilotSpeedHold(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_AIRSPEED_HOLD, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }

        public void SetAutopilotApprHold(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_APR_HOLD, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }

        public void SetAutopilotAttHold(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_ATT_HOLD, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }

        public void SetAutopilotHdgHold(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_HDG_HOLD, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
        }

        public void SetAutopilotVsHold(bool isOn)
        {
            try
            {
                uint val = isOn ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.AP_SPD_VAR_SET, val, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending pause command to Flight Simulator: {ex.Message}");
            }
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

        public void SetParkingBrakesState(bool brake)
        {
            try
            {
                uint brakeValue = brake ? 1u : 0u;
                simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, MyEvents.PARKING_BRAKES, brakeValue, MyGroups.GROUP0, SIMCONNECT_EVENT_FLAG.DEFAULT);
            }
            catch (COMException ex)
            {
                Console.WriteLine($"Error sending brake command to Flight Simulator: {ex.Message}");
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

                    bool isPaused = receivedData.ElapsedSeconds == previousElapsedSeconds;
                    Console.WriteLine($"IsPaused: {isPaused}");

                    // Remember to update the previousElapsedSeconds for the next check
                    previousElapsedSeconds = receivedData.ElapsedSeconds;

                    Console.WriteLine($"Plane Info - Latitude: {receivedData.Latitude}, Longitude: {receivedData.Longitude}, Altitude: {receivedData.Altitude},  Heading: {receivedData.Heading},  AP: {receivedData.AutoPilot}");
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