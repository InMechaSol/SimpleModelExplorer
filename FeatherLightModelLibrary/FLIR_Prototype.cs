using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;
// Imports for communication
using System.IO.Ports;
using System.Net.Sockets;
namespace FeatherLightConcepts
{
    // Flir Prototype Model
    // This model will execute on the simple model explorer and perform all of the 
    // control task of the micro ACU.  By inheriting from the BaseModelClass and
    // implementing the required abstract function of the base class, this class 
    // will adhere to the simple model explorer api.
    class FLIR_Prototype : BaseModelClass
    {
        // Define Constants
        double FLIR_Ang_Conv = 0.012857;            // Conversion factor from/to pedestal angles (rad) to command angles in (???steps)
        double Threshold = 2.0;                     // ?
        double speedOfLight = 2.99e8;               // Speed of Light / Electromagnetic Waves (?)
        double k = 70;                              // Coefficient for power function (?)

        // Declare Class Members
        //FLIR_ProtoGUI ControllerGUI;                // Pop-up Controller GUI for test and debug

        // Declare Fields linked to Properties
        // Inputs
        double baseYaw, baseRoll, basePitch;        // Base Attitude in Radians
        double wAz, wEl;                            // Satellite Look angles world frame in Radians
        double dRadius, dTheta, Rmax;			    // Spiral Scan Geometry Inputs
        double centerAz, centerEl;                  // The actual position of a satellite
        double freqRx, dReflector;                  // I don't remember (?)
        bool bSendCMDs;                             // Trigger to send a signal
        bool scanInPad;                             // Switch to apply scan offsets in world or ped coords        
        // Outputs
        double pAz, pEl;                            // Sat. Look angles, pedestal frame, in Radians
        double pAzCMD, pElCMD;                      // Total Combined pedestal coordinate command angle
        double AzOffset, ElOffset;                  // Scan offsets to the world look angles in radians
        double wAzCMD, wElCMD;                      // Total Combined world coordinate command angle
        double pAzFBK, pElFBK;                      // Pedestal axis feedback angles in FLIR controller units
        double spoofOffsetfromPeak;                 // The distance between a scanning point and the position of a satellite (?)
        double spoofSignalMetric;                   // Spoofed modem signal metric (?)
        bool modemLock;                             // Indication of modem lock
        double beta;                                // Coefficient for a parabolic function (?)
        List<double> SignalMetricValues = new List<Double>();            // ?
        List<double> AzimuthCMDValues = new List<Double>();               // ?
        List<double> ElevationCMDValues = new List<Double>();            // ?
        // Internals
        double wX, wY, wZ;                          // Vector of Sat. Look angles in world frame in Meters (irrelevant units)        
        double pX, pY, pZ;                          // Vector of Sat. Look angles in pedestal frame (irrelavant units)
        double n, r;                                // Radus of spiral scan (deg)
        double d;                                   // Active radial error from pcmd to pfbk
        int rounded_pAzCMD, rounded_pElCMD;         // Rounded Integer Representations of position commands
        int rounded_pAzFBk, rounded_pElFBk;         // Rounded Integer Representations of position feedback

        // Declare Fields that are internal and not exposed via properties
        double C_baseYaw, C_baseRoll, C_basePitch, S_baseYaw, S_baseRoll, S_basePitch;      // Sin/Cos values of base attitude angles
        SerialPort FLIRComPort;                     // RS-232 Comm Port to FLIR Controller
        bool FLIRInitialized;                       // Flag indicating status of FLIR communications
        bool onTarget;
        double lastpAzCMD, lastpElCMD;              // History of Pedestal Command Angles
        bool lastOnTarget;                          // History of ontarget indication
        int dwellTimer;                             // count of execution cycles for dwell time
        bool FlirSimulateMode;                      // Flag indicating simulationg of Flir Control, not connected to actual controller

        String BResponse;
        int caseCount = 1, caseCountLast = 1;
        string tempString = "";

        public enum CMDModes { Nominal, Spiral, Step, Peak }    // Position Commanding modes
        CMDModes CMDState = CMDModes.Nominal;
        CMDModes CMDStateLast = CMDModes.Nominal;


        // Defince Properties providing .NET access to select fields

        #region Input Parameters, triggering the prepare function
        // Base Yaw, rotation of base about z axis in world frame
        [property: Category("0: Input Parameters"), Description("Heading of Antenna Base in Deg")]
        public double BaseYaw
        {
            get { return 180 * baseYaw / Math.PI; }
            set
            {
                baseYaw = Math.PI / 180 * value;
                this.RePrepare = true;
            }
        }

        // Base Roll, rotation of base about x axis in world frame
        [property: Category("0: Input Parameters"), Description("Roll of Antenna Base in Deg")]
        public double BaseRoll
        {
            get { return 180 * baseRoll / Math.PI; }
            set
            {
                baseRoll = Math.PI / 180 * value;
                this.RePrepare = true;
            }
        }

        // Base Pitch, rotation of base about y axis in world frame
        [property: Category("0: Input Parameters"), Description("Pitch of Antenna Base in Deg")]
        public double BasePitch
        {
            get { return 180 * basePitch / Math.PI; }
            set
            {
                basePitch = Math.PI / 180 * value;
                this.RePrepare = true;
            }
        }

        // World Azimuth Look Angle
        [property: Category("0: Input Parameters"), Description("World Azimuth Look Angle in Deg")]
        public double WAz
        {
            get { return 180 * wAz / Math.PI; }
            set
            {
                wAz = Math.PI / 180 * value;
                this.RePrepare = true;
            }
        }

        // World Elevation Look Angle
        [property: Category("0: Input Parameters"), Description("World Elevation Look Angle in Deg")]
        public double WEl
        {
            get { return 180 * wEl / Math.PI; }
            set
            {
                wEl = Math.PI / 180 * value;
                this.RePrepare = true;
            }
        }

        // Radial Spacing of Sprials
        [property: Category("0: Input Parameters"), Description("Radial Spacing of Sprials in World Frame in Deg")]
        public double DRadius
        {
            get { return dRadius; }
            set
            {
                dRadius = value;
                this.RePrepare = true;
            }
        }

        // Angular Resolution of Sprials Geometry
        [property: Category("0: Input Parameters"), Description("Angular Resolution of Sprials Geometry in World Frame in Deg")]
        public double DTheta
        {
            get { return 180 * dTheta / Math.PI; }
            set
            {
                dTheta = Math.PI / 180 * value;
                this.RePrepare = true;
            }
        }

        // Maximum Radius of the Sprial Scan
        [property: Category("0: Input Parameters"), Description("Maximum Radius of the Sprial Scan in World Frame in Deg")]
        public double RMax
        {
            get { return 180 / Math.PI * Rmax; }
            set
            {
                Rmax = Math.PI / 180 * value;
            }
        }

