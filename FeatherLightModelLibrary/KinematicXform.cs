using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimpleModelExplorer;
using System.ComponentModel;

namespace FeatherLightConcepts
{
    public class KinematicXform : BaseModelClass
    {
        #region Properties exposing private members of PositionPlanning Class

        #region Input Parameters, triggering the prepare function

        [Category("Postion Inputs"), Description("World Coordinate Azimuth Look Angle (deg)")]
        public double WorldAzimuthCommand
        {
            get { return WorldAzimuthCMD * 180 / Math.PI; }
            set { WorldAzimuthCMD = value * Math.PI / 180; }
        }
        [Category("Postion Inputs"), Description("World Coordinate Elevation Look Angle (deg)")]
        public double WorldElevationCommand
        {
            get { return WorldElevationCMD * 180 / Math.PI; }
            set { WorldElevationCMD = value * Math.PI / 180; }
        }
        [Category("Postion Inputs"), Description("Pedestal Coordinate Azimuth Feedback Angle (deg)")]
        public double PedestalAzimuthFeedback
        {
            get { return PedestalAzFBK * 180 / Math.PI; }
            set { PedestalAzFBK = value * Math.PI / 180; }
        }
        [Category("Postion Inputs"), Description("Pedestal Coordinate Elevation Feedback Angle (deg)")]
        public double PedestalElevationFeedback
        {
            get { return PedestalElFBK * 180 / Math.PI; }
            set { PedestalElFBK = value * Math.PI / 180; }
        }
        [Category("Postion Inputs"), Description("Pedestal Coordinate Azimuth Offset/Augmentation Angle (deg)")]
        public double PedestalAzimuthOffset
        {
            get { return PedestalOffsetAz * 180 / Math.PI; }
            set { PedestalOffsetAz = value * Math.PI / 180; }
        }
        [Category("Postion Inputs"), Description("Pedestal Coordinate Elevation Offset/Augmentation Angle (deg)")]
        public double PedestalElevationOffset
        {
            get { return PedestalOffsetEl * 180 / Math.PI; }
            set { PedestalOffsetEl = value * Math.PI / 180; }
        }
        [property: Category("Postion Inputs"), Description("Heading of Antenna Base in Deg")]
        public double BaseYaw
        {
            get { return 180 * baseYaw / Math.PI; }
            set
            {
                baseYaw = Math.PI / 180 * value;
            }
        }
        [property: Category("Postion Inputs"), Description("Rolling of Antenna Base in Deg")]
        public double BaseRoll
        {
            get { return 180 * baseRoll / Math.PI; }
            set
            {
                baseRoll = Math.PI / 180 * value;
            }
        }
        [property: Category("Postion Inputs"), Description("Pitching of Antenna Base in Deg")]
        public double BasePitch
        {
            get { return 180 * basePitch / Math.PI; }
            set
            {
                basePitch = Math.PI / 180 * value;
            }
        }

        #endregion
        #region Output Parameters, changed by the model, not changed by the GUI

        [Category("Postion Outputs"), Description("Pedestal Coordinate Azimuth Look Angle (deg)"), ReadOnly(true)]
        public double PedestalAzimuthCommand
        {
            get { return PedestalAzimuthCMD * 180 / Math.PI; }
            set { PedestalAzimuthCMD = value * Math.PI / 180; }
        }
        [Category("Postion Outputs"), Description("Pedestal Coordinate Elevation Look Angle (deg)"), ReadOnly(true)]
        public double PedestalElevationCommand
        {
            get { return PedestalElevationCMD * 180 / Math.PI; }
            set { PedestalElevationCMD = value * Math.PI / 180; }
        }
        [Category("Postion Outputs"), Description("Pedestal Coordinate Azimuth Look Angle (deg)"), ReadOnly(true)]
        public double WorldAzimuthFeedback
        {
            get { return WorldAzFBK * 180 / Math.PI; }
            set { WorldAzFBK = value * Math.PI / 180; }
        }
        [Category("Postion Outputs"), Description("Pedestal Coordinate Elevation Look Angle (deg)"), ReadOnly(true)]
        public double WorldElevationFeedback
        {
            get { return WorldElFBK * 180 / Math.PI; }
            set { WorldElFBK = value * Math.PI / 180; }
        }

        #endregion
        #region Internal Values for Calculation and Debug

