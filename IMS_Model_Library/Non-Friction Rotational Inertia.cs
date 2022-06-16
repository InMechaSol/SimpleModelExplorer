using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSolModelLibrary
{
    class Non_Friction_Rotational_Inertia : BaseModelClass
    {
        #region Global Variables

        double appliedTorque;                    // Applied torque on the cylinder
        double angAccel;                         // Instantaneous angular acceleration of the cylinder
        double angVelocity;                      // Instantaneous angular velocity of the cylinder
        double theta;                            // Instantaneous angular position of the cylinder
        double mass;                             // Mass of the cylinder
        double depth;                            // Height of the cylinder 
        double radius;                           // Radius of the cylinder
        double cylinderInertia;                  // Moment of inertia of the cylinder
        double initialAngVelocity;               // Initial angular velocity of the cylinder
        double initialTheta;                     // Initial angular position of the cylinder
        double power;                            // Power of the cylinder
        double rotationalEnergy;                 // Rotational Kinetic Energy of the cylinder

        #endregion

        #region Preporties Browser

        //Implement properties for property grid
        [property: Category("Input Parameters"), Description("Applied Torque (N*m)"), DefaultValue(1.0)]
        public double AppliedTorque
        {
            get { return appliedTorque; }
            set { appliedTorque = value; this.RePrepare = true; }
        }

        [property: Category("Input Parameters"), Description("Disc Mass (kg)"), DefaultValue(0.0)]
        public double Mass
        {
            get { return mass; }
            set { mass = value; this.RePrepare = true; }
        }

        [property: Category("Input Parameters"), Description("Disc Radius (m)"), DefaultValue(0.0)]
        public double Radius
        {
            get { return radius; }
            set { radius = value; this.RePrepare = true; }
        }

        [property: Category("Input Parameters"), Description("Disc Depth (m)"), DefaultValue(0.0)]
        public double Depth
        {
            get { return depth; }
            set { depth = value; this.RePrepare = true; }
        }

        [property: Category("Input Parameters"), Description("Angular Velocity (rad/s)"), DefaultValue(0.0)]
        public double InitialAngVelocity
        {
            get { return initialAngVelocity; }
            set { initialAngVelocity = value; this.RePrepare = true; }
        }

        [property: Category("Input Parameters"), Description("Angular Position (rad)"), DefaultValue(0.0)]
        public double InitialAngularPosition
        {
            get { return initialTheta; }
            set { initialTheta = value; this.RePrepare = true; }
        }

        [property: Category("Output Parameters"), Description("Angular Position (rad)"), DefaultValue(0.0)]
        public double Theta
        {
            get { return theta; }
            set { theta = value; this.RePrepare = true; }
        }

        [property: Category("Output Parameters"), Description("Angular Velocity (rad/s)"), DefaultValue(0.0)]
        public double AngVelocity
        {
            get { return angVelocity; }
            set { angVelocity = value; this.RePrepare = true; }
        }

        [property: Category("Output Parameters"), Description("Angular Acceleration (rad/s^2)"), DefaultValue(0.0)]
        public double AngAccel
        {
            get { return angAccel; }
            set { angAccel = value; this.RePrepare = true; }
        }

        [property: Category("Output Parameters"), Description("Power (W)"), DefaultValue(0.0)]
        public double Power
        {
            get { return power; }
            set { power = value; this.RePrepare = true; }
        }

        [property: Category("Output Parameters"), Description("Rotational Energy (J)"), DefaultValue(0.0)]
        public double RotationalEnergy
        {
            get { return rotationalEnergy; }
            set { rotationalEnergy = value; this.RePrepare = true; }
        }

        [property: Category("Internal Parameters"), Description("Inertia of the System (kg*m^2)"), DefaultValue(0.0)]
        public double Inertia
        {
            get { return cylinderInertia; }
            set { cylinderInertia = value; this.RePrepare = true; }
        }

        #endregion

        // Default Constructor
        public Non_Friction_Rotational_Inertia()
        {

        }

        // Implements execution system funtions
        public override void Initialize()
        {

            ReInitialize = false;

        }

        // Calculate fixed values during run time
        public override void Prepare()
        {
            cylinderInertia = (.5) * mass * radius * radius;

            angVelocity = initialAngVelocity;
            theta = initialTheta;

            RePrepare = false;
        }

        // Execute main functions
        public override void Execute()
        {
            // Calculate the instantaneous angular acceleration
            angAccel = appliedTorque / cylinderInertia;

            // Calculate the instantaneous angular velocity
            angVelocity += angAccel * this.UniverseTimer.Resolution;

            // Calculate the instantaneous angular position
            theta += angVelocity * this.UniverseTimer.Resolution;

            // Calculate the power 
            power = appliedTorque * angVelocity;

            // Calculate the rotational kinetic energy
            rotationalEnergy = cylinderInertia * angVelocity * angVelocity / 2;
        }
    }
}