        // Blue dot
        // Azimuth angle of the actual position of a satellite in World Coordinate
        [property: Category("0: Input Parameters"), Description("Azimuth angle of the actual position of a satellite in World Coordinate in Deg")]
        public double CenterAz
        {
            get { return 180 / Math.PI * centerAz; }
            set
            {
                centerAz = Math.PI / 180 * value;
            }
        }

        // Blue dot
        // Elavation angle of the actual position of a satellite in World Coordinate
        [property: Category("0: Input Parameters"), Description("Elavation angle of the actual position of a satellite in World Coordinate in Deg")]
        public double CenterEl
        {
            get { return 180 / Math.PI * centerEl; }
            set
            {
                centerEl = Math.PI / 180 * value;
            }
        }

        // Frequency of Electromagnetic Wave
        [property: Category("0: Input Parameters"), Description("Frequency of Electromagnetic Wave")]
        public double FreqRx
        {
            get { return freqRx; }
            set
            {
                freqRx = value;
            }
        }

        // Diameter of the Antenna
        [property: Category("0: Input Parameters"), Description("Diameter of the Antenna")]
        public double DReflector
        {
            get { return dReflector; }
            set
            {
                dReflector = value;
            }
        }

        // Trigger to Initiate to Scan
        [property: Category("0: Input Parameters"), Description("Trigger to Iniate to scan")]
        public CMDModes CMDSTATE
        {
            get { return CMDState; }
            set
            {
                CMDState = value;
                this.RePrepare = true;
            }
        }

        // ?
        [property: Category("0: Input Parameters"), Description("?")]
        public bool ScanInPad
        {
            get { return scanInPad; }
            set
            {
                scanInPad = value;
                this.RePrepare = true;
            }
        }



        #endregion

        #region Output Parameters, changed by the model, not changed by the GUI
        // Pedestal Azimuth Feedback Angle
        [property: Category("1: Output Parameters"), Description("Pedestal Azimuth Feedback Angle in Deg")]
        public double PAzFBK
        {
            // watch for units comming back from FLIR controller
            get { return pAzFBK; }
            set
            {
                // watch for units comming back from FLIR controller
                pAzFBK = value;
            }
        }

        // Pedestal Elevation Feedback Angle
        [property: Category("1: Output Parameters"), Description("Pedestal Elevation Feedback Angle in Deg")]
        public double PElFBK
        {
            // watch for units comming back from FLIR controller
            get { return pElFBK; }
            set
            {
                // watch for units comming back from FLIR controller
                pElFBK = value;
            }
        }

        // Pedestal Azimuth Look Angle
        [property: Category("1: Output Parameters"), Description("Pedestal Azimuth Look Angle in Deg")]
        public double PAz
        {
            get { return 180 / Math.PI * pAz; }
            set
            {
                pAz = Math.PI / 180 * value;
            }
        }

        // Pedestal Elevation Look Angle
        [property: Category("1: Output Parameters"), Description("Pedestal Elevation Look Angle in Deg")]
        public double PEl
        {
            get { return 180 / Math.PI * pEl; }
            set
            {
                pEl = Math.PI / 180 * value;
            }
        }

        // Sprial Scan Azimuth Offset
        [property: Category("1: Output Parameters"), Description("Sprial Scan Azimuth Offset in World Frame in Deg")]
        public double AzimuthOffset
        {
            get { return 180 / Math.PI * AzOffset; }
            set
            {
                AzOffset = Math.PI / 180 * value;
            }
        }

        // Sprial Scan Elevation Offset
        [property: Category("1: Output Parameters"), Description("Sprial Scan Elevation Offset in World Frame in Deg")]
        public double ElevationOffset
        {
            get { return 180 / Math.PI * ElOffset; }
            set
            {
                ElOffset = Math.PI / 180 * value;
            }
        }

        // Total World Azimuth Position Commend
        [property: Category("1: Output Parameters"), Description("Total World Azimuth Position Command in Degrees")]
        public double WAzCMD
        {
            get { return 180 / Math.PI * wAzCMD; }
            set
            {
                wAzCMD = Math.PI / 180 * value;
            }
        }

        // Total World Elevation Position Commend
        [property: Category("1: Output Parameters"), Description("Total World Elevation Position Command in Degrees")]
        public double WElCMD
        {
            get { return 180 / Math.PI * wElCMD; }
            set
            {
                wElCMD = Math.PI / 180 * value;
            }
        }

        // Total Pedestal Azimuth Position Commend
        [property: Category("1: Output Parameters"), Description("Total World Elevation Position Command in Degrees")]
        public double PAzCMD
        {
            get { return 180 / Math.PI * pAzCMD; }
            set
            {
                pAzCMD = Math.PI / 180 * value;
            }
        }

        // Total Pedestal Elevation Position Commend
        [property: Category("1: Output Parameters"), Description("Total World Elevation Position Command in Degrees")]
        public double PElCMD
        {
            get { return 180 / Math.PI * pElCMD; }
            set
            {
                pElCMD = Math.PI / 180 * value;
            }
        }

        // The distance between Blue dot and Orange Dot
        // Actual acquisition error; BRE - Beam Radial Error
        [property: Category("1: Output Parameters"), Description("Actual acquisition error")]
        public double SpoofOffsetfromPeak
        {
            get { return 180 / Math.PI * spoofOffsetfromPeak; }
            set
            {
                spoofOffsetfromPeak = Math.PI / 180 * value;
            }
        }

        // 
        [property: Category("1: Output Parameters"), Description("")]
        public double SpoofSignalMetric
        {
            get { return spoofSignalMetric; }
            set
            {
                spoofSignalMetric = value;
            }
        }

        #endregion

        #region Internal Values for Calculation and Debug
        // World Frame x coordinate
        [property: Category("2: Internal Values"), Description("World Frame Look Angle Vector, X Coord, (unitless)")]
        public double WX
        {
            get { return wX; }
            set
            {
                wX = value;
            }
        }
        // World Frame y coordinate
        [property: Category("2: Internal Values"), Description("World Frame Look Angle Vector, Y Coord, (unitless)")]
        public double WY
        {
            get { return wY; }
            set
            {
                wY = value;
            }
        }
        // World Frame z coordinate
        [property: Category("2: Internal Values"), Description("World Frame Look Angle Vector, Z Coord, (unitless)")]
        public double WZ
        {
            get { return wZ; }
            set
            {
                wZ = value;
            }
        }
        // Pedestal Frame x coordinate
        [property: Category("2: Internal Values"), Description("Pedestal Frame Look Angle Vector, X Coord, (unitless)")]
        public double PX
        {
            get { return pX; }
            set
            {
                pX = value;
            }
        }
        // Pedestal Frame y coordinate
        [property: Category("2: Internal Values"), Description("Pedestal Frame Look Angle Vector, Y Coord, (unitless)")]
        public double PY
        {
            get { return pY; }
            set
            {
                pY = value;
            }
        }
        // Pedestal Frame z coordinate
        [property: Category("2: Internal Values"), Description("Pedestal Frame Look Angle Vector, Z Coord, (unitless)")]
        public double PZ
        {
            get { return pZ; }
            set
            {
                pZ = value;
            }
        }
        // 
        [property: Category("2: Internal Values"), Description("")]
        public double N
        {
            get { return n; }
            set
            {
                n = value;
            }
        }
        // 
        [property: Category("2: Internal Values"), Description("")]
        public double R
        {
            get { return 180 / Math.PI * r; }
            set
            {
                r = Math.PI / 180 * value;
            }
        }
        // 
        [property: Category("2: Internal Values"), Description("")]
        public double D
        {
            get { return 180 / Math.PI * d; }
            set
            {
                d = Math.PI / 180 * value;
            }
        }

