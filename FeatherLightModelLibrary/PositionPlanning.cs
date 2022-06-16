using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    public class PositionPlanning : BaseModelClass
    {

        #region Properties exposing private members of PositionPlanning Class
        [Category("Position Command Inputs"), Description("Selection of Input to become Setpoint for Position Loopf")]
        public SetSwitchPosition _PositionSelectionSwitch
        {
            get { return PositionInputSwitch; }
            set {
                resetSwitch = true;
                PositionInputSwitch = value; }
        }
        [Category("Position Command Inputs"), Description("Nominal Satellite Azimuth Look Angle in World Coordinate (deg)")]
        public double NominalSatAzimuth
        {
            get { return NomSatAz * 180 / Math.PI; }
            set { resetSwitch = true;
                NomSatAz = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("Nominal Satellite Elevation Look Angle in World Coordinate (deg)")]
        public double NominalSatElevation
        {
            get { return NomSatEl * 180 / Math.PI; }
            set
            {
                resetSwitch = true;
                NomSatEl = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("Calculated Peak Beam Elevation Lookangle - World (deg)")]
        public double CalculatedPeakElevation
        {
            get { return CalcPeakEl * 180 / Math.PI; }
            set
            {
                resetSwitch = true;
                LastPeakEl = CalcPeakEl;
                CalcPeakEl = value * Math.PI / 180;
            }
        }
        [Category("Position Command Inputs"), Description("Calculated Peak Beam Azimuth Lookangle - World (deg)")]
        public double CalculatedPeakAzimuth
        {
            get { return CalcPeakAz * 180 / Math.PI; }
            set
            {
                resetSwitch = true;
                LastPeakAz = CalcPeakAz;
                CalcPeakAz = value * Math.PI / 180;
            }
        }
        [Category("Position Command Inputs"), Description("Previous Calculated Peak Beam Elevation Lookangle - World (deg)")]
        public double LastCalculatedPeakElevation
        {
            get { return LastPeakEl * 180 / Math.PI; }
            set { LastPeakEl = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("Previous Calculated Peak Beam Azimuth Lookangle - World (deg)")]
        public double LastCalculatedPeakAzimuth
        {
            get { return LastPeakAz * 180 / Math.PI; }
            set { LastPeakAz = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("Stow Position Azimuth - Pedestal (deg)")]
        public double StowPedestalAzimuth
        {
            get { return StowAz * 180 / Math.PI; }
            set { StowAz = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("Stow Position Elevation - Pedestal (deg)")]
        public double StowPedestalElevation
        {
            get { return StowEl * 180 / Math.PI; }
            set { StowEl = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("User Position 1 Azimuth - World (deg)")]
        public double UserPosition1Azimuth
        {
            get { return UserAz1 * 180 / Math.PI; }
            set { UserAz1 = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("User Position 1 Elevation - World (deg)")]
        public double UserPosition1Elevation
        {
            get { return UserEl1 * 180 / Math.PI; }
            set { UserEl1 = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("User Position 2 Azimuth - World (deg)")]
        public double UserPosition2Azimuth
        {
            get { return UserAz2 * 180 / Math.PI; }
            set { UserAz2 = value * Math.PI / 180; }
        }
        [Category("Position Command Inputs"), Description("User Position 2 Elevation - World (deg)")]
        public double UserPosition2Elevation
        {
            get { return UserEl2 * 180 / Math.PI; }
            set { UserEl2 = value * Math.PI / 180; }
        }
        [Category("Scan and Acquisition"), Description("The Scan and Step Augmentation Generator")]
        public PositionAugmentation AugmentationLoop
        {
            get { return AugmentLoop; }
            set { AugmentLoop = value; }
        }
        [Category("Scan and Acquisition"), Description("The FeatherLight AutoAcquisition Algorithm")]
        public AutoAcquisition AutoAcquireLoop
        {
            get { return AutoAcqLoop; }
            set { AutoAcqLoop = value; }
        }
        [Category("Postion Control Status"), Description("Commanded Azimuth Look Angle in World Coordinate (deg)")]
        public double CommandedWorldAzimuth
        {
            get { return WorldAzCMD * 180 / Math.PI; }
            set { WorldAzCMD = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Commanded Elevation Look Angle in World Coordinate (deg)")]
        public double CommandedWorldElevation
        {
            get { return WorldElCMD * 180 / Math.PI; }
            set { WorldElCMD = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Azimuth World Coordinate Feedback Position (deg)")]
        public double WorldFeedbackAzimuth
        {
            get { return WorldAzFBK * 180 / Math.PI; }
            set { WorldAzFBK = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Elevation World Coordinate Feedback Position (deg)")]
        public double WorldFeedbackElevation
        {
            get { return WorldElFBK * 180 / Math.PI; }
            set { WorldElFBK = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Planning Loop Radial Error, Posistion Commnad - Position Feedback World (deg)")]
        public double WorldRadialError
        {
            get { return ControlRadialError * 180 / Math.PI; }
            set { ControlRadialError = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Azimuth World Coordinate Augmentation Offset World (deg)")]
        public double WorldOffsetAzimuth
        {
            get { return CalcOffsetAz * 180 / Math.PI; }
            set { CalcOffsetAz = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Elevation World Coordinate Augmentation Offset World (deg)")]
        public double WorldOffsetElevation
        {
            get { return CalcOffsetEl * 180 / Math.PI; }
            set { CalcOffsetEl = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Commanded Azimuth Look Angle in World Coordinate (deg)")]
        public double SelectedWorldAzSetpoint
        {
            get { return WorldAzSET * 180 / Math.PI; }
            set { WorldAzSET = value * Math.PI / 180; }
        }
        [Category("Postion Control Status"), Description("Commanded Elevation Look Angle in World Coordinate (deg)")]
        public double SelectedWorldElSetpoint
        {
            get { return WorldElSET * 180 / Math.PI; }
            set { WorldElSET = value * Math.PI / 180; }
        }
        #endregion
        #region Private Members of the PositionPlanning Class
        AutoAcquisition AutoAcqLoop;
        PositionAugmentation AugmentLoop;

        double NomSatAz, NomSatEl;                                  // World Coordinate nominal satellite location
        double StowAz, StowEl;                                      // PedCoord Stow Position
        double UserAz1, UserAz2, UserEl1, UserEl2;                  // User Input Position Commands
        double LastPeakAz, LastPeakEl;                              // Last stored peak position
        double CalcPeakAz, CalcPeakEl;                              // World Coordinate Peak look angle as calculated
        double WorldAzSET, WorldElSET;                              // Selected setpoints to feed planning loop
        double CalcOffsetAz, CalcOffsetEl;                          // Calculated offset from scan generator
        double WorldAzCMD, WorldElCMD, WorldAzFBK, WorldElFBK;      // World Coordinate commands and feedback
        double ControlRadialError;                                  // World Coordinate Degrees radial error
        double ErrorAz, ErrorEl;
        SetSwitchPosition PositionInputSwitch, LastPosSwitch;                      // 
        bool resetSwitch;

        #endregion
        #region Main Execution System Functions of PositionPlanning Class

        public PositionPlanning()
        {
            AutoAcqLoop = new AutoAcquisition(this, AugmentLoop);
            AutoAcqLoop.ModelsNotExpanded.Add(this);
            AutoAcqLoop.ModelsNotExpanded.Add(AugmentLoop);
            AugmentLoop = new PositionAugmentation(this);
            AugmentLoop.ModelsNotExpanded.Add(this);
            QuickTestInit();
        }
        public PositionPlanning(PositionAugmentation ScanGeninUse)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            AutoAcqLoop = new AutoAcquisition(this, AugmentLoop);
            AutoAcqLoop.ModelsNotExpanded.Add(this);
            AutoAcqLoop.ModelsNotExpanded.Add(AugmentLoop);
            AugmentLoop = ScanGeninUse;
            QuickTestInit();
        }
        public PositionPlanning(AutoAcquisition AUtoAcqInUse)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            AutoAcqLoop = AUtoAcqInUse;
            AugmentLoop = new PositionAugmentation(this);
            AugmentLoop.ModelsNotExpanded.Add(this);
            QuickTestInit();
        }
        public PositionPlanning(PositionAugmentation ScanGeninUse, AutoAcquisition AUtoAcqInUse)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            AutoAcqLoop = AUtoAcqInUse;
            AugmentLoop = ScanGeninUse;
            QuickTestInit();
        }
        public PositionPlanning(ModemDriver ModemInUse)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            AugmentLoop = new PositionAugmentation(this);
            AugmentLoop.ModelsNotExpanded.Add(this);
            AutoAcqLoop = new AutoAcquisition(this, AugmentLoop, ModemInUse);
            AutoAcqLoop.ModelsNotExpanded.Add(this);
            AutoAcqLoop.ModelsNotExpanded.Add(AugmentLoop);
            AutoAcqLoop.ModelsNotExpanded.Add(ModemInUse);

            QuickTestInit();
        }
        private void QuickTestInit()
        {
            
            // Setup Submodels List
            this.LocalSubModelList = new List<BaseModelClass>();
            this.LocalSubModelList.Add(AutoAcqLoop);
            this.LocalSubModelList.Add(AugmentationLoop);
        }
        public override void Initialize()
        {
            NominalSatAzimuth = 21.7;
            NominalSatElevation = 23.4;
            StowPedestalAzimuth = 2;
            StowPedestalElevation = -2;
            UserPosition1Azimuth = 3;
            UserPosition1Elevation = -3;
            UserPosition2Azimuth = 4;
            UserPosition2Elevation = -4;
            LastPosSwitch = SetSwitchPosition.User1;
            PositionInputSwitch = SetSwitchPosition.NomSat;
        }
        public override void Prepare()
        {

        }
        public override void Execute()
        {
            // Position Planning Mathematics
            ErrorAz = WorldAzCMD - WorldAzFBK;
            ErrorEl = WorldElCMD - WorldElFBK;
            ControlRadialError = Math.Sqrt(Math.Pow(ErrorAz, 2) + Math.Pow(ErrorEl, 2));

            // Run AutoAcquisition
            //AutoAcqLoop.Execute();

            #region Select Setpoint from Input Commands
            if (PositionInputSwitch != LastPosSwitch || resetSwitch)
                switch (PositionInputSwitch)
                {
                    case SetSwitchPosition.NomSat:
                        WorldAzSET = NomSatAz;
                        WorldElSET = NomSatEl;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Moving to Nominal Satellite Position");
                        break;
                    case SetSwitchPosition.CalcPeak:
                        WorldAzSET = CalcPeakAz;
                        WorldElSET = CalcPeakEl;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Moving to Calculated Peak Position");
                        break;
                    case SetSwitchPosition.LastPeak:
                        WorldAzSET = LastPeakAz;
                        WorldElSET = LastPeakEl;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Moving to Last Calculated Peak Position");
                        break;
                    case SetSwitchPosition.User1:
                        WorldAzSET = UserAz1;
                        WorldElSET = UserEl1;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Moving to User Input Position 1");
                        break;
                    case SetSwitchPosition.User2:
                        WorldAzSET = UserAz2;
                        WorldElSET = UserEl2;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Moving to User Input Position 2");
                        break;
                    case SetSwitchPosition.Stow:
                        WorldAzSET = StowAz;
                        WorldElSET = StowEl;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Moving to Stow Position");
                        break;
                }
            resetSwitch = false;
            LastPosSwitch = PositionInputSwitch;
            #endregion

            // Run Augmenation
            //AugmentLoop.Execute();

            // Position Planning Mathematics
            WorldAzCMD = WorldAzSET + CalcOffsetAz;
            WorldElCMD = WorldElSET + CalcOffsetEl;

        }
        #endregion
    }
}
