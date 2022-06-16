using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSolModelLibrary
{
     public class SimpleSpringOcscillator:BaseModelClass
    {
        //declare private variable parameters
        double mass, velocity, acceleration, position, springForce, springConstant, netForce, dampingForce, dampingCoe, gravitationalForce, accelGravity;

        // Implement properties for property grid
        [property: Category("Input Parameters"), Description("Mass (kg)"), DefaultValue(1.0)]
        public double Mass
        {
            get { return mass; }
            set { this.RePrepare = true; mass = value; }
        }

        [property: Category("Output Parameters"), Description("Position (m)"), DefaultValue(0.3)]
        public double Position
        {
            get { return position; }
            set { this.RePrepare = true; position = value; }
        }

        [property: Category("Output Parameters"), Description("Velocity (m/s)"), DefaultValue(0.0)]
        public double Velocity
        {
            get { return velocity; }
            set { this.RePrepare = true; velocity = value; }
        }

        [property: Category("Output Parameters"), Description("Acceleration (m/s/s"), DefaultValue(0.0)]
        public double Acceleration
        {
            get { return acceleration; }
            set { this.RePrepare = true; acceleration = value; }
        }

        [property: Category("Input Parameters"), Description("Spring Constant (N/m)"), DefaultValue(0.0)]
        public double SpringConstant
        {
            get { return springConstant; }
            set { this.RePrepare = true; springConstant = value; }
        }

        [property: Category("Input Parameters"), Description("Damping Coeffecient (unitless)"), DefaultValue(0.0)]
        public double DampingCoe
        {
            get { return dampingCoe; }
            set { this.RePrepare = true; dampingCoe = value; }
        }

        [property: Category("Output Parameters"), Description("Net Force (N)"), DefaultValue(0.0)]
        public double NetForce
        {
            get { return netForce; }
            set { this.RePrepare = true; netForce = value; }
        }

        [property: Category("Output Parameters"), Description("Force due to Gravity (N)"), DefaultValue(0.0)]
        public double GravitationalForce
        {
            get { return gravitationalForce; }
            set { this.RePrepare = true; gravitationalForce = value; }
        }

        [property: Category("Output Parameters"), Description("Damping Force (N)"), DefaultValue(0.0)]
        public double DampingForce
        {
            get { return dampingForce; }
            set { this.RePrepare = true; dampingForce = value; }
        }

        [property: Category("Output Parameters"), Description("Spring Force (N)"), DefaultValue(0.0)]
        public double SpringForce
        {
            get { return springForce; }
            set { this.RePrepare = true; springForce = value; }
        }

        // Constructor
        public SimpleSpringOcscillator()
        {

            this.mass = 0.5;
            this.position = 0.3;
            this.velocity = 0.0;
            this.acceleration = 0.0;
            this.springConstant = 50;
            this.dampingCoe = 3;
            this.springForce = 0.0;
            this.accelGravity = -9.81;

        }

        //Implements execution system funtions
        public override void Initialize()
        {

            ReInitialize = false;

        }

        public override void Prepare()
        {
            
            RePrepare = false;

        }

        public override void Execute()
        {
            //Calculate individual force parameters
            dampingForce = (dampingCoe) * velocity * -1;
            springForce = -(springConstant * position);
            gravitationalForce = mass * accelGravity;

            //Calculate net force
            netForce = dampingForce + springForce + gravitationalForce;

            //Calculate Dynamics
            acceleration = netForce / mass;
            velocity += acceleration * this.UniverseTimer.Resolution;
            position += velocity * this.UniverseTimer.Resolution;
               

        }
    
     }
}