        // Trigger to send a signal
        [property: Category("2: Internal Parameters"), Description("Flag from control system to send new commands to FLIR")]
        public Boolean BSendCMDs
        {
            get { return bSendCMDs; }
            set
            {
                bSendCMDs = value;
            }
        }
        #endregion

        // Constructor
        // According to the API, the constructor is called once at main GUI startup
        public FLIR_Prototype()
        {

            // Set some initial test values
            BaseYaw = -2;
            BasePitch = -3;
            BaseRoll = 4;
            DRadius = 0.1;
            DTheta = 97;
            RMax = .4;
            WAz = 15.7;
            WEl = 20.2;
            FreqRx = 3.10e10;
            dReflector = 0.85;
            CenterAz = 15.0;
            CenterEl = 21.0;


            // According to the API, set the ReInitialize flag true to trigger
            // Initialization on the execution system start immediately following instantiation
            this.ReInitialize = true;
        }

        // Initialize is the first of the API key executional functions and
        // According to the API, executes in the GUI timer cycle immediately following 
        // a Reset button click
        public override void Initialize()
        {
            // ReInitialize the GUI
            ReInitialize_GUI();


            // ReInitialize the Android UDP link
            InitializeFLIRSerial();

            // ReInitialize the FLIR RS-232 link


            // ReInitialize key Model parameters
            bSendCMDs = true;
            CMDState = CMDModes.Nominal;

            // When all subsystems are intialized,
            if (FLIRInitialized || FlirSimulateMode)
            {
                // Reset the exe system trigger
                ReInitialize = false;

                // Set the prepare trigger
                this.RePrepare = true;
            }

        }

        // ReInitialize_GUI
        void ReInitialize_GUI()
        {
            //// If the GUI is already open, close it
            //if (ControllerGUI != null)
            //{
            //    ControllerGUI.Close();
            //    ControllerGUI.Dispose();
            //}

            //// Open a new GUI instance
            //ControllerGUI = new FLIR_ProtoGUI();
            //ControllerGUI.Visible = true;
        }

        // InitializeFLIRSerial()
        // Function to reset and initialize serial communication with the FLIR controller
        // Blocking of this function, will cause main GUI to block as well
        public void InitializeFLIRSerial()
        {
            // check if serial object does not exist
            if (FLIRComPort == null)
            {
                // create the object
                FLIRComPort = new SerialPort();

                // configure the serial port
                FLIRComPort.PortName = "COM9";
                FLIRComPort.BaudRate = 9600;
                FLIRComPort.DataBits = 8;
                FLIRComPort.StopBits = StopBits.One;
                FLIRComPort.Handshake = Handshake.None;
                FLIRComPort.Parity = Parity.None;

                // open port
                try
                {
                    FLIRComPort.Open();

                    // check for success
                    if (FLIRComPort.IsOpen)
                        FLIRInitialized = true;
                    else
                        FlirSimulateMode = true;
                }
                catch
                {
                    FlirSimulateMode = true;
                }

            }
            // if the serial object does exist
            else
            {
                // Destroy it and reset
                FLIRComPort.Close();
                FLIRComPort.Dispose();
                FLIRInitialized = false;
            }

        }

        // Prepare is the second of the API key executional function and
        // According to the API, executes in the GUI timer cycle.
        public override void Prepare()
        {
            // RePrepare the GUI


            // RePrepare the Android UDP link


            // RePrepare the FLIR RS-232 link


            // RePrepare key Model parameters
            C_baseYaw = Math.Cos(baseYaw);
            C_baseRoll = Math.Cos(baseRoll);
            C_basePitch = Math.Cos(basePitch);
            S_baseYaw = Math.Sin(baseYaw);
            S_baseRoll = Math.Sin(baseRoll);
            S_basePitch = Math.Sin(basePitch);





            // Reset the exe system trigger
            RePrepare = false;
        }


        // Execute is the Final of the API key executional function and
        // According to the API, executes in the cyclic background worker thread
        // that is triggered by the GUI timer at periodic intervals.  
        // - This is the main excutional function of the controller
        public override void Execute()
        {
            // Update the GUI


            // Recieve data from the android


            // Recieve data from the FLIR
            FLIRStatusQuery();

            // Perform control system functional tasks
            FLIR_ProtoControl();

            // Send Commands to FLIR controller via RS-232
            FLIR_Commanding();

            // Send data to Android

        }


        // FLIRStatusQuery
        // Function to read the pedestal azimuth and elevation angles quickly
        public void FLIRStatusQuery()
        {
            if (!FlirSimulateMode)
            {
                // Send Position and Speed Query
                FLIRComPort.WriteLine("B");
                // Read Response
                BResponse = FLIRComPort.ReadLine();
                BResponse = FLIRComPort.ReadLine();
                BResponse = BResponse.TrimStart("B *P(".ToCharArray());
                // Parse
                #region Parse FLIR Response

                // Loop through all characters
                caseCount = 1;
                caseCountLast = 0;
                tempString = "";
                foreach (char flirChar in BResponse)
                {
                    // Switch based on search case
                    switch (caseCount)
                    {

                        // Parse Azimuth
                        case 1:
                            {
                                if (flirChar.CompareTo(",".ToCharArray()[0]) == 0)
                                {
                                    pAzFBK = Convert.ToDouble(tempString) * (FLIR_Ang_Conv);
                                    rounded_pAzFBk = Convert.ToInt32(tempString);
                                    tempString = "";
                                    caseCount++;
                                }
                                else
                                    tempString += flirChar;
                                break;
                            }
                        // Parse Elevation
                        case 2:
                            {
                                if (flirChar.CompareTo(")".ToCharArray()[0]) == 0)
                                {
                                    pElFBK = Convert.ToDouble(tempString) * (-FLIR_Ang_Conv);
                                    rounded_pElFBk = Convert.ToInt32(tempString);
                                    tempString = "";
                                    caseCount++;
                                }
                                else
                                    tempString += flirChar;
                                break;
                            }
                        // Skip S and Stuff
                        case 3: caseCount++; break;
                        case 4: caseCount++; break;
                        case 5: caseCount++; break;

                        // Parse Az Speed
                        case 6: break;
                        // Parse El Speed
                        case 7: break;


                    }
                    caseCountLast = caseCount;
                }
                #endregion
            }
            else // if simulating, set feedback equal to command
            {
                rounded_pAzFBk = rounded_pAzCMD;
                pAzFBK = rounded_pAzFBk * FLIR_Ang_Conv;
                rounded_pElFBk = rounded_pElCMD;
                pElFBK = -rounded_pElFBk * FLIR_Ang_Conv;
            }

        }