        [Category("Postion Internals"), Description("World Frame Look Angle Vector, X Coord, for Command values")]
        public double WCMDX
        {
            get { return wCMDX; }
            set { wCMDX = value; }
        }
        [Category("Postion Internals"), Description("World Frame Look Angle Vector, Y Coord, for Command values")]
        public double WCMDY
        {
            get { return wCMDY; }
            set { wCMDY = value; }
        }
        [Category("Postion Internals"), Description("World Frame Look Angle Vector, Z Coord, for Command values")]
        public double WCMDZ
        {
            get { return wCMDZ; }
            set { wCMDZ = value; }
        }
        [Category("Postion Internals"), Description("Pedestal Frame Look Angle Vector, X Coord, for Command values")]
        public double PCMDX
        {
            get { return pCMDX; }
            set { pCMDX = value; }
        }
        [Category("Postion Internals"), Description("Pedestal Frame Look Angle Vector, Y Coord, for Command values")]
        public double PCMDY
        {
            get { return pCMDY; }
            set { pCMDY = value; }
        }
        [Category("Postion Internals"), Description("Pedestal Frame Look Angle Vector, Z Coord, for Command values")]
        public double PCMDZ
        {
            get { return pCMDZ; }
            set { pCMDZ = value; }
        }
        [Category("Postion Internals"), Description("World Frame Look Angle Vector, X Coord, for Feedback values")]
        public double WFBKX
        {
            get { return wFBKX; }
            set { wFBKX = value; }
        }
        [Category("Postion Internals"), Description("World Frame Look Angle Vector, Y Coord, for Feedback values")]
        public double WFBKY
        {
            get { return wFBKY; }
            set { wFBKY = value; }
        }
        [Category("Postion Internals"), Description("World Frame Look Angle Vector, Z Coord, for Feedback values")]
        public double WFBKZ
        {
            get { return wFBKZ; }
            set { wFBKZ = value; }
        }
        [Category("Postion Internals"), Description("Pedestal Frame Look Angle Vector, X Coord, for Feedback values")]
        public double PFBKX
        {
            get { return pFBKX; }
            set { pFBKX = value; }
        }
        [Category("Postion Internals"), Description("Pedestal Frame Look Angle Vector, Y Coord, for Feedback values")]
        public double PFBKY
        {
            get { return pFBKY; }
            set { pFBKY = value; }
        }
        [Category("Postion Internals"), Description("Pedestal Frame Look Angle Vector, Z Coord, for Feedback values")]
        public double PFBKZ
        {
            get { return pFBKZ; }
            set { pFBKZ = value; }
        }
        [Category("Postion Outputs"), Description("Pedestal Coordinate Azimuth Look Angle (deg)"), ReadOnly(true)]
        public double PEdestalAzimuth
        {
            get { return PedestalAzimuth * 180 / Math.PI; }
            set { PedestalAzimuth = value * Math.PI / 180; }
        }
        [Category("Postion Outputs"), Description("Pedestal Coordinate Elevation Look Angle (deg)"), ReadOnly(true)]
        public double PEdestalElevation
        {
            get { return PedestalElevation * 180 / Math.PI; }
            set { PedestalElevation = value * Math.PI / 180; }
        }

        #endregion

