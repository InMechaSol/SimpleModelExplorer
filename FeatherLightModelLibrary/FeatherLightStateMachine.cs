using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    public class FeatherLightStateMachine : BaseModelClass
    {
        #region Properties exposing members of FeatherLightStateMachine Class
        [Category("Internal Loops"), Description("Position Planning Loop, where world coordinate position commands are calculated")]
        public PositionPlanning PositionPlanningLoop
        {
            get { return PlanningLoop; }
            set { PlanningLoop = value; }
        }
        [Category("Internal Loops"), Description("Coordinate Transform Loop, where conversions are made of commands and feedback")]
        public KinematicXform TransformLoop
        {
            get { return XformLoop; }
            set { XformLoop = value; }
        }
        [Category("External Devices"), Description("Modem Driver")]
        public ModemDriver LinktoModem
        {
            get { return LinktoModemDriver; }
            set { LinktoModemDriver = value; }
        }
        [Category("External Devices"), Description("FLIR Stepper Driver")]
        public FLIRDriver LinktoFLIR
        {
            get { return LinktoFLIRDriver; }
            set { LinktoFLIRDriver = value; }
        }
        [Category("External Devices"), Description("CAPIII Driver")]
        public AndroidDriver LinktoCAPIII
        {
            get { return LinktoAndroidDriver; }
            set { LinktoAndroidDriver = value; }
        }
        [Category("State Machine Status"), Description("Current State of Machine")]
        public ControlStates CurrentControlState
        {
            get { return ControllerState; }
            set { ControllerState = value; }
        }
        #endregion
        #region Members of FeatherLightStateMachine
        KinematicXform XformLoop;
        PositionPlanning PlanningLoop;
        ControlStates ControllerState;
        ModemDriver LinktoModemDriver;
        FLIRDriver LinktoFLIRDriver;
        AndroidDriver LinktoAndroidDriver;
        bool SimulatingModem, SimulatingFeedback;
        double LastAzPedFBK, LastElPEdFBK;
        #endregion
        #region Main Execution System Functions of FeatherLightStateMachine
        public FeatherLightStateMachine()
        {
            LinktoModemDriver = new ModemDriver();
            LinktoFLIRDriver = new FLIRDriver();
            LinktoAndroidDriver = new AndroidDriver();

            PlanningLoop = new PositionPlanning(LinktoModemDriver);
            PlanningLoop.ModelsNotExpanded.Add(LinktoModemDriver);
            XformLoop = new KinematicXform();
            SimulatingModem = true;
            SimulatingFeedback = true;

            // Setup Submodels List
            this.LocalSubModelList = new List<BaseModelClass>();
            this.LocalSubModelList.Add(LinktoModemDriver);
            this.LocalSubModelList.Add(LinktoFLIRDriver);
            this.LocalSubModelList.Add(LinktoAndroidDriver);
            this.LocalSubModelList.Add(PlanningLoop);
            this.LocalSubModelList.Add(XformLoop);

        }
        public override void Initialize()
        {
            
        }
        public override void Prepare()
        {
            
        }
        public override void Execute()
        {
            // Run State Machine
            switch (ControllerState)
            {
                case ControlStates.PowerOn: break;
                case ControlStates.PartialInit: break;
                case ControlStates.FullInit: break;
                case ControlStates.Ready: break;
                case ControlStates.Auto: break;
                case ControlStates.Manual: break;
                case ControlStates.Error: break;
            }
            // 
            PlanningLoop.WorldFeedbackAzimuth = XformLoop.WorldAzimuthFeedback;
            PlanningLoop.WorldFeedbackElevation = XformLoop.WorldElevationFeedback;
            if (SimulatingModem)
            {
                LinktoModemDriver.SpoofFeedbackAzimuth = PlanningLoop.WorldFeedbackAzimuth;
                LinktoModemDriver.SpoofFeedbackElevation = PlanningLoop.WorldFeedbackElevation;
                LinktoModemDriver.Execute();
            }
            // Run Planning Loop
            //PlanningLoop.Execute();
            // Transfer Planning Angle Commands to Xform Class Inputs
            XformLoop.WorldAzimuthCommand = PlanningLoop.CommandedWorldAzimuth;
            XformLoop.WorldElevationCommand = PlanningLoop.CommandedWorldElevation;
            // Run the Kinematic Xform
            //XformLoop.Execute();

            if (SimulatingFeedback)
            {
                XformLoop.PedestalAzimuthFeedback = LastAzPedFBK;
                XformLoop.PedestalElevationFeedback = LastElPEdFBK;
                LastAzPedFBK = XformLoop.PedestalAzimuthCommand;
                LastElPEdFBK = XformLoop.PedestalElevationCommand;
            }
        }
        #endregion
    }
}
