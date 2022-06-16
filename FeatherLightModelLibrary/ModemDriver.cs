using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    public class ModemDriver : BaseModelClass
    {

        #region Properties exposing members of ModemDriver Class
        [Category("Device Status"), Description("Indication of Rx Lock Status"), ReadOnly(false)]
        public bool ModemLockStatus
        {
            get { return this.ModemLock; }
            set { this.ModemLock = value; }
        }
        [Category("Device Status"), Description("Signal Power Metric (dB)"), ReadOnly(false)]
        public double SignalMetricStatus
        {
            get { return SignalMetric; }
            set { SignalMetric = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Tracking Signal Rx Frequency (MHz)")]
        public double TrackingMHz
        {
            get
            {
                this.RePrepare = true;
                return SpoofBeam.Frequency;
            }
            set { SpoofBeam.Frequency = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Noise floor level relative to peak signal metric in dB")]
        public double NoiseFloor
        {
            get { return SpoofBeam.NoiseFloorSim; }
            set { SpoofBeam.NoiseFloorSim = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Noise amplitude about noise floor level in dB")]
        public double NoiseDelta
        {
            get { return SpoofBeam.NoiseDelta; }
            set { SpoofBeam.NoiseDelta = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Required headroom above noise floor level to achieve modem lock (dB)")]
        public double RxLockThreshhold
        {
            get { return SpoofBeam.RxLockThresh; }
            set { SpoofBeam.RxLockThresh = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Simulated Actual Peak Beam Position World Azimuth Degrees")]
        public double SpoofPeakAzimuth
        {
            get { return SpoofBeam.SpoofActualAz; }
            set { SpoofBeam.SpoofActualAz = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Simulated Actual Peak Beam Position World Elevation Degrees")]
        public double SpoofPeakElevation
        {
            get { return SpoofBeam.SpoofActualEl; }
            set { SpoofBeam.SpoofActualEl = value; }
        }
        [Category("Simulation - Signal and Noise"), Description("Simulated Peak Beam Strength in dB"), ReadOnly(true)]
        public double PeakBeamSignalMetric
        {
            get { return SpoofBeam.PeakMetric; }
            set { SpoofBeam.PeakMetric = value; }
        }
        [Category("Simulation - Antenna Info"), Description("Simulated Feedback Position World Azimuth Degrees")]
        public double SpoofFeedbackAzimuth
        {
            get { return SpoofBeam.WorldFeedbackAz; }
            set { SpoofBeam.WorldFeedbackAz = value; }
        }
        [Category("Simulation - Antenna Info"), Description("Simulated Feedback Position World Elevation Degrees")]
        public double SpoofFeedbackElevation
        {
            get { return SpoofBeam.WorldFeedbackEl; }
            set { SpoofBeam.WorldFeedbackEl = value; }
        }
        [Category("Simulation - Antenna Info"), Description("Simulated Radial Error in World Coordinate Degrees"), ReadOnly(true)]
        public double SpoofRadialError
        {
            get { return SpoofBeam.Dist2Peak; }
            set { SpoofBeam.Dist2Peak = value; }
        }
        [Category("Simulation - Antenna Info"), Description("Estimated -3dB Beam Width in World Coordinate Degrees"), ReadOnly(true)]
        public double HalfPowerBW
        {
            get { return SpoofBeam.HPBeamWidth; }
            set { SpoofBeam.HPBeamWidth = value; }
        }
        [Category("Simulation - Antenna Info"), Description("Diameter of Antenna Reflector in Meters")]
        public double ReflectorDiameter
        {
            get
            {
                this.RePrepare = true;
                return SpoofBeam.Diameter;
            }
            set { SpoofBeam.Diameter = value; }
        }
        [Category("Device Status"), Description("Execution State of Driver Class")]
        public DeviceExeStates StateofExecution
        {
            get { return CommState; }
            set { CommState = value; }
        }
        #endregion
        #region Members of ModemDriver Class
        DeviceExeStates CommState;              // Device Driver Communication State
        bool SimulateData;                      // Flag to simulate data 
        int CommConnections;                    // Counter of Attempted communication attempts
        bool ModemLock, LastModemLock;          // Indication of RX Lock from Modem
        double SignalMetric;                    // Rx Signal Power Metric from Modem
        BeamShapeInfo SpoofBeam;                // Data Container for parabolic beam spoof




        #endregion
        #region Main Execution System Functions of ModemDriver Class
        public ModemDriver()
        {
            SpoofBeam.NoiseRandSim = new Random();  // rand(-1,1) dB
            

        }
        public override void Initialize()
        {
            CommState = DeviceExeStates.Initialize;
            ModemLock = false;
            // check for success
            SimulateData = true;
            if (!SimulateData)
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Modem Driver Connected via RS232");
            else
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Modem Driver Running in Simulation Mode");
            CommConnections = 0;
            SpoofBeam.Frequency = 3.1e10;           // 31 GHz
            SpoofBeam.Diameter = 0.60;              // 60 cm
            SpoofBeam.NoiseFloorSim = -12;          // -12 dB down from Peak
            SpoofBeam.NoiseDelta = 0.20;            // 0.01 dB times rand(0,1)
            SpoofBeam.PeakMetric = 1.0;             // 1.0 dBm actual peak power
            SpoofBeam.RxLockThresh = 1.0;           // 1.0 dB above noise floor to achieve lock
            SpoofBeam.SpoofActualAz = 23.7;            // World coord Azimuth to peak of beam
            SpoofBeam.SpoofActualEl = 22.7;           // World coord Elevation to peak of beam
            this.RePrepare = true;
        }
        public override void Prepare()
        {
            SpoofBeam.beta = -12 * Math.Pow((SpoofBeam.Frequency * SpoofBeam.Diameter) / (BeamShapeInfo.K * BeamShapeInfo.C), 2);
            SpoofBeam.HPBeamWidth = (BeamShapeInfo.K * BeamShapeInfo.C) / (SpoofBeam.Frequency * SpoofBeam.Diameter);
        }
        public override void Execute()
        {
            switch (CommState)
            {
                case DeviceExeStates.Initialize:
                    CommState = DeviceExeStates.ReadStatus;
                    break;
                case DeviceExeStates.ReadStatus:

                    if (SimulateData)
                    {
                        // Calculate Distance from Simulated center
                        SpoofBeam.Dist2Peak = Math.Sqrt(Math.Pow(SpoofBeam.SpoofActualAz - SpoofBeam.WorldFeedbackAz, 2) + Math.Pow(SpoofBeam.SpoofActualEl - SpoofBeam.WorldFeedbackEl, 2));

                        // Calculate Spoof Signal Metric from Peak Signal + Parabolic signal Drop + Noise                        
                        SignalMetric = SpoofBeam.PeakMetric + SpoofBeam.beta * Math.Pow(SpoofBeam.Dist2Peak, 2) + SpoofBeam.NoiseDelta * SpoofBeam.NoiseRandSim.NextDouble();

                        // Determine Rx Lock
                        ModemLock = SignalMetric > (SpoofBeam.NoiseFloorSim + SpoofBeam.RxLockThresh);
                        // Report Signal Metric only if Rx Lock
                        if (!ModemLock)
                            SignalMetric = Double.NaN;

                    }
                    if (LastModemLock != ModemLock)
                    {
                        if (ModemLock)
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Modem Lock Status Achieved");
                        else
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Lost Modem Lock Status");
                    }
                    LastModemLock = ModemLock;
                    CommState = DeviceExeStates.WriteCommands;
                    break;
                case DeviceExeStates.WriteCommands:
                    CommState = DeviceExeStates.ReadStatus;
                    break;
            }

        }
        #endregion

    }
}
