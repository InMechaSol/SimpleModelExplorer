using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace SimpleModelExplorer
{
    public class UniverseTimer : BaseModelClass
    {
        #region Members of Universe Timer Class
        double time, resolution;
        public DateTime GUITime;
        #endregion
        #region Properties of Universe Timer Class
        [Category("Timer Parameters"), Description("The time resolution in seconds of the universe timer; with each call of execute(), time is incremented by resolution")]
        public double Resolution
        {
            set { resolution = value; }
            get { return resolution; }
        }
        [Category("Timer Parameters"), ReadOnly(true), Description("The absolute time of the entire universe (simulated seconds)")]
        public double Time
        {
            set { time = value; }
            get { return time; }
        }
        #endregion
        #region Execution System Function of Timer Class
        // Called on after load and after reset in GUI timer thread only when block thread is not active
        public override void Initialize()
        {
            time = 0.0;
            if (resolution == 0.0)
                resolution = 0.00000001;
            //this.MessagesLink.NewMessage(this, time, MessageType.Info, "Universe Time Reset");
        }
        // Called on demand when selectedmodel.rePrepare == true on the next block exe cycle
        public override void Prepare()
        {
            if (resolution == 0.00000001)
                resolution = 0.1;
            //this.MessagesLink.NewMessage(this, time, MessageType.Info, "Universe Time Resolution Adjusted");
        }
        // Called once per block exe cycle
        public override void Execute()
        {
            time += resolution;     // Simply Increment the master time reference by a constant resolution
        }
        #endregion
    }
}