        // Function to send serial commands to FLIR controller
        public void FLIR_Commanding()
        {

            // Only send commands when instructed by control system
            if (bSendCMDs)
            {
                // prepare values for commanding to FLIR
                rounded_pAzCMD = (int)Math.Round((pAzCMD * 180 / Math.PI) / FLIR_Ang_Conv);
                rounded_pElCMD = (int)Math.Round((pElCMD * 180 / Math.PI) / -FLIR_Ang_Conv);
                if (!FlirSimulateMode)
                {
                    // Send Position/Speed Command to FLIR 
                    FLIRComPort.WriteLine("B" + rounded_pAzCMD.ToString() + "," + rounded_pElCMD.ToString() + "," + (1100).ToString() + "," + (1100).ToString());
                    // Read response and verify
                    FLIRComPort.ReadLine();
                    FLIRComPort.ReadLine();
                }
                // Clear Send Flag
                bSendCMDs = false;

            }
        }

        // Perform Control Functional Tasks
        void FLIR_ProtoControl()
        {
            #region// perform preliminary calculations in prep for control
            // Calculates radial error between commanded angle and feedback of FLIR controller
            d = Math.Sqrt((rounded_pAzCMD - rounded_pAzFBk) * (rounded_pAzCMD - rounded_pAzFBk) + (rounded_pElCMD - rounded_pElFBk) * (rounded_pElCMD - rounded_pElFBk));
            // Determine if on commanded target positions
            onTarget = d < Threshold;

            // Detect change in on target state (rising edge, just got to target
            if (onTarget && !lastOnTarget)
                dwellTimer = 0;
            // Detect level, (while on target)
            else if (onTarget)
                // Increment Dwell Timer
                dwellTimer++;

            // Simulate Collecting Modem Signal Information
            ParabolaFunction();
            modemLock = spoofSignalMetric > -12.0;
            #endregion

            #region// Perform Commanding and Auto Acquisition
            switch (CMDState)
            {
                // Goto the commanded nominal satellite look angle
                case CMDModes.Nominal:
                    wAzCMD = wAz;
                    wElCMD = wEl;
                    break;
                // Perform coarse spiral scan to detect modem lock
                case CMDModes.Spiral:

                    // Stop running when R >= Rmax, no modem lock
                    if (r >= Rmax)
                    {
                        // Send back to nominal sat location
                        CMDState = CMDModes.Nominal;
                        // Reset Dwell Timer
                        dwellTimer = 0;
                    }
                    // Stop running when modem is locked            
                    else if (modemLock)
                    {


                        // Send to step state for fine peaking
                        CMDState = CMDModes.Step;
                        // Reset Dwell Timer
                        dwellTimer = 0;
                    }

                    // Only run when dwell time has expired
                    if (dwellTimer >= 19)
                    {
                        // detect rising edge of spiral trigger
                        if (CMDStateLast != CMDModes.Spiral)
                        {
                            // Reset Scan variables
                            n = 0;
                            AzOffset = 0;
                            ElOffset = 0; ;
                        }

                        // Calculate Spiral Scan Command Offsets
                        r = dRadius * dTheta * n / 360;
                        AzOffset = r * Math.Cos(n * dTheta);
                        ElOffset = r * Math.Sin(n * dTheta);
                        n++;

                        // if scanning in pedestal coordinates, apply offsets
                        if (scanInPad)
                        {
                            wAzCMD = wAz;
                            wElCMD = wEl;
                        }
                        else
                        {
                            // when scanning, augment command
                            wAzCMD = wAz + AzOffset;
                            wElCMD = wEl + ElOffset;
                        }
                        dwellTimer = 0;

                    }

                    break;
                // Perform fine step operation to and fit parabola to Az
                case CMDModes.Step:

                    break;
                case CMDModes.Peak:

                    break;

            }
            #endregion

            #region // Convert to pedestal coordinates and trigger to send commands
            // 1) Convert wAz and wEl to Cartesian Coordinates
            Spher2Cart();
            // 2) Rotate wX, wY, wZ vector by base angles
            World2Ped();
            // 3) Convert world vector to Spherical Coordinates
            Cart2Sphere();

            // Determine whether or not to command the FLIR controller
            if (pAzCMD != lastpAzCMD || pElCMD != lastpElCMD)
                bSendCMDs = true;
            #endregion

            // Capture History for Change Detection
            lastOnTarget = onTarget;
            lastpAzCMD = pAzCMD;
            lastpElCMD = pElCMD;
            CMDStateLast = CMDState;

        }

        // Calculate Simulated signal power as a function of offset from peak
        void ParabolaFunction()
        {
            beta = -12 * ((freqRx * dReflector) / (k * speedOfLight)) * ((freqRx * dReflector) / (k * speedOfLight));
            spoofOffsetfromPeak = Math.Sqrt((wAzCMD - centerAz) * (wAzCMD - centerAz) + (wElCMD - centerEl) * (wElCMD - centerEl));
            spoofSignalMetric = beta * SpoofOffsetfromPeak * SpoofOffsetfromPeak;
        }

        // Process scan data for parabolic fit to find center
        void ProcessScanData(bool processAzimuth)
        {
            // Calculate Required Intermediate Values
            double sum0N = 0, sum1N = 0, sum2N = 0, sum3N = 0, sum4N = 0, sum0YN = 0, sum1YN = 0, sum2YN = 0;
            for (int Procindex = 0; Procindex < this.SignalMetricValues.Count; Procindex++)
            {
                // Zero Order Sum = N
                sum0N++;

                // Higher order sums depend on axis of processing
                if (processAzimuth)
                {
                    sum1N += AzimuthCMDValues[Procindex];
                    sum2N += Math.Pow(AzimuthCMDValues[Procindex], 2);
                    sum3N += Math.Pow(AzimuthCMDValues[Procindex], 3);
                    sum4N += Math.Pow(AzimuthCMDValues[Procindex], 4);
                    sum0YN += SignalMetricValues[Procindex];
                    sum1YN += SignalMetricValues[Procindex] * AzimuthCMDValues[Procindex];
                    sum2YN += SignalMetricValues[Procindex] * Math.Pow(AzimuthCMDValues[Procindex], 2);
                }
                else
                {
                    sum1N += ElevationCMDValues[Procindex];
                    sum2N += Math.Pow(ElevationCMDValues[Procindex], 2);
                    sum3N += Math.Pow(ElevationCMDValues[Procindex], 3);
                    sum4N += Math.Pow(ElevationCMDValues[Procindex], 4);
                    sum0YN += SignalMetricValues[Procindex];
                    sum1YN += SignalMetricValues[Procindex] * ElevationCMDValues[Procindex];
                    sum2YN += SignalMetricValues[Procindex] * Math.Pow(ElevationCMDValues[Procindex], 2);
                }
            }


            // Build M-transpose-M matrix 
            double[,] MTM = new double[3, 3];
            MTM[0, 0] = sum0N;
            MTM[0, 1] = sum1N;
            MTM[0, 2] = sum2N;
            MTM[1, 0] = sum1N;
            MTM[1, 1] = sum2N;
            MTM[1, 2] = sum3N;
            MTM[2, 0] = sum2N;
            MTM[2, 1] = sum3N;
            MTM[2, 2] = sum4N;

            // Build M-transpose-Y vector
            double[,] MTY = new double[3, 1];
            MTY[0, 0] = sum0YN;
            MTY[1, 0] = sum1YN;
            MTY[2, 0] = sum2YN;

            // Calculate Det of MTM
            double detMTM = Det3x3(MTM);

            // Calculate Closed Form Inverse of MTM
            if (detMTM == 0)
            {
                // Error, exit cleanly

                // Set next world coordinate command positions
                wAzCMD = wAz;
                wElCMD = wEl;
            }
            else
            {
                // Calculate Inverse
                double[,] MTMInv = Inv3x3(MTM);

                // Multiply Inverse by MTY
                double[,] vStar = MatProd(MTMInv, MTY);

                // Set next world coordinate command positions
                if (processAzimuth)
                    wAzCMD = -vStar[1, 0] / (2 * vStar[2, 0]);
                else
                    wElCMD = -vStar[1, 0] / (2 * vStar[2, 0]);

            }
        }

