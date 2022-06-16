using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    public class AutoAcquisition : BaseModelClass
    {
        #region Properties exposing private members of PositionPlanning Class
        [Category("Drivers and Algorithms"), Description("The Modem Driver in Use")]
        public ModemDriver ModemInUse
        {
            get { return LinktoModem; }
            set { LinktoModem = value; }
        }
        [Category("Drivers and Algorithms"), Description("The Planning Loop in Use")]
        public PositionPlanning PlanningLoopInUse
        {
            get { return LinktoPosPlan; }
            set { LinktoPosPlan = value; }
        }
        [Category("Drivers and Algorithms"), Description("The Augmentation Loop in Use")]
        public PositionAugmentation AugmentationLoopInUse
        {
            get { return LinktoScanGen; }
            set { LinktoScanGen = value; }
        }
        [Category("Auto Acquisition"), Description("Algorithm State")]
        public AutoAcquisitionStates AutoAcquisitionState
        {
            get { return AcquisitionState; }
            set { AcquisitionState = value; }
        }
        #endregion
        #region Private Members of the PositionPlanning Class
        AutoAcquisitionStates AcquisitionState, LastAcqState; // State of the Acquisition Algorithm
        HillClimbStates StepState, LastStepState;
        ModemDriver LinktoModem;                // Pointer to modem driver object 
        PositionPlanning LinktoPosPlan;         // Pointer to position planning object
        PositionAugmentation LinktoScanGen;     // Pointer to scan/step generator 
        bool OnTarget, LastOnTarget;
        double OnTargetThresh = 0.1;
        List<double> SpiralSignalMetrics;
        List<double> SpiralAzimuthFeedbacks, SpiralElevationFeedbacks;
        List<double> SpiralAzOffsets, SpiralElOffsets;
        List<bool> SpiralModemLocks;
        List<double> StepSignalMetrics;
        List<double> StepAxisFeedbacks;
        List<double> StepAxisOffsets;
        List<bool> StepModemLocks;
        DateTime StartTime;
        int upCount, downCount;
        double tempcenteroffset, lastMaxMetric;

        #endregion
        #region Main Execution System Functions of PositionPlanning Clas
        public AutoAcquisition()
        {
            LinktoModem = new ModemDriver();
            LinktoScanGen = new PositionAugmentation(LinktoPosPlan);
            LinktoScanGen.ModelsNotExpanded.Add(LinktoPosPlan);

            LinktoPosPlan = new PositionPlanning(LinktoScanGen, this);
            LinktoPosPlan.ModelsNotExpanded.Add(LinktoScanGen);
            LinktoPosPlan.ModelsNotExpanded.Add(this);

        }
        public AutoAcquisition(PositionPlanning plannerInUse, PositionAugmentation scannerInUse)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            LinktoModem = new ModemDriver();
            LinktoScanGen = scannerInUse;
            LinktoPosPlan = plannerInUse;
        }
        public AutoAcquisition(PositionPlanning plannerInUse, PositionAugmentation scannerInUse, ModemDriver LinktoModemDriver)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            LinktoModem = LinktoModemDriver;
            LinktoScanGen = scannerInUse;
            LinktoPosPlan = plannerInUse;
        }
        public AutoAcquisition(ModemDriver LinktoModemDriver)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            LinktoModem = LinktoModemDriver;

            LinktoScanGen = new PositionAugmentation(LinktoPosPlan);
            LinktoScanGen.ModelsNotExpanded.Add(LinktoPosPlan);

            LinktoPosPlan = new PositionPlanning(LinktoScanGen, this);
            LinktoPosPlan.ModelsNotExpanded.Add(LinktoScanGen);
            LinktoPosPlan.ModelsNotExpanded.Add(this);
        }
        public override void Initialize()
        {

        }
        public override void Prepare()
        {

        }
        public override void Execute()
        {
            // Indicate on Target
            OnTarget = OnTargetThresh >= LinktoPosPlan.WorldRadialError;

            // Run the State Machine
            switch (AcquisitionState)
            {
                case AutoAcquisitionStates.Ready:
                    caseReady();
                    break;
                case AutoAcquisitionStates.Nominal:
                    caseNominal();
                    break;
                case AutoAcquisitionStates.ModemLock:
                    caseModemLock();
                    break;
                case AutoAcquisitionStates.StepAz:
                    caseStepAxis(AxisSelect.Azimuth);
                    break;
                case AutoAcquisitionStates.StepEl:
                    caseStepAxis(AxisSelect.Elevation);
                    break;
                case AutoAcquisitionStates.Peaked:
                    casePeaked();
                    break;
                case AutoAcquisitionStates.Error:
                    caseError();
                    break;
            }
            // Capture History for Change Detection
            LastOnTarget = OnTarget;
        }
        #endregion
        #region Subfunctions of the AutoAcquisition Class
        void latchAxisStepData(AxisSelect inputAxis, bool turnaround)
        {
            if (turnaround)
            {
                StepAxisFeedbacks.Clear();
                StepAxisOffsets.Clear();
                StepModemLocks.Clear();
                StepSignalMetrics.Clear();
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Turning Around in Step Scan");
            }

            if (inputAxis == AxisSelect.Azimuth)
            {
                StepAxisFeedbacks.Add(LinktoPosPlan.WorldFeedbackAzimuth);
                StepAxisOffsets.Add(LinktoPosPlan.WorldFeedbackAzimuth - LinktoPosPlan.SelectedWorldAzSetpoint);
            }

            else if (inputAxis == AxisSelect.Elevation)
            {
                StepAxisFeedbacks.Add(LinktoPosPlan.WorldFeedbackElevation);
                StepAxisOffsets.Add(LinktoPosPlan.WorldFeedbackElevation - LinktoPosPlan.SelectedWorldElSetpoint);
            }
            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Latching Data in Step Scan");
            StepModemLocks.Add(LinktoModem.ModemLockStatus);
            StepSignalMetrics.Add(LinktoModem.SignalMetricStatus);

        }
        void caseReady()
        {
            // Indicate Ready
            if (AcquisitionState != LastAcqState)
            {
                LastAcqState = AcquisitionState;
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Auto Acquistion Ready to Begin");
            }

        }
        void caseNominal()
        {
            // Send the GOTO Nominal Command
            if (AcquisitionState != LastAcqState)
            {
                LastAcqState = AcquisitionState;
                LinktoPosPlan._PositionSelectionSwitch = SetSwitchPosition.NomSat;
                OnTarget = false; LastOnTarget = false;
            }


            if (OnTarget && !LastOnTarget)   // Just Arrived at Nominal Location
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Arrived at Nominal Satellite Position");
            }
            else if (!OnTarget && LastOnTarget)  // Just moved off Nominal
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Traversed Away from Nominal Satellite Position");
            }
            else if (OnTarget)   // What to do when motion is complete
            {
                AcquisitionState = AutoAcquisitionStates.ModemLock;
            }
            else if (!OnTarget) // While waiting for motion to complete
            {

            }
        }
        void caseModemLock()
        {
            // Indicate Scan Start
            if (AcquisitionState != LastAcqState)
            {
                LastAcqState = AcquisitionState;
                #region Clear Data Lists
                if (SpiralSignalMetrics == null)
                    SpiralSignalMetrics = new List<double>();
                SpiralSignalMetrics.Clear();
                if (SpiralModemLocks == null)
                    SpiralModemLocks = new List<bool>();
                SpiralModemLocks.Clear();
                if (SpiralAzimuthFeedbacks == null)
                    SpiralAzimuthFeedbacks = new List<double>();
                SpiralAzimuthFeedbacks.Clear();
                if (SpiralElevationFeedbacks == null)
                    SpiralElevationFeedbacks = new List<double>();
                SpiralElevationFeedbacks.Clear();
                if (SpiralAzOffsets == null)
                    SpiralAzOffsets = new List<double>();
                SpiralAzOffsets.Clear();
                if (SpiralElOffsets == null)
                    SpiralElOffsets = new List<double>();
                SpiralElOffsets.Clear();
                #endregion
                LinktoScanGen.SCAN_STATE = ScanType.Spiral;
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Beginning Spiral Scan");
                OnTarget = false; LastOnTarget = false;
            }
            else if (OnTarget && !LastOnTarget)   // Just Arrived at Sample Location
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Arrived at Spiral Sample Location...");
                StartTime = this.UniverseTimer.GUITime;
            }
            else if (!OnTarget && LastOnTarget)  // Just moved off Sample Location
            {

            }
            else if (OnTarget)   // dwelling at sample location
            {
                // If dwell time has expired, latch data and go
                if (DateTime.Compare(StartTime.AddSeconds(LinktoScanGen.SpiralDwellTime), UniverseTimer.GUITime) < 0)
                {
                    // Check for Modem Lock
                    if (LinktoModem.ModemLockStatus)
                    {
                        // Collect Modem Data
                        SpiralAzimuthFeedbacks.Add(LinktoPosPlan.WorldFeedbackAzimuth);
                        SpiralElevationFeedbacks.Add(LinktoPosPlan.WorldFeedbackElevation);
                        SpiralAzOffsets.Add(LinktoPosPlan.WorldFeedbackAzimuth - LinktoPosPlan.SelectedWorldAzSetpoint);
                        SpiralElOffsets.Add(LinktoPosPlan.WorldFeedbackElevation - LinktoPosPlan.SelectedWorldElSetpoint);
                        SpiralModemLocks.Add(LinktoModem.ModemLockStatus);
                        SpiralSignalMetrics.Add(LinktoModem.SignalMetricStatus);
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Sampling Modem Data...");

                        
                        // Check for first modem lock location
                        if (SpiralModemLocks.Count == 1)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Found 1st Modem Lock Location");
                            // Rebase to HERE
                            LinktoPosPlan.CalculatedPeakAzimuth = SpiralAzimuthFeedbacks[0];
                            LinktoPosPlan.CalculatedPeakElevation = SpiralElevationFeedbacks[0];
                            LinktoPosPlan._PositionSelectionSwitch = SetSwitchPosition.CalcPeak;
                            LinktoScanGen.StartAngle = 0;
                            LinktoScanGen.IncrementalRadialDistance = LinktoScanGen.IncrementalRadialDistance / 2;
                            //LinktoScanGen.IncrementalSpiralAngle = LinktoScanGen.IncrementalSpiralAngle / 2;
                            LinktoScanGen.LastN = -1;
                            LinktoScanGen.N = 0;
                            lastMaxMetric = SpiralSignalMetrics.Max();
                            upCount = 0; downCount = 0;
                        }
                        //// Check for second modem lock location
                        //else if (SpiralModemLocks.Count == 2)
                        //{

                        //    this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Found 2nd Modem Lock Location");

                        //    // Rebase to Max (set start angle)
                        //    LinktoPosPlan.CalculatedPeakAzimuth = SpiralAzimuthFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())];
                        //    LinktoPosPlan.CalculatedPeakElevation = SpiralElevationFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())];
                        //    LinktoScanGen.StartAngle = 180 / Math.PI * Math.Atan2(SpiralElevationFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())] - SpiralElevationFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Min())], SpiralAzimuthFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())] - SpiralAzimuthFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Min())]);
                        //    //LinktoScanGen.IncrementalRadialDistance = LinktoScanGen.IncrementalRadialDistance / 2;
                        //    LinktoScanGen.IncrementalSpiralAngle = LinktoScanGen.IncrementalSpiralAngle / 2;
                        //    LinktoScanGen.N = 0;
                        //    lastMaxMetric = SpiralSignalMetrics.Max();
                        //}
                        // Check for after the 2nd lock is found
                        else if (SpiralModemLocks.Count > 1)
                        {
                            if(lastMaxMetric >= SpiralSignalMetrics.Max())
                            {
                                upCount = 0;
                                downCount++;
                                // exit
                                if (downCount > 8)
                                {
                                    this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Found Final Modem Lock at Max Location");
                                    AcquisitionState = AutoAcquisitionStates.StepAz;
                                    LinktoScanGen.SCAN_STATE = ScanType.None;
                                }                   
                            }
                            else
                            {
                                upCount++;
                                downCount=0;
                                // Rebase to Max (set start angle)
                                LinktoPosPlan.CalculatedPeakAzimuth = SpiralAzimuthFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())];
                                LinktoPosPlan.CalculatedPeakElevation = SpiralElevationFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())];
                                LinktoScanGen.StartAngle = 180 / Math.PI * Math.Atan2(SpiralElevationFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())] - SpiralElevationFeedbacks[SpiralSignalMetrics.IndexOf(lastMaxMetric)], SpiralAzimuthFeedbacks[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())] - SpiralAzimuthFeedbacks[SpiralSignalMetrics.IndexOf(lastMaxMetric)]);
                                LinktoScanGen.LastN = -1;
                                LinktoScanGen.N = 0;
                                lastMaxMetric = SpiralSignalMetrics.Max();
                                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Found a Better Modem Lock Location");
                            }                            
                        }
                    }
                    else if(SpiralModemLocks.Count > 1)
                    {
                        downCount++;
                        // exit
                        if (downCount > 8)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Found Final Modem Lock at Max Location");
                            AcquisitionState = AutoAcquisitionStates.StepAz;
                            LinktoScanGen.SCAN_STATE = ScanType.None;
                        }
                    }

                    


                    // Check for Scan End Conditions
                    if (LinktoScanGen.ScanComplete && SpiralModemLocks.Count == 0)
                    {
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Did Not Find Modem Lock");
                        AcquisitionState = AutoAcquisitionStates.Error;
                        LinktoScanGen.SCAN_STATE = ScanType.None;
                    }
                    //// Check for Scan End and some Lock Points
                    else if (LinktoScanGen.ScanComplete && SpiralModemLocks.Count <= 2)
                    {
                        LinktoPosPlan.CalculatedPeakAzimuth = SpiralAzOffsets[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())] + LinktoPosPlan.SelectedWorldAzSetpoint;
                        LinktoPosPlan.CalculatedPeakElevation = SpiralElOffsets[SpiralSignalMetrics.IndexOf(SpiralSignalMetrics.Max())] + LinktoPosPlan.SelectedWorldElSetpoint;
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Spiral Scan Found Max of Only 1 or 2 Lock Locations");
                        AcquisitionState = AutoAcquisitionStates.StepAz;
                        LinktoScanGen.SCAN_STATE = ScanType.None;
                    }  
                    // Otherwise, Calculate next set of offset values
                    else
                    {
                        AugmentationLoopInUse.N++;
                        OnTarget = false; LastOnTarget = false;
                    }

                }
            }
            else if (!OnTarget) // In route to sample location
            {

            }
        }
        void caseStepAxis(AxisSelect inputAxis)
        {
            // Send the GOTO Calc Peak Command
            if (AcquisitionState != LastAcqState)
            {
                LastAcqState = AcquisitionState;
                #region Clear Data Lists
                if (StepSignalMetrics == null)
                    StepSignalMetrics = new List<double>();
                StepSignalMetrics.Clear();
                if (StepModemLocks == null)
                    StepModemLocks = new List<bool>();
                StepModemLocks.Clear();
                if (StepAxisFeedbacks == null)
                    StepAxisFeedbacks = new List<double>();
                StepAxisFeedbacks.Clear();
                if (StepAxisOffsets == null)
                    StepAxisOffsets = new List<double>();
                StepAxisOffsets.Clear();
                #endregion
                
                LinktoPosPlan._PositionSelectionSwitch = SetSwitchPosition.CalcPeak;
                LinktoScanGen.StepAxis = inputAxis;
                LinktoScanGen.SCAN_STATE = ScanType.Step;
                StepState = HillClimbStates.Nominal;
                OnTarget = false; LastOnTarget = false;
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Beginning Step Scan of "+ inputAxis.ToString());
            }
            else if (OnTarget && !LastOnTarget)   // Just Arrived at Sample Location
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Arrived at Sample Location, Step Scan of " + inputAxis.ToString()+", "+StepState.ToString());
                StartTime = this.UniverseTimer.GUITime;
            }
            else if (!OnTarget && LastOnTarget)  // Just moved off Sample Location
            {
            }
            else if (OnTarget)   // What to do when motion is complete
            {
                // If dwell time has expired, latch data and go
                if (DateTime.Compare(StartTime.AddSeconds(LinktoScanGen.StepDwellTime), UniverseTimer.GUITime) < 0)
                {
                    // Check for Modem Lock
                    if (LinktoModem.ModemLockStatus)
                    {
                        switch (StepState)
                        {
                            case HillClimbStates.Nominal:
                                caseStepNominal(inputAxis);
                                break;
                            case HillClimbStates.FindUP:
                                caseStepFindUP(inputAxis);
                                break;
                            case HillClimbStates.FindDOWN:
                                caseStepFindDown(inputAxis);
                                break;
                            case HillClimbStates.RoughFit:
                                caseStepRoughFit(inputAxis);
                                break;
                            case HillClimbStates.FindEND:
                                caseStepFindEnd(inputAxis);
                                break;
                            case HillClimbStates.FineFit:
                                caseStepFineFit(inputAxis);
                                break;
                        }
                        if (LastStepState != StepState)
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Step Scan of " + inputAxis.ToString() + " : Entering '" + StepState.ToString() + " State");
                        LastStepState = StepState;

                    }
                    // When no Modem Lock
                    else
                    {
                        // Failed to Latch Nominal???
                        if(StepSignalMetrics.Count == 0)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, inputAxis.ToString() + " Step Scan Lost Lock at Nominal Location");
                            AcquisitionState = AutoAcquisitionStates.Error;
                            LinktoScanGen.SCAN_STATE = ScanType.None;
                        }
                        // Latched Data at Nominal location, trying to findup, somehting is wrong
                        else if(StepSignalMetrics.Count == 1 && StepState == HillClimbStates.FindUP)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Warning, inputAxis.ToString()+" Step Scan Lost Lock after Initial Step off");
                            // Step Same Size in Opposite Direction
                            AugmentationLoopInUse.StepDirection = (-1)*AugmentationLoopInUse.StepDirection;
                            upCount = 0; downCount++;
                            OnTarget = false; LastOnTarget = false;

                        }
                        // Latched Data at Nominal location and Second location, trying to findup, dealing with an unusually flat beam
                        else if (StepSignalMetrics.Count == 2 && StepState == HillClimbStates.FindUP)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Warning, inputAxis.ToString() + " Step Scan Lost Lock after Second Step off, Flat Beam??");
                            // Go Back to Where we last had a good metric
                            AugmentationLoopInUse.N = (0.5) * AugmentationLoopInUse.N;
                            upCount = 0; downCount++;
                            OnTarget = false; LastOnTarget = false;

                        }
                        // Lost lock during HIll Climb and peak detection
                        else if (StepState == HillClimbStates.FindUP)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Warning, inputAxis.ToString() + " " + "Step Scan Struggling from Intermittent Modem Lock" + " state:" + StepState.ToString());
                            upCount = 0; downCount++;
                        }
                        // Lost lock during HIll Climb and peak detection
                        else if(StepState == HillClimbStates.FindDOWN)
                        {
                            this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Warning, inputAxis.ToString() + " " + "Step Scan Struggling from Intermittent Modem Lock" + " state:" + StepState.ToString());
                            upCount = 0; downCount++;
                        }
                        
                    }


                    // Check for Step End Conditions
                    if (LinktoScanGen.ScanComplete && StepModemLocks.Count == 0)
                    {
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Step Scan Failed from No Modem Lock");
                        AcquisitionState = AutoAcquisitionStates.Error;
                        LinktoScanGen.SCAN_STATE = ScanType.None;
                    }
                    else if (LinktoScanGen.ScanComplete && StepModemLocks.Count <= 6)
                    {
                        this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Step Scan Failed from too few Valid Signal Metrics (Modem Lock)");
                        AcquisitionState = AutoAcquisitionStates.StepAz;
                        LinktoScanGen.SCAN_STATE = ScanType.None;
                    }
                }
            }
            else if (!OnTarget) // While waiting for motion to complete
            {

            }
        }
        void casePeaked()
        {
            // Send the GOTO Calc Peak Command
            if (AcquisitionState != LastAcqState)
            {
                LastAcqState = AcquisitionState;
                LinktoPosPlan._PositionSelectionSwitch = SetSwitchPosition.CalcPeak;
                OnTarget = false; LastOnTarget = false;
            }


            if (OnTarget && !LastOnTarget)   // Just Arrived at Peak
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Arrived at Calculated Peak Position");
            }
            else if (!OnTarget && LastOnTarget)  // Just moved off Peak
            {
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Traversed Away from Calculated Peak Position");
            }
            else if (OnTarget)   // What to do when motion is complete
            {

            }
            else if (!OnTarget) // While waiting for motion to complete
            {

            }
        }
        void caseError()
        {

            // Perform Rising Edge, State Init Actions
            if (AcquisitionState != LastAcqState)
            {
                LastAcqState = AcquisitionState;
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Dummy Error Cleared...");
            }


            // TODO: Everything

            // With Errors Cleared, Return to Ready
            AcquisitionState = AutoAcquisitionStates.Ready;
        }
        void caseStepNominal(AxisSelect inputAxis)
        {
            
            // Latch Data
            latchAxisStepData(inputAxis, false);
            // Go Find Up
            AugmentationLoopInUse.MaximumTotalSteps = ModemInUse.HalfPowerBW * 8;
            AugmentationLoopInUse.N = 5;            // 1/2 of the Half Power Beam Width, when Delta is 1/10 of the Half Power Beam Width
            StepState = HillClimbStates.FindUP;
            upCount = 0; downCount = 0;
            OnTarget = false; LastOnTarget = false;
        }
        void caseStepFindUP(AxisSelect inputAxis)
        {
            // Act on First sample after nominal sample
            if(StepSignalMetrics.Count == 1)
            {
                // Compare to Nominal Sample, Bigger or Equal, Just stepped into the beam, flat beam??
                if (StepSignalMetrics[0] <= LinktoModem.SignalMetricStatus)
                {
                    // Try another big step same direction
                    latchAxisStepData(inputAxis, false);
                    AugmentationLoopInUse.N = 2 * AugmentationLoopInUse.N;
                    upCount = 0; downCount = 0;
                    OnTarget = false; LastOnTarget = false;
                }
                // Smaller, Just stepped off beam
                else
                {
                    // Initiate Hill Climb
                    latchAxisStepData(inputAxis, true);
                    StepState = HillClimbStates.FindDOWN;
                    AugmentationLoopInUse.N--;
                    upCount = 0; downCount = 0;
                    OnTarget = false; LastOnTarget = false;
                }
            }
            // Act on third total sample
            else if(StepSignalMetrics.Count == 2)
            {
                // Compare to Last Sample, Bigger or Equal, Just stepped into the beam again, flat beam??
                if (StepSignalMetrics[1] <= LinktoModem.SignalMetricStatus)
                {
                    // Try another big step same direction
                    latchAxisStepData(inputAxis, false);
                    AugmentationLoopInUse.N = 1.5 * AugmentationLoopInUse.N;
                    upCount = 0; downCount = 0;
                    OnTarget = false; LastOnTarget = false;
                }
                // Smaller, Just stepped off beam
                else
                {
                    // Initiate Hill Climb
                    latchAxisStepData(inputAxis, true);
                    StepState = HillClimbStates.FindDOWN;
                    AugmentationLoopInUse.N--;
                    upCount = 0; downCount = 0;
                    OnTarget = false; LastOnTarget = false;
                }
            }
            
        }
        void caseStepFindDown(AxisSelect inputAxis)
        {
            // Compare to last collected, if bigger, keep going that way
            if (StepSignalMetrics[StepSignalMetrics.Count - 1] <= LinktoModem.SignalMetricStatus)
            {
                // Latch Data
                latchAxisStepData(inputAxis, false);
                upCount++;
                downCount=0;                
                // Trigger the next step
                AugmentationLoopInUse.N--;
                OnTarget = false; LastOnTarget = false;               
            }
            // Compare to last collected, if smaller,  keepgoing or stop
            else
            {
                latchAxisStepData(inputAxis, false);
                upCount=0;
                downCount++;
                if (StepSignalMetrics.Count > 3 && downCount >= Math.Floor(StepSignalMetrics.Count/4.0F))      // roughly 1/4 of total samples are consecutively down
                {
                    StepState = HillClimbStates.RoughFit;
                    upCount = 0; downCount = 0;
                }   
                else
                {
                    // Trigger the next step
                    AugmentationLoopInUse.N--;
                    OnTarget = false; LastOnTarget = false;
                }          
            }
        }
        void caseStepRoughFit(AxisSelect inputAxis)
        {
            tempcenteroffset = Matrix3x3.ProcessScanData(StepSignalMetrics, StepAxisOffsets);
                        
            if (tempcenteroffset != double.NaN)
            {
                // Calculate and Set new Step Scan End Condition
                LinktoScanGen.MaximumTotalSteps = Math.Abs(StepAxisOffsets[0] + tempcenteroffset);

                // Goto the "Find End" State
                StepState = HillClimbStates.FindEND;

                // Command Scan Generator to calculate next offsets
                LinktoScanGen.N--;
                OnTarget = false; LastOnTarget = false;
            }
            else
            {
                // Indicate Error
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Parabolic LSF of Offset/Modem data returned NAN in preliminary fit");
                AcquisitionState = AutoAcquisitionStates.Error;
                LinktoScanGen.SCAN_STATE = ScanType.None;
            }
        }
        void caseStepFindEnd(AxisSelect inputAxis)
        {
            // Compare to last collected, if smaller, keep going that way
            if (StepSignalMetrics[StepSignalMetrics.Count - 1] >= LinktoModem.SignalMetricStatus)
            {
                // Latch Data
                latchAxisStepData(inputAxis, false);
                upCount = 0;
                downCount++;
                // Check for Exit Condition
                if (LinktoScanGen.ScanComplete)
                {
                    // Go fine fit
                    StepState = HillClimbStates.FineFit;
                    upCount = 0; downCount = 0;
                }
                else
                {
                    // Command Scan Generator to calculate next offsets
                    LinktoScanGen.N--;
                    OnTarget = false; LastOnTarget = false;
                }
            }
            // Compare to last collected, if bigger,  keepgoing or stop
            else if (StepSignalMetrics[StepSignalMetrics.Count - 1] < LinktoModem.SignalMetricStatus)
            {
                latchAxisStepData(inputAxis, false);
                downCount = 0;
                downCount++;
                if (upCount > 3) // stop
                {
                    this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Info, "Step Scan Failed from Positive traverse in Hill Descend End");
                    AcquisitionState = AutoAcquisitionStates.Error;
                    LinktoScanGen.SCAN_STATE = ScanType.None;
                }
            }

        }
        void caseStepFineFit(AxisSelect inputAxis)
        {
            tempcenteroffset = Matrix3x3.ProcessScanData(StepSignalMetrics, StepAxisOffsets);
            if (tempcenteroffset != double.NaN)
            {
                // Set the Peak Positions
                if (inputAxis == AxisSelect.Azimuth)
                {
                    LinktoPosPlan.CalculatedPeakAzimuth = LinktoPosPlan.SelectedWorldAzSetpoint + tempcenteroffset;
                    AcquisitionState = AutoAcquisitionStates.StepEl;
                }
                else if (inputAxis == AxisSelect.Elevation)
                {
                    LinktoPosPlan.CalculatedPeakElevation = LinktoPosPlan.SelectedWorldElSetpoint + tempcenteroffset;
                    AcquisitionState = AutoAcquisitionStates.Peaked;
                }
                LinktoScanGen.SCAN_STATE = ScanType.None;
            }
            else
            {
                // Indicate Error
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Parabolic LSF of Offset/Modem data returned NAN in final fit");
                AcquisitionState = AutoAcquisitionStates.Error;
                LinktoScanGen.SCAN_STATE = ScanType.None;
            }
        }
        #endregion
    }
}
