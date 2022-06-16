using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    public class PositionAugmentation : BaseModelClass
    {
        #region Properties exposing private members of PositionPlanning Class
        [Category("Step Settings"), Description("Incremental Step Size in Deg")]
        public double IncrementalStepSize
        {
            get { return StepParameters.DeltaTheta * 180 / Math.PI; }
            set { StepParameters.DeltaTheta = value * Math.PI / 180; }
        }
        [Category("Step Settings"), Description("Maximum Cummulative Steps in Deg")]
        public double MaximumTotalSteps
        {
            get { return StepParameters.MaximumTheta * 180 / Math.PI; }
            set { StepParameters.MaximumTheta = value * Math.PI / 180; }
        }
        [Category("Step Settings"), Description("Axis on which to Step (Az or El)")]
        public AxisSelect StepAxis
        {
            get { return StepParameters.StepAxis; }
            set { StepParameters.StepAxis = value; }
        }
        [Category("Step Settings"), Description("Time to Dwell at Sample Location")]
        public double StepDwellTime
        {
            get { return StepParameters.DwellTime; }
            set { StepParameters.DwellTime = value; }
        }
        [Category("Step Settings"), Description("Direction to Step (pos/neg)")]
        public double StepDirection
        {
            get { return StepParameters.DirectionSign; }
            set
            {
                lastn = -n;
                StepParameters.DirectionSign = Math.Sign(value);
            }
        }
        [Category("Spiral Scan Settings"), Description("Start Angle of Sprials Geometry in Deg")]
        public double StartAngle
        {
            get { return 180 * ScanParameters.StartTheta / Math.PI; }
            set
            {
                ScanParameters.StartTheta = Math.PI / 180 * value;
            }
        }
        [Category("Spiral Scan Settings"), Description("Angular Resolution of Sprials Geometry in Deg")]
        public double IncrementalSpiralAngle
        {
            get { return 180 * ScanParameters.DeltaTheta / Math.PI; }
            set
            {
                ScanParameters.DeltaTheta = Math.PI / 180 * value;
            }
        }
        [Category("Spiral Scan Settings"), Description("Radial Resolution of Sprials Geometry in in Deg")]
        public double IncrementalRadialDistance
        {
            get { return ScanParameters.DeltaRadius; }
            set
            {
                ScanParameters.DeltaRadius = value;
            }
        }
        [Category("Spiral Scan Settings"), Description("Maximum Radius of the Sprial Scan in Deg")]
        public double MaiximumSpiralRadius
        {
            get { return ScanParameters.MaximumRadius; }
            set
            {
                ScanParameters.MaximumRadius = value;
            }
        }
        [Category("Spiral Scan Settings"), Description("Time to Dwell at Sample Location")]
        public double SpiralDwellTime
        {
            get { return ScanParameters.DwellTime; }
            set
            {
                ScanParameters.DwellTime = value;
            }
        }
        [Category("Scan Modes"), Description("Switch for Scan Mode")]
        public ScanType SCAN_STATE
        {
            get { return SCANState; }
            set
            {
                SCANState = value;
            }
        }
        [Category("Scan Modes"), Description("Increasing factor in the offset function")]
        public double N
        {
            get { return n; }
            set
            {
                n = value;
            }
        }
        [Category("Scan Modes"), Description("Increasing factor in the offset function")]
        public double LastN
        {
            get { return lastn; }
            set
            {
                lastn = value;
            }
        }
        [Category("Scan Modes"), Description("Active 'Radius' of scan")]
        public double R
        {
            get { return r; }
            set
            {
                r = value;
            }
        }
        [Category("Scan Modes"), Description("Switch for Scan Mode")]
        public bool ScanComplete
        {
            get { return ScanDone; }
            set
            {
                ScanDone = value;
            }
        }
        [Category("Link to Position Planning Loop"), Description("Position Planning Loop in Use")]
        public PositionPlanning PositionPlanningLink
        {
            get { return LinktoPlanningObj; }
            set { LinktoPlanningObj = value; }
        }
        #endregion
        #region Private Members of the PositionPlanning Class
        PositionPlanning LinktoPlanningObj;     // Link to position planning object
        StepSettings StepParameters;            // Step Peaking, Hill Climb, Settings
        ScanSettings ScanParameters;            // Spiral Scan Settings
        double r;                               // Radius of Sprial Scan (deg)
        double n, lastn;
        bool ScanDone;
        ScanType SCANState = ScanType.None;
        ScanType lastScanSwitch;
        double lastDirection;
        #endregion
        #region Main Execution System Functions of PositionPlanning Class

        public PositionAugmentation()
        {
            LinktoPlanningObj = new PositionPlanning(this);
            LinktoPlanningObj.ModelsNotExpanded.Add(this);
            
        }
        public PositionAugmentation(PositionPlanning PlannerInUse)
        {
            this.ModelsNotExpanded = new List<BaseModelClass>();
            LinktoPlanningObj = PlannerInUse;
        }
        public override void Initialize()
        {
            IncrementalSpiralAngle = 45;
            IncrementalRadialDistance = 1.0 * 1.125;        // Half Power Beam Width
            MaiximumSpiralRadius = 10;                      // 
            IncrementalStepSize = 0.1 * 1.125;              // 10% of Half Power Beam Width
            MaximumTotalSteps = 3 * 1.125;                  // 300% of Half Power Beam Width
            ScanParameters.DwellTime = 3;
            StepParameters.DwellTime = 1;
            StepParameters.DirectionSign = 1;
            n = -1;
        }
        public override void Prepare()
        {

        }
        public override void Execute()
        {
            // Reset Scan Parameters on state change
            if (lastScanSwitch != SCANState)
            {
                n = 0;
                ScanDone = false;
            }
            // Update Offsets on change of n (index variable)
            if (n != lastn || StepParameters.DirectionSign != lastDirection)
            {
                // Calculate offset based on scan switch state
                switch (SCANState)
                {
                    case ScanType.None:
                        LinktoPlanningObj.WorldOffsetAzimuth = 0.0;
                        LinktoPlanningObj.WorldOffsetElevation = 0.0;
                        break;
                    case ScanType.Spiral:
                        //r = ScanParameters.DeltaRadius * ScanParameters.DeltaTheta * n / (2 * Math.PI);
                        if (n > 0)
                            r = Math.Abs(ScanParameters.DeltaRadius * (Math.Floor( (ScanParameters.DeltaTheta * (n-1) / (2 * Math.PI)) )+1 ) );
                        else
                            r = 0;
                        if (r <= ScanParameters.MaximumRadius)
                        {
                            LinktoPlanningObj.WorldOffsetAzimuth = r * Math.Cos( (n-1) * ScanParameters.DeltaTheta + ScanParameters.StartTheta);
                            LinktoPlanningObj.WorldOffsetElevation = r * Math.Sin( (n-1) * ScanParameters.DeltaTheta + ScanParameters.StartTheta);
                        }
                        else
                            ScanDone = true;
                        break;
                    case ScanType.Step:
                        r = n * StepParameters.DeltaTheta;
                        if (Math.Abs(r) <= StepParameters.MaximumTheta)
                        {
                            if (StepParameters.StepAxis == AxisSelect.Azimuth)
                                LinktoPlanningObj.WorldOffsetAzimuth = StepParameters.DirectionSign * r * 180 / Math.PI;
                            else if (StepParameters.StepAxis == AxisSelect.Elevation)
                                LinktoPlanningObj.WorldOffsetElevation = StepParameters.DirectionSign * r * 180 / Math.PI;
                        }
                        else
                            ScanDone = true;

                        break;
                }
            }
            // Capture History
            lastScanSwitch = SCANState;
            lastDirection = StepParameters.DirectionSign;
            lastn = n;
        }

        #endregion
        #region Sub Functions of PositionPlanning Class


        #endregion
    }
}
