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
    public class FLIRDriver : BaseModelClass
    {
        #region Properties exposing members of FLIRDriver Class
        [property: Category("Comm Link Parameters"), Description("Trigger to Send Commands to FLIR Controller")]
        public bool p_SendCommands
        {
            get { return SendCommands; }
            set { SendCommands = value; }
        }
        [property: Category("Comm Link Parameters"), Description("Execution State of FLIR Driver")]
        public DeviceExeStates p_CommStatus
        {
            get { return CommState; }
            set
            {
                CommState = value;
                this.RePrepare = true;
            }
        }
        [property: Category("Position Commands"), Description("Pan Axis Angle Command (deg)")]
        public double p_PanAngleCMD
        {
            get { return PanAngleCMD * 180 / Math.PI; }
            set
            {
                PanAngleCMD = value * Math.PI / 180;
                this.RePrepare = true;
            }
        }
        [property: Category("Position Commands"), Description("Tilt Axis Angle Command (deg)")]
        public double p_TiltAngleCMD
        {
            get { return TiltAngleCMD * 180 / Math.PI; }
            set
            {
                TiltAngleCMD = value * Math.PI / 180;
                this.RePrepare = true;
            }
        }
        [property: Category("Position Feedback"), Description("Pan Axis Angle (deg)")]
        public double p_PanAngle
        {
            get { return PanAngle * 180 / Math.PI; }
            set
            {
                PanAngle = value * Math.PI / 180;
                this.RePrepare = true;
            }
        }
        [property: Category("Position Feedback"), Description("Tilt Axis Angle (deg)")]
        public double p_TiltAngle
        {
            get { return TiltAngle * 180 / Math.PI; }
            set
            {
                TiltAngle = value * Math.PI / 180;
                this.RePrepare = true;
            }
        }
        #endregion
        #region Members of FLIRDriver Class
        // Driver State Variables
        DeviceExeStates CommState;          // State Variable driving execution() function 
        bool SimulateData;                  // Flag indicating simulation of FLIR data
        bool SendCommands;                  // Trigger to send commands to FLIR Controller
        int CommConnections;                // Counter of Attempted communication attempts
        string FLIRIP;                      // FLIR Controller IP address
        int FLIRPORT;                       // FLIR Controller Port #
        string BResponse;                   // FLIR Return String
        int RetBytes;                       // Number of Response Bytes
        byte[] RespBytes;                   // Array of response data
        int caseCount;                      // State of Parsing Algorithm
        int caseCountLast;                  // Last State of Parsing Algorithm
        string tempString;                  // Temp string for Parsing
        int PanStepsCMD;                    // Pan axis position command in steps
        int PanSteps;                       // Pan axis position feedback in steps
        double PanRes;                      // Pan axis resolution from FLIR Controller (rad per step)
        double PanAngleCMD;                 // Pan axis position command in radians
        double PanAngle;                    // Pan axis position feedback from FLIR Controller
        int PanSpeed;                       // Pan axis speed feedback from FLIR Controller
        int TiltStepsCMD;                   // Tilt axis position command in steps
        int TiltSteps;                      // Tilt axis position feedback in steps
        double TiltRes;                     // Tilt axis resolution from FLIR Controller (rad per step)
        double TiltAngleCMD;                // Tilt axis position command in radians
        double TiltAngle;                   // Tilt axis position feedback from FLIR Controller
        int TiltSpeed;                      // Tilt axis speed feedback from FLIR Controller
        bool NewData;                       // Indication of new feedback data from FLIR Controller
        // Driver Communication Objects
        TcpClient FLIRTCPClient;            // TCP Client socket for FLIR Controller
        SerialPort FLIRComPort;             // RS-232 Comm Port to FLIR Controller
        #endregion
        #region Main Execution System Functions of FLIRDriver Class
        // Constructor   
        public FLIRDriver()
        {
            CommState = DeviceExeStates.Initialize;
            SimulateData = false;
            CommConnections = 0;
            FLIRIP = "10.21.9.207";
            FLIRPORT = 4000;
            FLIRTCPClient = new TcpClient();
            FLIRTCPClient.ReceiveTimeout = 10;
            FLIRTCPClient.SendTimeout = 10;
            FLIRTCPClient.NoDelay = true;
            BResponse = "";
            RespBytes = new byte[1024];
        }
        public override void Initialize()
        {
            if (!(FLIRComPort == null))
            {
                // Destroy it and reset
                FLIRComPort.Close();
                FLIRComPort.Dispose();
            }

            // create the object
            FLIRComPort = new SerialPort();

            // configure the serial port
            FLIRComPort.PortName = "COM9";
            FLIRComPort.BaudRate = 9600;
            FLIRComPort.DataBits = 8;
            FLIRComPort.StopBits = StopBits.One;
            FLIRComPort.Handshake = Handshake.None;
            FLIRComPort.Parity = Parity.None;
            //FLIRComPort.ReadTimeout = 2;
            //FLIRComPort.WriteTimeout = 2;

            // open port
            try
            {
                FLIRComPort.Open();
                // Get Pan and Tilt Resolution
                if (!SendFLIRCommand("PR") || !SendFLIRCommand("TR"))
                {
                    this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "FLIR Driver Entering Simulation Mode");
                    PanRes = 0.012857 * Math.PI / 180;
                    TiltRes = -PanRes;
                    SimulateData = true;
                }
                else
                {
                    SimulateData = false;
                    this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "FLIR Driver Connected via RS232");
                }

            }
            catch (TimeoutException tex)
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, tex.Message);
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "FLIR Driver Entering Simulation Mode");
                PanRes = 0.012857 * Math.PI / 180;
                TiltRes = -PanRes;
                SimulateData = true;
            }
            catch (Exception ex)
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, ex.Message);
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "FLIR Driver Entering Simulation Mode");
                PanRes = 0.012857 * Math.PI / 180;
                TiltRes = -PanRes;
                SimulateData = true;

            }


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
                        // Send Position and Speed Query
                        NewData = SendFLIRCommand("B");

                    }
                    else
                    {
                        PanSteps = PanStepsCMD;
                        TiltSteps = TiltStepsCMD;
                        PanAngle = PanSteps * PanRes;
                        TiltAngle = TiltSteps * TiltRes;
                    }
                    CommState = DeviceExeStates.WriteCommands;
                    break;
                case DeviceExeStates.WriteCommands:
                    // Only send commands when instructed by control system
                    if (SendCommands)
                    {
                        // prepare values for commanding to FLIR
                        PanStepsCMD = (int)Math.Round(PanAngleCMD / PanRes);
                        TiltStepsCMD = (int)Math.Round(TiltAngleCMD / TiltRes);
                        if (!SimulateData)
                        {
                            // Send Position/Speed Command to FLIR 
                            SendCommands = !SendFLIRCommand("B" + PanStepsCMD.ToString() + "," + TiltStepsCMD.ToString() + "," + (1100).ToString() + "," + (1100).ToString());
                        }
                        else
                        {
                            // Clear Send Flag
                            SendCommands = false;
                        }
                        // Clear Send Flag
                        SendCommands = false;

                    }
                    CommState = DeviceExeStates.ReadStatus;
                    break;
            }
        }
        #endregion
        #region Subfunctions of FLIRDriver Class
        public bool SendFLIRCommand(string CommandString)
        {
            // Send Command String
            FLIRComPort.WriteLine(CommandString);

            // Read Response
            try
            {
                BResponse = "";
                BResponse = FLIRComPort.ReadLine();
                BResponse += FLIRComPort.ReadLine();

                if (CommandString == "PR")
                {
                    BResponse = BResponse.TrimStart("PR* ".ToCharArray());
                    #region Parse FLIR Response
                    // Loop through all characters
                    tempString = "";
                    foreach (char flirChar in BResponse)
                    {
                        if (flirChar.CompareTo(" ".ToCharArray()[0]) == 0)
                        {
                            PanRes = Convert.ToDouble(tempString);
                            PanRes = PanRes * Math.PI / (3600 * 180);           // convert to rad per step
                            return true;
                        }
                        else
                            tempString += flirChar;
                    }
                    #endregion
                }
                else if (CommandString == "TR")
                {
                    BResponse = BResponse.TrimStart("TR* ".ToCharArray());
                    #region Parse FLIR Response
                    // Loop through all characters
                    tempString = "";
                    foreach (char flirChar in BResponse)
                    {
                        if (flirChar.CompareTo(" ".ToCharArray()[0]) == 0)
                        {
                            TiltRes = Convert.ToDouble(tempString);
                            TiltRes = TiltRes * Math.PI / (3600 * 180);           // convert to rad per step
                            return true;
                        }
                        else
                            tempString += flirChar;
                    }
                    #endregion
                }
                else if (CommandString == "B")
                {
                    BResponse = BResponse.TrimStart("B *P(".ToCharArray());
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

                            // Parse Pan
                            case 1:

                                if (flirChar.CompareTo(",".ToCharArray()[0]) == 0)
                                {
                                    PanSteps = Convert.ToInt32(tempString);
                                    PanAngle = PanSteps * PanRes;
                                    tempString = "";
                                    caseCount++;
                                }
                                else
                                    tempString += flirChar;
                                break;

                            // Parse Tilt
                            case 2:

                                if (flirChar.CompareTo(")".ToCharArray()[0]) == 0)
                                {
                                    TiltSteps = Convert.ToInt32(tempString);
                                    TiltAngle = TiltSteps * TiltRes;
                                    tempString = "";
                                    caseCount++;
                                }
                                else
                                    tempString += flirChar;
                                break;

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
                    return true;
                }
                else if (CommandString.StartsWith("B"))
                {
                    BResponse = BResponse.TrimStart("B".ToCharArray());
                    // ToDO, add check for command acceptance
                    return true;
                }
                return true;

            }
            catch (TimeoutException tex)
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, tex.Message);
                return false;
            }
            catch (Exception ex)
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, ex.Message);
                return false;
            }


        }
        #endregion
    }
}