        // Convert from wSpherical Coordinates to wCartesian
        void Spher2Cart()
        {
            wX = Math.Cos(wAzCMD) * Math.Cos(wElCMD);
            wY = Math.Sin(wAzCMD) * Math.Cos(wElCMD);
            wZ = Math.Sin(wElCMD);
        }
        // Convert from pCartesian Coordinates to pSpherical
        void Cart2Sphere()
        {
            if (pY < 0 && pZ >= 0)
            {
                pAz = -Math.Acos(pX / Math.Sqrt(pX * pX + pY * pY));
                pEl = Math.Acos(Math.Sqrt(pX * pX + pY * pY));
            }
            else if (pZ < 0 && pY >= 0)
            {
                pAz = Math.Acos(pX / Math.Sqrt(pX * pX + pY * pY));
                pEl = -Math.Acos(Math.Sqrt(pX * pX + pY * pY));
            }
            else if (pZ < 0 && pY < 0)
            {
                pAz = -Math.Acos(pX / Math.Sqrt(pX * pX + pY * pY));
                pEl = -Math.Acos(Math.Sqrt(pX * pX + pY * pY));
            }
            else
            {
                pAz = Math.Acos(pX / Math.Sqrt(pX * pX + pY * pY));
                pEl = Math.Acos(Math.Sqrt(pX * pX + pY * pY));
            }

            // if scanning in pedestal coordinates, apply offsets
            if (scanInPad)
            {
                pAzCMD = pAz + AzOffset;
                pElCMD = pEl + ElOffset;
            }
            else
            {
                pAzCMD = pAz;
                pElCMD = pEl;
            }

        }
        // Transform wCart vector to pCart vector via Rotation Matrix
        void World2Ped()
        {
            pX = wX * (C_baseYaw * C_basePitch) + wY * (C_baseYaw * S_basePitch * S_baseRoll + C_baseRoll * S_baseYaw) + wZ * (S_baseYaw * S_baseRoll - C_baseYaw * S_basePitch * C_baseRoll);
            pY = wX * (-S_baseYaw * C_basePitch) + wY * (C_baseYaw * C_baseRoll - S_baseYaw * S_basePitch * S_baseRoll) + wZ * (S_baseYaw * S_basePitch * C_baseRoll + C_baseYaw * S_baseRoll);
            pZ = wX * (S_basePitch) + wY * (-C_basePitch * S_baseRoll) + wZ * (C_basePitch * C_baseRoll);

            // pX = wX * (C_baseYaw * C_basePitch) + wY * (C_baseYaw * S_basePitch * S_baseRoll - C_baseRoll * S_baseYaw) + wZ * (S_baseYaw * S_baseRoll + C_baseYaw * S_basePitch * C_baseRoll);
            // pY = wX * (S_baseYaw * C_basePitch) + wY * (C_baseYaw * C_baseRoll + S_baseYaw * S_basePitch * S_baseRoll) + wZ * (S_baseYaw * S_basePitch * C_baseRoll - C_baseYaw * S_baseRoll);
            // pZ = wX * (-S_basePitch) + wY * (C_basePitch * S_baseRoll) + wZ * (C_basePitch * C_baseRoll);
        }

        // Calculate and Return Determinant of 3x3 matrix
        double Det3x3(double[,] Matrix3x3)
        {
            // Bounds check input, must be 3x3
            if (3 != Matrix3x3.GetLength(0) && Matrix3x3.GetLength(0) != Matrix3x3.GetLength(1))
                return Double.NaN;

            // Create some intermediate variables to make code more readable
            double b11 = Matrix3x3[0, 0];
            double b12 = Matrix3x3[0, 1];
            double b13 = Matrix3x3[0, 2];
            double b21 = Matrix3x3[1, 0];
            double b22 = Matrix3x3[1, 1];
            double b23 = Matrix3x3[1, 2];
            double b31 = Matrix3x3[2, 0];
            double b32 = Matrix3x3[2, 1];
            double b33 = Matrix3x3[2, 2];

            // Return Closed form solution of det3x3
            return b11 * b22 * b33 + b21 * b32 * b13 + b31 * b12 * b23 - b11 * b32 * b23 - b31 * b22 * b13 - b21 * b12 * b33;
        }

        // Calculate and Return Determinant of 3x3 matrix
        double[,] Inv3x3(double[,] Matrix3x3)
        {
            // Bounds Check the Inputs
            double detinputmat = Det3x3(Matrix3x3);
            if (detinputmat == 0.0)
                return null;

            // Create the output matrix to return
            double[,] Inv3x3 = new double[3, 3];

            // Create some intermediate variables to make code more readable 
            double c11 = Matrix3x3[0, 0];
            double c12 = Matrix3x3[0, 1];
            double c13 = Matrix3x3[0, 2];
            double c21 = Matrix3x3[1, 0];
            double c22 = Matrix3x3[1, 1];
            double c23 = Matrix3x3[1, 2];
            double c31 = Matrix3x3[2, 0];
            double c32 = Matrix3x3[2, 1];
            double c33 = Matrix3x3[2, 2];

            // Calculate each matrix entry
            Inv3x3[0, 0] = (c22 * c33 - c23 * c32) / detinputmat;
            Inv3x3[0, 1] = (c13 * c32 - c12 * c33) / detinputmat;
            Inv3x3[0, 2] = (c12 * c23 - c13 * c22) / detinputmat;
            Inv3x3[1, 0] = (c23 * c31 - c21 * c33) / detinputmat;
            Inv3x3[1, 1] = (c11 * c33 - c13 * c31) / detinputmat;
            Inv3x3[1, 2] = (c13 * c21 - c11 * c23) / detinputmat;
            Inv3x3[2, 0] = (c21 * c32 - c22 * c31) / detinputmat;
            Inv3x3[2, 1] = (c12 * c31 - c11 * c32) / detinputmat;
            Inv3x3[2, 2] = (c11 * c22 - c12 * c21) / detinputmat;

            // Return Matrix Inverse
            return Inv3x3;
        }

