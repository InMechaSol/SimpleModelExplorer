using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;
namespace FeatherLightConcepts
{
    public class AndroidDriver : BaseModelClass
    {
        #region Properties exposing members of AndroidDriver Class
        [property: Category("Satellite Information"), Description("Nominal Satellite Azimuth Look Angle in World Coord Deg")]
        public double NominalSatelliteAzimuth
        {
            get { return SatInformation.NomSatAz * 180 / Math.PI; }
            set { SatInformation.NomSatAz = value * Math.PI / 180; }
        }
        [property: Category("Satellite Information"), Description("Nominal Satellite Elevation Look Angle in World Coord Deg")]
        public double NominalSatelliteElevation
        {
            get { return SatInformation.NomSatEl * 180 / Math.PI; }
            set { SatInformation.NomSatEl = value * Math.PI / 180; }
        }
        [property: Category("Satellite Information"), Description("Tracking Signal Frequency in MHZ")]
        public double TrackingSignalFrequency
        {
            get { return SatInformation.TrackingFrequency / 1e6; }
            set { SatInformation.TrackingFrequency = value * 1e6; }
        }
        [property: Category("Antenna Information"), Description("Reflector Diameter in meters")]
        public double ReflectorDiameter
        {
            get { return AntInformation.RefDiameter; }
            set { AntInformation.RefDiameter = value; }
        }
        [property: Category("Antenna Information"), Description("Base Frame Rotation about World Frame X axis in Deg")]
        public double BaseRollAngle
        {
            get { return AntInformation.BaseRoll * 180 / Math.PI; }
            set { AntInformation.BaseRoll = value * Math.PI / 180; }
        }
        [property: Category("Antenna Information"), Description("Base Frame Rotation about World Frame Z axis in Deg")]
        public double BaseYawAngle
        {
            get { return AntInformation.BaseYaw * 180 / Math.PI; }
            set { AntInformation.BaseYaw = value * Math.PI / 180; }
        }
        [property: Category("Antenna Information"), Description("Base Frame Rotation about World Frame Y axis in Deg")]
        public double BasePitchAngle
        {
            get { return AntInformation.BasePitch * 180 / Math.PI; }
            set { AntInformation.BasePitch = value * Math.PI / 180; }
        }
        [property: Category("Scan Settings"), Description("Maximum Outer Radius of Spiral Scan in Deg")]
        public double MaximumRadius
        {
            get { return ScanParameters.MaximumRadius; }
            set { ScanParameters.MaximumRadius = value; }
        }
        [property: Category("Scan Settings"), Description("Radial Spacing of Spiral Scan Samples in Deg")]
        public double RadialSpacing
        {
            get { return ScanParameters.DeltaRadius; }
            set { ScanParameters.DeltaRadius = value; }
        }
        [property: Category("Scan Settings"), Description("Angular Spacing of Scan Samples Along Spiral in Deg")]
        public double AngularSpacing
        {
            get { return ScanParameters.DeltaTheta * 180 / Math.PI; }
            set { ScanParameters.DeltaTheta = value * Math.PI / 180; }
        }
        [property: Category("Scan Settings"), Description("Selection of Coordinate Frame (world or ped) in which to scan")]
        public CoordinateFrames ScanFrameSelect
        {
            get { return ScanParameters.CoordFrameSelect; }
            set { ScanParameters.CoordFrameSelect = value; }
        }
        [property: Category("Scan Settings"), Description("Time to remain at each scan sample location in SEC")]
        public double ScanTimeToDwell
        {
            get { return ScanParameters.DwellTime; }
            set { ScanParameters.DwellTime = value; }
        }
        [property: Category("Step Settings"), Description("Selection of Coordinate Frame (world or ped) in which to step")]
        public CoordinateFrames StepFrameSelect
        {
            get { return StepParameters.CoordFrameSelect; }
            set { StepParameters.CoordFrameSelect = value; }
        }
        [property: Category("Step Settings"), Description("Time to remain at each step sample location in SEC")]
        public double StepTimeToDwell
        {
            get { return StepParameters.DwellTime; }
            set { StepParameters.DwellTime = value; }
        }
        [property: Category("Step Settings"), Description("Angular Spacing of Step Samples Along Hill Climb in Deg")]
        public double StepSize
        {
            get { return StepParameters.DeltaTheta * 180 / Math.PI; }
            set { StepParameters.DeltaTheta = value * Math.PI / 180; }
        }
        #endregion
        #region Members of AndroidDriver Class
        // Driver State Variables
        DeviceExeStates CommState;          // State Variable driving execution() function 
        bool SimulateData;                  // Flag indicating simulation of FLIR data
        int CommConnections;                // Counter of Attempted communication attempts

        // Android SetPoint Data
        SatelliteInfo SatInformation;       // Satellite Informatoin Class
        AntennaInfo AntInformation;         // Antenna Information Class
        ScanSettings ScanParameters;        // Modem Lock, Coarse Scan Settings
        StepSettings StepParameters;        // Peak Signal, Fine Step Settings

        // Driver Communication Objects

        #endregion
        #region Main Execution System Functions of AndroidDriver Class
        // Constructor   
        public AndroidDriver()
        {
            CommState = DeviceExeStates.Initialize;
            SimulateData = false;
            CommConnections = 0;
        }
        public override void Initialize()
        {


            // check for success
            SimulateData = true;
            if (!SimulateData)
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Android Driver Connected via RS232");
            else
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Android Driver Running in Simulation Mode");
            CommConnections = 0;
        }
        public override void Prepare()
        {
            ;
        }
        public override void Execute()
        {
            switch (CommState)
            {
                case DeviceExeStates.Initialize:
                    CommState = DeviceExeStates.ReadStatus;
                    break;
                case DeviceExeStates.ReadStatus:

                    if (!SimulateData)
                    {
                    }
                    CommState = DeviceExeStates.WriteCommands;
                    break;
                case DeviceExeStates.WriteCommands:

                    CommState = DeviceExeStates.ReadStatus;
                    break;
            }
        }
        #endregion
        #region Subfunctions of AndroidDriver Class

        #endregion
    }
}
