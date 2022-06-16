using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSolModelLibrary
{
    public class DCMotorGearCouple : BaseModelClass
    {
        //declare private variable parameters
        double resistance, inductance, voltage, backEmf, current, didit, kt, ke, inertiaG, inertiaR, angAccelG, angAccelR, angSpeedG, angSpeedR, thetaG, thetaR, dampingC, springC, forceDamping, forceSpring, netTorqueG, netTorqueR, forceMotor, wCmd, kv;



        // Implement properties for property grid
        [property: Category("Input Parameters"), Description("Resistance of DC Motor Circuit (olms)"), DefaultValue(10.0)]
        public double Resistance
        {
            get { return resistance; }
            set { this.RePrepare = true; resistance = value; }
        }

        [property: Category("Input Parameters"), Description("Inductance of DC Motor Circuit (H)"), DefaultValue(0.01)]
        public double Inductance
        {
            get { return inductance; }
            set { this.RePrepare = true; inductance = value; }
        }

        [property: Category("Output Parameters"), Description("Voltage of DC Motor Circuit (v)"), DefaultValue(10.0)]
        public double Voltage
        {
            get { return voltage; }
            set { this.RePrepare = true; voltage = value; }
        }

        [property: Category("Input Parameters"), Description("Back Emf Constant"), DefaultValue(0.001)]
        public double Ke
        {
            get { return ke; }
            set { this.RePrepare = true; ke = value; }
        }

        [property: Category("Input Parameters"), Description("Motor Torque Constant"), DefaultValue(0.0)]
        public double Kt
        {
            get { return kt; }
            set { this.RePrepare = true; kt = value; }
        }

        [property: Category("Input Parameters"), Description("Damping Constant of Rotors"), DefaultValue(3.0)]
        public double DampingC
        {
            get { return dampingC; }
            set { this.RePrepare = true; dampingC = value; }
        }

        [property: Category("Input Parameters"), Description("Spring Constant of Rotors (N*m)"), DefaultValue(10.0)]
        public double SpringC
        {
            get { return springC; }
            set { this.RePrepare = true; springC = value; }
        }

        [property: Category("Input Parameters"), Description("Inertia of Rotor G"), DefaultValue(1.0)]
        public double InertiaG
        {
            get { return inertiaG; }
            set { this.RePrepare = true; inertiaG = value; }
        }

        [property: Category("Input Parameters"), Description("Inertia of Rotor R"), DefaultValue(1.0)]
        public double InertiaR
        {
            get { return inertiaR; }
            set { this.RePrepare = true; inertiaR = value; }
        }

        [property: Category("Output Parameters"), Description("Current of DC Motor Circuit (A)"), DefaultValue(0.25)]
        public double Current
        {
            get { return current; }
            set { this.RePrepare = true; current = value; }
        }

        [property: Category("Output Parameters"), Description("Back EMF (v)"), DefaultValue(0.0)]
        public double BackEmf
        {
            get { return backEmf; }
            set { this.RePrepare = true; backEmf = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Acceleration of Rotor G (rad/s/s)"), DefaultValue(0.0)]
        public double AngAccelG
        {
            get { return angAccelG; }
            set { this.RePrepare = true; angAccelG = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Acceleration of R (rad/s/s)"), DefaultValue(0.0)]
        public double AngAccelR
        {
            get { return angAccelR; }
            set { this.RePrepare = true; angAccelR = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Speed of Rotor R (rad/s)"), DefaultValue(0.0)]
        public double AngSpeedR
        {
            get { return angSpeedR; }
            set { this.RePrepare = true; angSpeedR = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Speed of Rotor G (rad/s)"), DefaultValue(0.0)]
        public double AngSpeedG
        {
            get { return angSpeedG; }
            set { this.RePrepare = true; AngSpeedG = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Position of Rotor R (rad)"), DefaultValue(0.0)]
        public double ThetaR
        {
            get { return thetaR; }
            set { this.RePrepare = true; thetaR = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Position of Rotor G (rad)"), DefaultValue(0.0)]
        public double ThetaG
        {
            get { return thetaG; }
            set { this.RePrepare = true; thetaG = value; }
        }

        [property: Category("Output Parameters"), Description("Force due to Damping (N)"), DefaultValue(0.0)]
        public double ForceDamping
        {
            get { return forceDamping; }
            set { this.RePrepare = true; forceDamping = value; }
        }

        [property: Category("Output Parameters"), Description("Force due to Spring Componet of Rotors (N)"), DefaultValue(0.0)]
        public double ForceSpring
        {
            get { return forceSpring; }
            set { this.RePrepare = true; forceSpring = value; }
        }

        [property: Category("Output Parameters"), Description("Net Torque of Rotor G (N)"), DefaultValue(0.0)]
        public double NetForceG
        {
            get { return netTorqueG; }
            set { this.RePrepare = true; netTorqueG = value; }
        }

        [property: Category("Output Parameters"), Description("Net Torque of Rotor R (N)"), DefaultValue(0.0)]
        public double NetTorqueR
        {
            get { return netTorqueR; }
            set { this.RePrepare = true; netTorqueR = value; }
        }

        [property: Category("Input Parameters"), Description("Controller Command for Angular Speed (rad/s)"), DefaultValue(0.0)]
        public double WCmd
        {
            get { return wCmd; }
            set { this.RePrepare = true; wCmd = value; }
        }

        [property: Category("Output Parameters"), Description("Contolller Command Constant"), DefaultValue(1.0)]
        public double Kv
        {
            get { return kv; }
            set { this.RePrepare = true; kv = value; }
        }

        // Constructor
        public DCMotorGearCouple()
        {

            this.resistance = 10.2;
            this.inductance = 0.2E-3;
            this.backEmf = 0.0;
            this.voltage = 4.50;
            this.dampingC = 5E-6;
            this.springC = 70;
            this.current = 0.25;
            this.didit = 0.0;
            this.angAccelG = 0.0;
            this.angAccelR = 0.0;
            this.angSpeedG = 0.0;
            this.angSpeedR = 0.0;
            this.thetaG = 0.0;
            this.thetaR = 0.0;
            this.forceDamping = 0.0;
            this.forceSpring = 0.0;
            this.netTorqueG = 0.0;
            this.netTorqueR = 0.0;
            this.inertiaG = 0.0000011199999999999999;
            this.inertiaR = 0.0000011199999999999999;
            this.kt = 8.0;
            this.ke = 125E-3;
            this.kv = 1.0;
            this.wCmd = 3.0;

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

            //Calculate the Controller Response
            //voltage = (wCmd - angSpeedG) * kv;

            //Calculate Back EMF
            backEmf = ke * angSpeedR;

            //Calculate Force Componets of Rotor R
            forceDamping = dampingC * (angSpeedG - angSpeedR);
            forceSpring = springC * (thetaG - thetaR);
            forceMotor = 1 / ke * current;

            //Calculate Net Torque for Rotor R
            netTorqueR = (forceMotor + forceDamping + forceSpring);

            //Calculate Dynamics of Rotor R
            angAccelR = 1 / inertiaR * netTorqueR;
            didit = 1 / inertiaR * (-(current * resistance) * (1 / inductance) - (backEmf / inductance) + (voltage / inductance));
            current += didit * this.UniverseTimer.Resolution;
            angSpeedR += angAccelR * this.UniverseTimer.Resolution;
            thetaR += angSpeedR * this.UniverseTimer.Resolution;

            //Calculate Net Torque of Rotor G
            netTorqueG = -forceDamping - forceSpring;

            //Calculate Dynamics of Rotor G
            angAccelG = netTorqueG / inertiaG;
            angSpeedG += angAccelG * this.UniverseTimer.Resolution;
            thetaG += angSpeedG * this.UniverseTimer.Resolution;

        }

    }
}
