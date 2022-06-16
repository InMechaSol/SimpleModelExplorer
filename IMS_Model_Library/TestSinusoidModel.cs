using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSolModelLibrary

{
    public class TestSinusoidModel : BaseModelClass
    {
        // Declare private variable parameters
        double frequency, amplitude, phase;

        // Implement properties for property grid
        [property: Category("Input Parameters"), Description("Frequency of Oscillation (Hz)"), DefaultValue(1.0)]
        public double Frequency
        {
            get { return frequency; }
            set { this.RePrepare = true; frequency = value; }
        }
        [CategoryAttribute("Input Parameters"), Description("Amplitude of Sinusoid; A*sin(x)"), DefaultValue(1.0)]
        public double Amplitude
        {
            get { return amplitude; }
            set { amplitude = value; }
        }
        [CategoryAttribute("Input Parameters"), Description("Phase shift of Sinusoid (rad)"), DefaultValue(3.1417)]
        public double Phase
        {
            get { return phase; }
            set { phase = value; }
        }

        // Declare internal calculation variables
        double output;
        double radFreq;

        [CategoryAttribute("Output"), ReadOnly(true), Description("Calculated value of sinusoid")]
        public double Output
        {
            get { return output; }
            set { output = value; }
        }


        // Constructor
        public TestSinusoidModel()
        {

        }


        // Implement execution system functions
        public override void Initialize()
        {


        }

        public override void Prepare()
        {

        }

        public override void Execute()
        {

        }

    }

	}