        // Calculate and Return Matrix Product of two 3x3 Matracies
        double[,] MatProd3x3(double[,] MatrixLeft, double[,] MatrixRight)
        {
            // Bounds check input, must be 3x3

            if (3 != MatrixLeft.GetLength(0) && MatrixLeft.GetLength(0) != MatrixLeft.GetLength(1))
                return null;
            if (3 != MatrixRight.GetLength(0) && MatrixRight.GetLength(0) != MatrixRight.GetLength(1))
                return null;

            // Create the output matrix to return
            double[,] MatProd = new double[3, 3];

            // Calculate Entries
            MatProd[0, 0] = MatrixLeft[0, 0] * MatrixRight[0, 0] + MatrixLeft[0, 1] * MatrixRight[1, 0] + MatrixLeft[0, 2] * MatrixRight[2, 0];
            MatProd[0, 1] = MatrixLeft[0, 0] * MatrixRight[0, 1] + MatrixLeft[0, 1] * MatrixRight[1, 1] + MatrixLeft[0, 2] * MatrixRight[2, 1];
            MatProd[0, 2] = MatrixLeft[0, 0] * MatrixRight[0, 2] + MatrixLeft[0, 1] * MatrixRight[1, 2] + MatrixLeft[0, 2] * MatrixRight[2, 2];
            MatProd[1, 0] = MatrixLeft[1, 0] * MatrixRight[1, 0] + MatrixLeft[1, 1] * MatrixRight[1, 0] + MatrixLeft[1, 2] * MatrixRight[2, 0];
            MatProd[1, 1] = MatrixLeft[1, 0] * MatrixRight[1, 1] + MatrixLeft[1, 1] * MatrixRight[1, 1] + MatrixLeft[1, 2] * MatrixRight[2, 1];
            MatProd[1, 2] = MatrixLeft[1, 0] * MatrixRight[1, 2] + MatrixLeft[1, 1] * MatrixRight[1, 2] + MatrixLeft[1, 2] * MatrixRight[2, 2];
            MatProd[2, 0] = MatrixLeft[2, 0] * MatrixRight[0, 0] + MatrixLeft[2, 1] * MatrixRight[1, 0] + MatrixLeft[2, 2] * MatrixRight[2, 0];
            MatProd[2, 1] = MatrixLeft[2, 0] * MatrixRight[0, 1] + MatrixLeft[2, 1] * MatrixRight[1, 1] + MatrixLeft[2, 2] * MatrixRight[2, 1];
            MatProd[2, 2] = MatrixLeft[2, 0] * MatrixRight[0, 2] + MatrixLeft[2, 1] * MatrixRight[1, 2] + MatrixLeft[2, 2] * MatrixRight[2, 2];

            // Return Matrix Product
            return MatProd;
        }

        // Calculate and Return Matrix Product of two Matracies
        double[,] MatProd(double[,] MatrixLeft, double[,] MatrixRight)
        {
            // Bounds Check
            if (MatrixLeft.Rank != 2 || MatrixLeft.Rank != 2)
                return null;
            if (MatrixLeft.GetLength(1) != MatrixRight.GetLength(0))
                return null;

            // Create the output matrix to return
            double[,] MatProd = new double[MatrixLeft.GetLength(0), MatrixRight.GetLength(1)];

            // Calculate matrix product
            for (int i = 0; i < MatProd.GetLength(0); i++)
            {
                for (int j = 0; j < MatProd.GetLength(1); j++)
                {
                    // For each element in the output matrix,
                    // calculate entry from row and column numbers
                    MatProd[i, j] = 0;
                    for (int k = 0; k < MatProd.GetLength(1); k++)
                        MatProd[i, j] += MatrixLeft[i, k] * MatrixRight[k, j];

                }
            }
            // Return matrix product
            return MatProd;
        }
    }