        #endregion
        #region Private Members of the PositionPlanning Class
        double WorldAzimuthCMD, WorldElevationCMD;              // World Coordinate Look Angle Commands
        double PedestalAzimuthCMD, PedestalElevationCMD;        // Pedestal Coordinate Look Angle Commands after Adding Offsets
        double PedestalOffsetAz, PedestalOffsetEl;              // Pedestal Coordinate Offset Augmentations
        double PedestalAzFBK, PedestalElFBK;                    // Pedestal Coordinate Feedback Angles
        double WorldAzFBK, WorldElFBK;                          // World Coordinate Feedback Angles
        double wCMDX, wCMDY, wCMDZ;                             // World Look Angle Commands in Cartesian Coordinate for Command values
        double pCMDX, pCMDY, pCMDZ;                             // Pedestal Look Angle Commands in Cartesian Coordinate for Command values
        double wFBKX, wFBKY, wFBKZ;                             // World Look Angle Commands in Cartesian Coordinate for Feedback values
        double pFBKX, pFBKY, pFBKZ;                             // Pedestal Look Angle Commands in Cartesian Coordinate for Feedback values
        double baseYaw, baseRoll, basePitch;                    // Angle information to calculate Pedestal Coordinate
        double C_baseYaw, C_baseRoll, C_basePitch;              // Cosine values for calculation
        double S_baseYaw, S_baseRoll, S_basePitch;              // Sine values for calculation
        double PedestalAzimuth, PedestalElevation;              // Pedestal Coordinate Look Angle Commands before Adding Offsets
        double a1, a2, a3, a4, a5, a6, a7, a8, a9;              // Transformation Matrix Components
        double c1, c2, c3, c4, c5, c6, c7, c8, c9, det;         // Inverse Transformation Matrix Components
        AntennaInfo AngleInformation;
        #endregion
        #region Main Execution System Functions of PositionPlanning Clas
        public KinematicXform()
        {
            BaseYaw = 0.01;
            BaseRoll = 0.01;
            basePitch = 0.01;
            this.ReInitialize = true;
        }
        public override void Initialize()
        {
            this.RePrepare = true;
        }
        public override void Prepare()
        {
            C_baseYaw = Math.Cos(baseYaw);
            C_baseRoll = Math.Cos(baseRoll);
            C_basePitch = Math.Cos(basePitch);
            S_baseYaw = Math.Sin(baseYaw);
            S_baseRoll = Math.Sin(baseRoll);
            S_basePitch = Math.Sin(basePitch);

            a1 = C_baseYaw * C_basePitch;
            a2 = C_baseYaw * S_basePitch * S_baseRoll + C_baseRoll * S_baseYaw;
            a3 = S_baseYaw * S_baseRoll - C_baseYaw * S_basePitch * C_baseRoll;
            a4 = -S_baseYaw * C_basePitch;
            a5 = C_baseYaw * C_baseRoll - S_baseYaw * S_basePitch * S_baseRoll;
            a6 = S_baseYaw * S_basePitch * C_baseRoll + C_baseYaw * S_baseRoll;
            a7 = S_basePitch;
            a8 = -C_basePitch * S_baseRoll;
            a9 = C_basePitch * C_baseRoll;

            det = a1 * a5 * a9 + a4 * a8 * a3 + a7 * a2 * a6 - a1 * a8 * a6 - a7 * a5 * a3 - a4 * a2 * a9;

            c1 = (a5 * a9 - a6 * a8) / det;
            c2 = (a3 * a8 - a2 * a9) / det;
            c3 = (a2 * a6 - a3 * a5) / det;
            c4 = (a6 * a7 - a4 * a9) / det;
            c5 = (a1 * a9 - a3 * a7) / det;
            c6 = (a3 * a4 - a1 * a6) / det;
            c7 = (a4 * a8 - a5 * a7) / det;
            c8 = (a2 * a7 - a1 * a8) / det;
            c9 = (a1 * a5 - a2 * a4) / det;

            RePrepare = false;

        }
        public override void Execute()
        {
            Spher2Cart4CMD();
            World2Ped4CMD();
            Cart2Sphere4CMD();

            PedestalElevationCMD = PedestalElevation + PedestalOffsetEl;
            PedestalAzimuthCMD = PedestalAzimuth + PedestalOffsetAz;

            Spher2Cart4FBK();
            Ped2World4FBK();
            Cart2Sphere4FBK();
        }
        #endregion
        #region Sub-Functions of Xform Class
        void Spher2Cart4CMD()
        {
            wCMDX = Math.Cos(WorldAzimuthCMD) * Math.Cos(WorldElevationCMD);
            wCMDY = Math.Sin(WorldAzimuthCMD) * Math.Cos(WorldElevationCMD);
            wCMDZ = Math.Sin(WorldElevationCMD);
        }
        void World2Ped4CMD()
        {

            pCMDX = wCMDX * a1 + wCMDY * a2 + wCMDZ * a3;
            pCMDY = wCMDX * a4 + wCMDY * a5 + wCMDZ * a6;
            pCMDZ = wCMDX * a7 + wCMDY * a8 + wCMDZ * a9;
        }
        void Cart2Sphere4CMD()
        {
            if (pCMDY < 0 && pCMDZ >= 0)
            {
                PedestalAzimuth = -Math.Acos(pCMDX / Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
                PedestalElevation = Math.Acos(Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
            }
            else if (pCMDZ < 0 && pCMDY >= 0)
            {
                PedestalAzimuth = Math.Acos(pCMDX / Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
                PedestalElevation = -Math.Acos(Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
            }
            else if (pCMDZ < 0 && pCMDY < 0)
            {
                PedestalAzimuth = -Math.Acos(pCMDX / Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
                PedestalElevation = -Math.Acos(Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
            }
            else
            {
                PedestalAzimuth = Math.Acos(pCMDX / Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
                PedestalElevation = Math.Acos(Math.Sqrt(pCMDX * pCMDX + pCMDY * pCMDY));
            }
            if (PedestalAzimuth != PedestalAzimuth)
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Invalid Pedestal Azimuth Command Calculated");
            if (PedestalElevation != PedestalElevation)
                this.MessagesLink.NewMessage(this, this.UniverseTimer.Time, MessageType.Alarm, "Invalid Pedestal Elevation Command Calculated");

        }
        void Spher2Cart4FBK()
        {
            pFBKX = Math.Cos(PedestalAzFBK) * Math.Cos(PedestalElFBK);
            pFBKY = Math.Sin(PedestalAzFBK) * Math.Cos(PedestalElFBK);
            pFBKZ = Math.Sin(PedestalElFBK);
        }
        void Ped2World4FBK()
        {
            wFBKX = pFBKX * c1 + pFBKY * c2 + pFBKZ * c3;
            wFBKY = pFBKX * c4 + pFBKY * c5 + pFBKZ * c6;
            wFBKZ = pFBKX * c7 + pFBKY * c8 + pFBKZ * c9;
        }
        void Cart2Sphere4FBK()
        {
            if (wFBKY < 0 && wFBKZ >= 0)
            {
                WorldAzFBK = -Math.Acos(wFBKX / Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
                WorldElFBK = Math.Acos(Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
            }
            else if (wFBKZ < 0 && wFBKY >= 0)
            {
                WorldAzFBK = Math.Acos(wFBKX / Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
                WorldElFBK = -Math.Acos(Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
            }
            else if (wFBKZ < 0 && wFBKY < 0)
            {
                WorldAzFBK = -Math.Acos(wFBKX / Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
                WorldElFBK = -Math.Acos(Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
            }
            else
            {
                WorldAzFBK = Math.Acos(wFBKX / Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
                WorldElFBK = Math.Acos(Math.Sqrt(wFBKX * wFBKX + wFBKY * wFBKY));
            }

        }
        #endregion
    }
}
