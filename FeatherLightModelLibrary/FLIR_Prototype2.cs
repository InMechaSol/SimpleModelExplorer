using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    // The new controller
    public class FLIR_Prototype2 : BaseModelClass
    {
        #region Members of the Prototype Class
        // External Device Objects
        FLIRDriver FLIRController;
        AndroidDriver CAPIIIController;
        ModemDriver ModemController;
        // Control System Object
        FeatherLightStateMachine AntennaController;
        #endregion
        #region Properties exposing Members and Submembers of ProtoType Class
        [property: Category("Devices"), Description("Modem Device Driver")]
        public ModemDriver ModemControl
        {
            get { return this.ModemController; }
            set { this.ModemController = value; }
        }
        [property: Category("Devices"), Description("FLIR Pan/Tilt Device Driver")]
        public FLIRDriver FLIRControl
        {
            get { return this.FLIRController; }
            set { this.FLIRController = value; }
        }
        [property: Category("Devices"), Description("CAPIII Device Driver")]
        public AndroidDriver CAPControl
        {
            get { return this.CAPIIIController; }
            set { this.CAPIIIController = value; }
        }
        [property: Category("Controller"), Description("FeatherLight Control System with FLIR Pan/Tilt Controller")]
        public FeatherLightStateMachine AntennaControl
        {
            get { return this.AntennaController; }
            set { this.AntennaController = value; }
        }
        #endregion
        #region Main Execution System Functions of ProtoType Class
        public FLIR_Prototype2()
        {
            // Instantiate Members
            FLIRController = new FLIRDriver();
            CAPIIIController = new AndroidDriver();
            ModemController = new ModemDriver();
            AntennaController = new FeatherLightStateMachine();

            // Setup Submodels List
            this.LocalSubModelList = new List<BaseModelClass>();
            this.LocalSubModelList.Add(CAPIIIController);
            this.LocalSubModelList.Add(ModemController);
            this.LocalSubModelList.Add(FLIRController);
            this.LocalSubModelList.Add(AntennaController);

        }
        public override void BuildLocalExeMap(UniverseTimer exeUtime, ModelMessenger exeModMess)
        {
            // first build the default linear aligned with submodel list
            base.BuildLocalExeMap(exeUtime, exeModMess);
            // then add a write cycle for the devices
            this.LocalModelExeMap.Add(FLIRController);
            this.LocalModelExeMap.Add(ModemController);
            this.LocalModelExeMap.Add(CAPIIIController);
        }
        public override void Initialize()
        {
            CAPIIIController.Initialize();
            FLIRController.Initialize();
            ModemController.Initialize();
            AntennaController.Initialize();
        }
        public override void Prepare()
        {
            CAPIIIController.Prepare();
            FLIRController.Prepare();
            ModemController.Prepare();
            AntennaController.Prepare();
        }
        public override void Execute()
        {
            // Setup Inputs to Devices
            ModemController.SpoofFeedbackAzimuth = AntennaController.TransformLoop.WorldAzimuthFeedback;
            ModemController.SpoofFeedbackElevation = AntennaController.TransformLoop.WorldElevationFeedback;

            // Read Cycle
            CAPIIIController.Execute();
            ModemController.Execute();
            FLIRController.Execute();

            // Setup Inputs to AntennaController
            AntennaController.TransformLoop.PedestalAzimuthFeedback = FLIRController.p_PanAngle;
            AntennaController.TransformLoop.PedestalElevationFeedback = FLIRController.p_TiltAngle;

            // Execute Cycle
            AntennaController.Execute();

            // Setup Inputs to Devices
            FLIRController.p_PanAngleCMD = AntennaController.TransformLoop.PedestalAzimuthCommand;
            FLIRController.p_TiltAngleCMD = AntennaController.TransformLoop.PedestalElevationCommand;
            //FLIRController.p_SendCommands = AntennaController.PositionPlanningLoop.Trigger2SendCMDs;

            // Write Cycle
            FLIRController.Execute();
            ModemController.Execute();
            CAPIIIController.Execute();
        }
        #endregion
    }
}
