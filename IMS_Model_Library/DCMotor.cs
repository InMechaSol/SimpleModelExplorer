using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSolModelLibrary
{
    public class DCMotor : BaseModelClass
    {
        //declare private variable parameters
        double inertia, angAccel, angSpeed, angPosi, voltage, current, resistance, inductance, torqueCons, emf, currentTerm, speedTerm, didt;



        // Implement properties for property grid
        [property: Category("Input Parameters"), Description("Inertia (kg*m^2"), DefaultValue(1.0)]
        public double Inertia
        {
            get { return inertia; }
            set { this.RePrepare = true; inertia = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Acceleration (rad/s/s"), DefaultValue(0.0)]
        public double AngAccel
        {
            get { return angAccel; }
            set { this.RePrepare = true; angAccel = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Speed (rad/s)"), DefaultValue(0.0)]
        public double AngSpeed
        {
            get { return angSpeed; }
            set { this.RePrepare = true; angSpeed = value; }
        }

        [property: Category("Output Parameters"), Description("Angular Position (rad)"), DefaultValue(0.0)]
        public double AngPosi
        {
            get { return angPosi; }
            set { this.RePrepare = true; angPosi = value; }
        }

        [property: Category("Input Parameters"), Description("Voltage (v)"), DefaultValue(0.0)]
        public double Voltage
        {
            get { return voltage; }
            set { this.RePrepare = true; voltage = value; }
        }

        [property: Category("Output Parameters"), Description("Current (A)"), DefaultValue(5.0)]
        public double Current
        {
            get { return current; }
            set { this.RePrepare = true; current = value; }
        }

        [property: Category("Input Parameters"), Description("Resistance (olms)"), DefaultValue(100.0)]
        public double Resistance
        {
            get { return resistance; }
            set { this.RePrepare = true; resistance = value; }
        }

        [property: Category("Input Parameters"), Description("Inductance (H)"), DefaultValue(0.001)]
        public double Inductance
        {
            get { return inductance; }
            set { this.RePrepare = true; inductance = value; }
        }

        [property: Category("Input Parameters"), Description("Machine Torque Constant (N*M*1/A)"), DefaultValue(0.5)]
        public double TorqueCons
        {
            get { return torqueCons; }
            set { this.RePrepare = true; torqueCons = value; }
        }

        // Constructor
        public DCMotor()
        {

            this.inertia = 1.0;
            this.angAccel = 0.0;
            this.angSpeed = 0.0;
            this.angPosi = 0.0;
            this.voltage = 10.0;
            this.current = 0.25;
            this.resistance = 10;
            this.inductance = 0.1;
            this.torqueCons = 0.793;
            this.emf = 0.0;
            this.currentTerm = 0.0;
            this.speedTerm = 0.0;
            this.didt = 0.0;

        }

        //Implements execution system funtions
        public override void Initialize()
        {

            ReInitialize = false;

        }

        public override void Prepare()
        {
            emf = 1 / torqueCons;
            currentTerm = resistance / inductance;
            speedTerm = emf / inductance;
            RePrepare = false;

        }

        public override void Execute()
        {
            //Calculate voltage and rate of change of current
            didt = voltage / inductance - current * currentTerm - angSpeed * speedTerm;

            //Calculate Dynamics
            current += didt * this.UniverseTimer.Resolution;
            angAccel = (torqueCons / inertia) * current;
            angSpeed += angAccel * this.UniverseTimer.Resolution;
            angPosi += angSpeed * this.UniverseTimer.Resolution;

        }

    }
}