    // The FLIR prototype is built using several self contained classes
    public enum FeatherLightCtrlStates { PowerOn, Error, FullInit, PartialInit }
    public enum DeviceExeStates { Initialize, ReadStatus, WriteCommands }
    public enum CoordinateFrames { WorldCoords, PedestalCoords }
    public enum ControlStates { PowerOn, Error, PartialInit, FullInit, Ready, Auto, Manual }
    public enum ScanType { None, Spiral, Step }
    public enum AxisSelect { Azimuth, Elevation, Polarization }
    public enum SetSwitchPosition { NomSat, User2, Stow, CalcPeak, User1, LastPeak }
    public enum AutoAcquisitionStates { Ready, Error, Nominal, ModemLock, StepAz, StepEl, Peaked }
    public enum HillClimbStates { Nominal, FindUP, FindDOWN, RoughFit, FindEND, FineFit }
    public struct ScanSettings
    {
        public CoordinateFrames CoordFrameSelect;      // World or Pedestal Coord Selection
        public double MaximumRadius;                   // Maximum Spiral Radius in Deg
        public double DeltaRadius;                     // Radial Spacing of Spiral in Deg
        public double DeltaTheta;                      // Angular Spacing of samples along spiral in Rad
        public double DwellTime;                       // Seconds to Dwell 
        public double StartTheta;                      // Initial Angle
    }
    public struct StepSettings
    {
        public CoordinateFrames CoordFrameSelect;      // World or Pedestal Coord Selection
        public double MaximumTheta;                    // Maximum cummulative step distance in Rad
        public double DeltaTheta;                      // Angular Spacing of samples along hill climb in Rad
        public double DwellTime;                       // Seconds to Dwell
        public double DirectionSign;
        public AxisSelect StepAxis;                    // Axis of Step
    }
    public struct SatelliteInfo
    {
        public double NomSatAz, NomSatEl;          // Nominal Satellite Look Angles
        public double TrackingFrequency;           // Frequency of tracking signal in Hz for metric determination
    }
    public struct AntennaInfo
    {
        public double RefDiameter;                 // Reflector Diameter
        public double BaseRoll, BasePitch, BaseYaw;// Base Frame rotation from Fixed World Frame
    }
    public struct BeamShapeInfo
    {
        public double Frequency;            // Frequency of Electromagnetic Wave (hz)
        public double Diameter;             // Diameter of parabolic reflector (m) 
        public static double C = 2.99e8;    // Meters per second, speed of light (m/s)
        public static double K = 70;        // Curvature Constant, from empirical data, from the internet, for parabolic reflectors 
        public double beta;                 // Final 2nd order coefficient for negative parabola representing signal drop in dB
        public double HPBeamWidth;          // Resulting -3dB BeamWidth (deg) 
        public double Dist2Peak;            // Simulated Radial Error (deg)
        public double PeakMetric;           // Last latch peak signal metric (dB)
        public double NoiseFloorSim;        // Simulated Noise Floor Level (dB)
        public double NoiseDelta;           // Random Number Multiplier (dB)
        public Random NoiseRandSim;         // Simulated Noise (random not AWGN) (dB)
        public double RxLockThresh;         // Simulated Rx Lock Threshold above noise floor (dB)
        public double SpoofActualAz;        // Simulated Peak Azimuth Position
        public double SpoofActualEl;        // Simulated Peak Elevation Position
        public double WorldFeedbackAz;      // Simulated Azimuth Feedback Angles
        public double WorldFeedbackEl;      // Simulated Elevation Feedback Angles
    }
    public static class Matrix3x3
    {
        // Calculate and Return Matrix Product of two 3x3 Matracies
        public static double[,] MatProd3x3(double[,] MatrixLeft, double[,] MatrixRight)
        {
            // Bounds check input, must be 3x3

            if (3 != MatrixLeft.GetLength(0) || 3 != MatrixLeft.GetLength(1))
                return null;
            if (3 != MatrixRight.GetLength(0) || 3 != MatrixRight.GetLength(1))
                return null;

            // Create the output matrix to return
            double[,] MatProd = new double[3, 3];

            // Calculate Entries
            MatProd[0, 0] = MatrixLeft[0, 0] * MatrixRight[0, 0] + MatrixLeft[0, 1] * MatrixRight[1, 0] + MatrixLeft[0, 2] * MatrixRight[2, 0];
            MatProd[0, 1] = MatrixLeft[0, 0] * MatrixRight[0, 1] + MatrixLeft[0, 1] * MatrixRight[1, 1] + MatrixLeft[0, 2] * MatrixRight[2, 1];
            MatProd[0, 2] = MatrixLeft[0, 0] * MatrixRight[0, 2] + MatrixLeft[0, 1] * MatrixRight[1, 2] + MatrixLeft[0, 2] * MatrixRight[2, 2];
            MatProd[1, 0] = MatrixLeft[1, 0] * MatrixRight[1, 0] + MatrixLeft[1, 1] * MatrixRight[1, 0] + MatrixLeft[1, 2] * MatrixRight[2, 0];
            MatProd[1, 1] = MatrixLeft[1, 0] * MatrixRight[1, 1] + MatrixLeft[1, 1] * MatrixRight[1, 1] + MatrixLeft[1, 2] * MatrixRight[2, 1];
            MatProd[1, 2] = MatrixLeft[1, 0] * MatrixRight[1, 2] + MatrixLeft[1, 1] * MatrixRight[1, 2] + MatrixLeft[1, 2] * MatrixRight[2, 2];
            MatProd[2, 0] = MatrixLeft[2, 0] * MatrixRight[0, 0] + MatrixLeft[2, 1] * MatrixRight[1, 0] + MatrixLeft[2, 2] * MatrixRight[2, 0];
            MatProd[2, 1] = MatrixLeft[2, 0] * MatrixRight[0, 1] + MatrixLeft[2, 1] * MatrixRight[1, 1] + MatrixLeft[2, 2] * MatrixRight[2, 1];
            MatProd[2, 2] = MatrixLeft[2, 0] * MatrixRight[0, 2] + MatrixLeft[2, 1] * MatrixRight[1, 2] + MatrixLeft[2, 2] * MatrixRight[2, 2];

            // Return Matrix Product
            return MatProd;
        }
        // Calculate and Return Matrix Product of a 3x3 Matrix and 3x1 Vector
        public static double[,] Mat3x3Vect3x1Prod(double[,] MatrixLeft, double[,] VectorRight)
        {
            // Bounds check input, must be 3x3

            if (3 != MatrixLeft.GetLength(0) || 3 != MatrixLeft.GetLength(1))
                return null;
            if (3 != VectorRight.GetLength(0) || 1 != VectorRight.GetLength(1))
                return null;

            // Create the output matrix to return
            double[,] MatProd = new double[3, 1];

            // Calculate Entries
            MatProd[0, 0] = MatrixLeft[0, 0] * VectorRight[0, 0] + MatrixLeft[0, 1] * VectorRight[1, 0] + MatrixLeft[0, 2] * VectorRight[2, 0];            
            MatProd[1, 0] = MatrixLeft[1, 0] * VectorRight[0, 0] + MatrixLeft[1, 1] * VectorRight[1, 0] + MatrixLeft[1, 2] * VectorRight[2, 0];            
            MatProd[2, 0] = MatrixLeft[2, 0] * VectorRight[0, 0] + MatrixLeft[2, 1] * VectorRight[1, 0] + MatrixLeft[2, 2] * VectorRight[2, 0];            

            // Return Matrix Product
            return MatProd;
        }
        // Calculate and Return Determinant of 3x3 matrix
        public static double Det3x3(double[,] Matrix3x3)
        {
            // Bounds check input, must be 3x3
            if (3 != Matrix3x3.GetLength(0) && Matrix3x3.GetLength(0) != Matrix3x3.GetLength(1))
                return Double.NaN;

            // Create some intermediate variables to make code more readable
            double b11 = Matrix3x3[0, 0];
            double b12 = Matrix3x3[0, 1];
            double b13 = Matrix3x3[0, 2];
            double b21 = Matrix3x3[1, 0];
            double b22 = Matrix3x3[1, 1];
            double b23 = Matrix3x3[1, 2];
            double b31 = Matrix3x3[2, 0];
            double b32 = Matrix3x3[2, 1];
            double b33 = Matrix3x3[2, 2];

            // Return Closed form solution of det3x3
            return b11 * b22 * b33 + b21 * b32 * b13 + b31 * b12 * b23 - b11 * b32 * b23 - b31 * b22 * b13 - b21 * b12 * b33;
        }
        // Calculate and Return Determinant of 3x3 matrix
        public static double[,] Inv3x3(double[,] Matrix3x3)
        {
            // Bounds Check the Inputs
            double detinputmat = Det3x3(Matrix3x3);
            if (detinputmat == 0.0)
                return null;

            // Create the output matrix to return
            double[,] Inv3x3 = new double[3, 3];

            // Create some intermediate variables to make code more readable 
            double c11 = Matrix3x3[0, 0];
            double c12 = Matrix3x3[0, 1];
            double c13 = Matrix3x3[0, 2];
            double c21 = Matrix3x3[1, 0];
            double c22 = Matrix3x3[1, 1];
            double c23 = Matrix3x3[1, 2];
            double c31 = Matrix3x3[2, 0];
            double c32 = Matrix3x3[2, 1];
            double c33 = Matrix3x3[2, 2];

            // Calculate each matrix entry
            Inv3x3[0, 0] = (c22 * c33 - c23 * c32) / detinputmat;
            Inv3x3[0, 1] = (c13 * c32 - c12 * c33) / detinputmat;
            Inv3x3[0, 2] = (c12 * c23 - c13 * c22) / detinputmat;
            Inv3x3[1, 0] = (c23 * c31 - c21 * c33) / detinputmat;
            Inv3x3[1, 1] = (c11 * c33 - c13 * c31) / detinputmat;
            Inv3x3[1, 2] = (c13 * c21 - c11 * c23) / detinputmat;
            Inv3x3[2, 0] = (c21 * c32 - c22 * c31) / detinputmat;
            Inv3x3[2, 1] = (c12 * c31 - c11 * c32) / detinputmat;
            Inv3x3[2, 2] = (c11 * c22 - c12 * c21) / detinputmat;

            // Return Matrix Inverse
            return Inv3x3;
        }
        // Process scan data for parabolic fit to find center
        public static double ProcessScanData(List<double> SignalMetricValues, List<double> AxisFeedbackValues)
        {
            // Calculate Required Intermediate Values
            double sum0N = 0, sum1N = 0, sum2N = 0, sum3N = 0, sum4N = 0, sum0YN = 0, sum1YN = 0, sum2YN = 0;
            for (int Procindex = 0; Procindex < SignalMetricValues.Count; Procindex++)
            {
                // Zero Order Sum = N
                sum0N++;

                sum1N += AxisFeedbackValues[Procindex];
                sum2N += Math.Pow(AxisFeedbackValues[Procindex], 2);
                sum3N += Math.Pow(AxisFeedbackValues[Procindex], 3);
                sum4N += Math.Pow(AxisFeedbackValues[Procindex], 4);
                sum0YN += SignalMetricValues[Procindex];
                sum1YN += SignalMetricValues[Procindex] * AxisFeedbackValues[Procindex];
                sum2YN += SignalMetricValues[Procindex] * Math.Pow(AxisFeedbackValues[Procindex], 2);

            }

            // Build M-transpose-M matrix 
            double[,] MTM = new double[3, 3];
            MTM[0, 0] = sum0N;
            MTM[0, 1] = sum1N;
            MTM[0, 2] = sum2N;
            MTM[1, 0] = sum1N;
            MTM[1, 1] = sum2N;
            MTM[1, 2] = sum3N;
            MTM[2, 0] = sum2N;
            MTM[2, 1] = sum3N;
            MTM[2, 2] = sum4N;

            // Build M-transpose-Y vector
            double[,] MTY = new double[3, 1];
            MTY[0, 0] = sum0YN;
            MTY[1, 0] = sum1YN;
            MTY[2, 0] = sum2YN;
            
            // Calculate Closed Form Inverse of MTM
            double[,] MTMInv = Inv3x3(MTM);

            if(MTMInv!=null)
            {
                // Multiply Inverse by MTY
                double[,] vStar = Mat3x3Vect3x1Prod(MTMInv, MTY);

                // Return the calculated center angle
                return -vStar[1, 0] / (2 * vStar[2, 0]);
            }
            else
            {
                // Error, exit cleanly
                return double.NaN;
            }

            

            
        }
    }






