using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using SimpleModelExplorer;

namespace InMechaSol_Model_Library
{
    class P_Regulator : BaseModelClass
    {
        #region Global Variables

        double reference;                               // The desired output of the system
        double feedback;                                // Current output of the system
        double error;                                   // Error between reference and feedback
        double k;                                       // Some constant for calculation

        #endregion

        #region Preporties Browser

        //Implement properties for property grid
        [property: Category("Input Parameters"), Description(""), DefaultValue(1.0)]
        public double Reference
        {
            get { return reference; }
            set { reference = value; this.RePrepare = true; }
        }

        [property: Category("Input Parameters"), Description(""), DefaultValue(0.0)]
        public double kError
        {
            get { return k; }
            set { k = value; this.RePrepare = true; }
        }

        [property: Category("Output Parameters"), Description(""), DefaultValue(0.0)]
        public double Error
        {
            get { return error; }
            set { error = value; this.RePrepare = true; }
        }

        [property: Category("Internal Parameters"), Description(""), DefaultValue(0.0)]
        public double Reference_Feedback
        {
            get { return feedback; }
            set { feedback = value; this.RePrepare = true; }
        }
        
        #endregion

        // Default Constructor
        public P_Regulator() { }

        // Implements execution system funtions
        public override void Initialize()
        {

            ReInitialize = false;

        }

        // Calculate fixed values during run time
        public override void Prepare()
        {
            RePrepare = false;
        }

        // Execute main functions
        public override void Execute()
        {
            
        }

    }
}
