using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSolModelLibrary
{
    class Friction_Rotational_Inertia : BaseModelClass
    {
        #region Private Members of the Model

        double appliedTorque;                    // Applied torque on the cylinder
        double angAccel;                         // Instantaneous angular acceleration of the cylinder
        double angVelocity;                      // Instantaneous angular velocity of the cylinder
        double theta;                            // Instantaneous angular position of the cylinder
        double cylinderInertia;                  // Moment of inertia of the cylinder
        double power;                            // Power of the cylinder
        double rotationalEnergy;                 // Rotational Kinetic Energy of the cylinder
        double breakfreetorque;                  // Static Friction Torque N*m
        double frictionTorque;                   // Friction Torque
        double netTorque;                        // Net Torque
        double zerospeed;                        // Floating point comparision value for "0" speed
        double efficiencysetting;                // The desired output of the system
        double efficiencyactual;                 // Current output of the system
        double efficiencyerror;                  // Error between reference and feedback
        double keff;                             // Some constant for calculation

        #endregion

        #region Public Properties exposing Members of the Model

        //Implement properties for property grid
        [Category("Torques"), Description("Applied Torque (N*m)")]
        public double AppliedTorque
        {
            get { return appliedTorque; }
            set { appliedTorque = value;}
        }
        [Category("Torques"), Description("Friction Torque (N*m)"), ReadOnly(true)]
        public double FrictionTorque
        {
            get { return frictionTorque; }
            set { frictionTorque = value; }
        }
        [Category("Torques"), Description("Net Torque (N*m)"), ReadOnly(true)]
        public double NetTorque
        {
            get { return netTorque; }
            set { netTorque = value; }
        }

        [Category("Dimensions and Mass"), Description("Inertia of the System (kg*m^2)")]
        public double Inertia
        {
            get { return cylinderInertia; }
            set { cylinderInertia = value; }
        }


        [Category("Friction and Efficiency"), Description("Static Friction Torque (Nm)")]
        public double StaticFrictionTorque
        {
            get { return breakfreetorque; }
            set { breakfreetorque = value;}
        }
        [Category("Friction and Efficiency"), Description("Static Friction Torque (Nm)")]
        public double Efficiency
        {
            get { return efficiencysetting; }
            set { efficiencysetting = value; }
        }
        [Category("Friction and Efficiency"), Description("Proportional Multiplier in Friction/Efficiency Loop")]
        public double kEfficinecy
        {
            get { return keff; }
            set { keff = value; }
        }
        [Category("Friction and Efficiency"), Description("Floating Point value for '0' speed")]
        public double ZeroSpeed
        {
            get { return zerospeed; }
            set { zerospeed = value;}
        }

        [Category("Position and Dynmaics"), Description("Angular Position (rad)"), ReadOnly(true)]
        public double Theta
        {
            get { return theta; }
            set { theta = value;}
        }
        [Category("Position and Dynmaics"), Description("Angular Velocity (rad/s)"), ReadOnly(true)]
        public double AngVelocity
        {
            get { return angVelocity; }
            set { angVelocity = value;}
        }
        [Category("Position and Dynmaics"), Description("Angular Acceleration (rad/s^2)"), ReadOnly(true)]
        public double AngAccel
        {
            get { return angAccel; }
            set { angAccel = value;}
        }

        [Category("Power and Energy"), Description("Power (W)"), ReadOnly(true)]
        public double Power
        {
            get { return power; }
            set { power = value;}
        }
        [Category("Power and Energy"), Description("Rotational Energy (J)"), ReadOnly(true)]
        public double RotationalEnergy
        {
            get { return rotationalEnergy; }
            set { rotationalEnergy = value;}
        }
        [Category("Power and Energy"), Description("Error in Friction Model Control Loop"), ReadOnly(true)]
        public double EfficiencyError
        {
            get { return efficiencyerror; }
            set { efficiencyerror = value;}
        }
        [Category("Power and Energy"), Description("Actual Efficiency of Inertia Pout/Pin"), ReadOnly(true)]
        public double InstantaneousEfficiency
        {
            get { return efficiencyactual; }
            set { efficiencyactual = value;}
        }

        #endregion

        // Default Constructor
        public Friction_Rotational_Inertia() { }

        // Implements execution system funtions
        public override void Initialize()
        {
            
            keff = 1;
            appliedTorque = 0.1;
            efficiencysetting = 0.9;
            // Set the initial values of angular velocities & positions
            angVelocity = 0;
            theta = 0;

        }

        // Calculate fixed values during run time
        public override void Prepare()
        {
            
        }

        // Execute main functions
        public override void Execute()
        {

            if (Math.Abs(angVelocity) <= zerospeed && Math.Abs(appliedTorque) < breakfreetorque)
            {
                frictionTorque = Math.Sign(angVelocity) * appliedTorque;
            }
            else if (Math.Abs(angVelocity) < zerospeed || Math.Abs(appliedTorque) <= zerospeed)
            {
                frictionTorque = keff * angVelocity;
            }
            else
            {
                frictionTorque = keff * angVelocity;
                keff = (1 - efficiencysetting) * appliedTorque / angVelocity;
            }

            

            // Calculate the Net Torque
            netTorque = appliedTorque - frictionTorque;            

            // Calculate the instantaneous angular acceleration
            angAccel = netTorque / cylinderInertia;

            // Calculate the instantaneous angular velocity
            angVelocity += angAccel * this.UniverseTimer.Resolution;

            // Calculate the instantaneous angular position
            theta += angVelocity * this.UniverseTimer.Resolution;

            // Calculate the power 
            power = netTorque * angVelocity;

            // Calculate the rotational kinetic energy
            rotationalEnergy = cylinderInertia * angVelocity * angVelocity / 2;

            efficiencyactual = netTorque / appliedTorque;
        }
    }
}