    //public class KinematicXform:BaseModelClass
    //{
    //    #region Properties exposing private members of PositionPlanning Class
    //    [Category("Postion Inputs"), Description("World Coordinate Azimuth Look Angle (deg)")]
    //    public double WorldAzimuthCommand
    //    {
    //        get { return WorldAzimuthCMD * 180 / Math.PI; }
    //        set { WorldAzimuthCMD = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Inputs"), Description("World Coordinate Elevation Look Angle (deg)")]
    //    public double WorldElevationCommand
    //    {
    //        get { return WorldElevationCMD * 180 / Math.PI; }
    //        set { WorldElevationCMD = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Inputs"), Description("Pedestal Coordinate Azimuth Feedback Angle (deg)")]
    //    public double PedestalAzimuthFeedback
    //    {
    //        get { return PedestalAzFBK * 180 / Math.PI; }
    //        set { PedestalAzFBK = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Inputs"), Description("Pedestal Coordinate Elevation Feedback Angle (deg)")]
    //    public double PedestalElevationFeedback
    //    {
    //        get { return PedestalElFBK * 180 / Math.PI; }
    //        set { PedestalElFBK = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Inputs"), Description("Pedestal Coordinate Azimuth Offset/Augmentation Angle (deg)")]
    //    public double PedestalAzimuthOffset
    //    {
    //        get { return PedestalOffsetAz * 180 / Math.PI; }
    //        set { PedestalOffsetAz = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Inputs"), Description("Pedestal Coordinate Elevation Offset/Augmentation Angle (deg)")]
    //    public double PedestalElevationOffset
    //    {
    //        get { return PedestalOffsetEl * 180 / Math.PI; }
    //        set { PedestalOffsetEl = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Outputs"), Description("Pedestal Coordinate Azimuth Look Angle (deg)"), ReadOnly(true)]
    //    public double PedestalAzimuthCommand
    //    {
    //        get { return PedestalAzimuthCMD * 180 / Math.PI; }
    //        set { PedestalAzimuthCMD = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Outputs"), Description("Pedestal Coordinate Elevation Look Angle (deg)"), ReadOnly(true)]
    //    public double PedestalElevationCommand
    //    {
    //        get { return PedestalElevationCMD * 180 / Math.PI; }
    //        set { PedestalElevationCMD = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Outputs"), Description("Pedestal Coordinate Azimuth Look Angle (deg)"), ReadOnly(true)]
    //    public double WorldAzimuthFeedback
    //    {
    //        get { return WorldAzFBK * 180 / Math.PI; }
    //        set { WorldAzFBK = value * Math.PI / 180; }
    //    }
    //    [Category("Postion Outputs"), Description("Pedestal Coordinate Elevation Look Angle (deg)"), ReadOnly(true)]
    //    public double WorldElevationFeedback
    //    {
    //        get { return WorldElFBK * 180 / Math.PI; }
    //        set { WorldElFBK = value * Math.PI / 180; }
    //    }
    //    #endregion
    //    #region Private Members of the PositionPlanning Class
    //    double WorldAzimuthCMD, WorldElevationCMD;              // World Coordinate Look Angle Commands
    //    double PedestalAzimuthCMD, PedestalElevationCMD;        // Pedestal Coordinate Look Angle Commands
    //    double PedestalOffsetAz, PedestalOffsetEl;              // Pedestal Coordinate Offset Augmentations
    //    double PedestalAzFBK, PedestalElFBK;                    // Pedestal Coordinate Feedback Angles
    //    double WorldAzFBK, WorldElFBK;                          // World Coordinate Feedback Angles
    //    #endregion
    //    #region Main Execution System Functions of PositionPlanning Clas
    //    public KinematicXform()
    //    {

    //    }
    //    public override void Initialize()
    //    {

    //    }
    //    public override void Prepare()
    //    {

    //    }
    //    public override void Execute()
    //    {

    //    }
    //    #endregion
    //}



}
