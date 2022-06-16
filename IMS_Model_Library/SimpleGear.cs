using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;



namespace InMechaSolModelLibrary
{
    public class SimpleGear : BaseModelClass
    {
        //Declare private variables(Fields)
        double appliedTorque, inertia, angAccel, theta, angSpeed, frictionCoes, frictionCoek, frictionTorque, netTorque, mass, depth, radius, zeroPoint = .00001;

        //Implement properties for property grid
        [property:Category("Input Parameters"), Description("Applied Torque(Nm)"), DefaultValue(1.0)]
        public double AppliedTorque
        {
            get

            { return appliedTorque; }
            set { appliedTorque = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Input Parameters"), Description("Inertia of the System(kg*m^2)"), DefaultValue(0.0)]
        public double Inertia
        {
            get { return inertia; }
            set { inertia = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Output Parameters"), Description("Angular Position (rad)"), DefaultValue(0.0)]
        public double Theta
        {
            get { return theta; }
            set { theta = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Output Parameters"), Description("Angular Speed (rad/s)"), DefaultValue(0.0)]
        public double AngSpeed
        {
            get { return angSpeed; }
            set { angSpeed = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Output Parameters"), Description("Angular Acceleration (rad/s/s"), DefaultValue(0.0)]
        public double AngAccel
        {
            get { return angAccel; }
            set { angAccel = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Input Parameters"), Description("Coefficient of static friction (unitless)"), DefaultValue(0.1)]
        public double FrictionCoes
        {
            get { return frictionCoes; }
            set { frictionCoes = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Input Parameters"), Description("Coefficient of kinetic friction (unitless)"), DefaultValue(0.5)]
        public double FrictionCoek
        {
            get { return frictionCoek; }
            set { frictionCoek = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Output Parameters"), Description("Frictional Torque(N)"), DefaultValue(0.0)]
        public double FrictionTorque
        {
            get { return frictionTorque; }
            set { frictionTorque = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Output Parameters"), Description("Net Torque (N)"), DefaultValue(0.0)]
        public double NetTorque
        {
            get { return netTorque; }
            set { netTorque = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Input Parameters"), Description("Disc Mass (kg)"), DefaultValue(0.0)]
        public double Mass
        {
            get { return mass; }
            set { mass = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Input Parameters"), Description("Disc Radius (m)"), DefaultValue(0.0)]
        public double Radius
        {
            get { return radius; }
            set { radius = value; this.RePrepare = true; }
        }

        [CategoryAttribute("Input Parameters"), Description("Disc Depth (m)"), DefaultValue(0.0)]
        public double Depth
        {
            get { return depth; }
            set { depth = value; this.RePrepare = true; }
        }



        //Declare internal calculation variables


        //Constructor
        public SimpleGear()
        {
            this.mass = 1.0;
            this.depth = 0.1;
            this.radius = 0.2;
            this.appliedTorque = 0.25;
            this.inertia = 1.0;
            this.theta = 0.0;
            this.angSpeed = 0.0;
            this.AngAccel = 0.0;
            this.frictionTorque = 0.0;
            this.frictionCoes = 2.0;
            this.frictionCoek = 1.5;
            this.netTorque = 0.0;
            this.zeroPoint = .00001;

        }

        //Implements execution system funtions
        public override void Initialize()
        {

            ReInitialize = false;

        }

        public override void Prepare()
        {
            inertia = (.5) * mass * radius * radius;
            RePrepare = false;

        }

        public override void Execute()
        {

            if ((angSpeed < zeroPoint) && (angSpeed > -zeroPoint))
            {
                frictionTorque = frictionCoes * inertia * Math.Sign(-angSpeed);               
            }
            else
                frictionTorque = frictionCoek * inertia * Math.Sign(-angSpeed);


             // Calculate Net Torque
             netTorque = appliedTorque + frictionTorque;


            // Calculate Resulting Dynamics
            theta += angSpeed * this.UniverseTimer.Resolution;
            angSpeed += angAccel * this.UniverseTimer.Resolution;
            angAccel = netTorque / inertia;

            
            


            

        }
    }
}
