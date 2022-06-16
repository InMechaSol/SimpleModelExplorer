using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace SimpleModelExplorer
{
    public enum MessageType { Info, Warning, Alarm }
    public class ModelMessenger
    {
        List<string> Messages = new List<string>();
        List<System.Drawing.Color> MsgColors = new List<System.Drawing.Color>();
        public int NewMessages
        {
            get { return Messages.Count; }
            set {; }
        }
        public void NewMessage(BaseModelClass thisModel, double UniverseTime, MessageType TypeOfMessage, string Message)
        {
            switch(TypeOfMessage)
            {
                case MessageType.Alarm: MsgColors.Add(System.Drawing.Color.Red);
                    break;
                case MessageType.Warning: MsgColors.Add(System.Drawing.Color.Blue);
                    break;
                case MessageType.Info: MsgColors.Add(System.Drawing.Color.Black);
                    break;

            }
            if(thisModel!=null)
                this.Messages.Add(String.Concat(UniverseTime.ToString("F1"), " : ", TypeOfMessage.ToString(), " : " + thisModel.GetType().Name, " : ", Message+"\n"));
            else
                this.Messages.Add(String.Concat(UniverseTime.ToString("F1"), " : ", TypeOfMessage.ToString(), " : SimpleModelExplorer : ", Message + "\n"));
        
        }
        public string RemoveMessage()
        {
            string RemoveMessage = Messages[0];
            Messages.RemoveAt(0);
            return RemoveMessage;
        }
        public System.Drawing.Color RemoveMessageColor()
        {
            System.Drawing.Color mColor = MsgColors[0];
            MsgColors.RemoveAt(0);
            return mColor;
        }

    }
    public class curveTag
    {
        public PropertyInfo pinfo;
        public BaseModelClass callingModel;
        public bool isForY2Axis = false;
        public ZedGraph.CurveItem LinktoLineItem;
        public curveTag(PropertyInfo p, BaseModelClass b)
        {
            pinfo = p;
            callingModel = b;
        }

    }

    public class BaseModelClassConverter : ExpandableObjectConverter
    {
        // functions for property grid expandability
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return base.GetPropertiesSupported(context);
        }
        public override bool CanConvertTo(ITypeDescriptorContext context, System.Type destinationType)
        {
            if (destinationType == typeof(BaseModelClass))
                return true;

            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, System.Type destinationType)
        {
            if (destinationType == typeof(System.String) && value is BaseModelClass)
            {

                BaseModelClass so = (BaseModelClass)value;
                return so.Name;
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverterAttribute(typeof(BaseModelClassConverter)), DescriptionAttribute("Expand to see...")]
    public abstract class BaseModelClass
    {
        // public properties of the base model class exposing its private members
        [property: Category("Base Model Parameters"), Description("This is reference to the universe timer of the execution system"), Browsable(false)]
        virtual public UniverseTimer UniverseTimer
        {
            get { return uTimeLink; }
            set
            {
                uTimeLink = value;
                if (this.LocalSubModelList != null)
                    if (this.LocalSubModelList.Count > 1)
                        foreach (BaseModelClass subModel in this.LocalSubModelList)
                            subModel.UniverseTimer = value;
            }
        }
        [property: Category("Base Model Parameters"), Description("This is a reference to the model messenger of the exe system"), Browsable(false)]
        virtual public ModelMessenger MessagesLink
        {
            get { return messagesLink; }
            set
            {
                messagesLink = value;
                if (this.LocalSubModelList != null)
                    if (this.LocalSubModelList.Count > 1)
                        foreach (BaseModelClass subModel in this.LocalSubModelList)
                            subModel.MessagesLink = value;
            }
        }
        [property: Category("Base Model Parameters"), Description("The name of the model type"), ReadOnly(true)]
        public string Name
        {
            get { return this.GetType().FullName; }
            set {; }
        }
        [property: Category("Base Model Parameters"), Description("Set the reInitialize flag to call the initialize() function of the model on the next GUI Timer cycle"), Browsable(false)]
        public bool ReInitialize
        {
            get { return reInitialize; }
            set { reInitialize = value; }
        }
        [property: Category("Base Model Parameters"), Description("Set the rePrepare flag to call the prepare() function of the model on the next GUI Timer cycle"), Browsable(false)]
        public bool RePrepare
        {
            get { return rePrepare; }
            set { rePrepare = value; }
        }
        [property: Category("Base Model Parameters"), Description("List of submodels on contained in this model"), ReadOnly(true)]
        public List<BaseModelClass> LocalSubModelList
        {
            get { return localSubModels; }
            set { localSubModels = value; }
        }
        [property: Category("Base Model Parameters"), Description("List of models and submodels on which to execute in order of list index for local model"), ReadOnly(true)]
        public List<BaseModelClass> LocalModelExeMap
        {
            get { return localExeMap; }
            set { localExeMap = value; }
        }
        [property: Category("Base Model Parameters"), Description("List of models and submodels on which to execute in order of list index for complete global model"), ReadOnly(true)]
        public List<BaseModelClass> MainModelExeMap
        {
            get { return ModelEXEMap; }
            set { ModelEXEMap = value; }
        }
        [property: Category("Base Model Parameters"), Description("List of properties selected for data logging"), Browsable(false)]
        public List<curveTag> Props2PlotList
        {
            get { return props2plot; }
            set { props2plot = value; }
        }
        [Category("Base Model Parameters"), Description("List of nested models to not expand in property browsers")]
        public List<BaseModelClass> ModelsNotExpanded
        {
            get { return expansionExcludes; }
            set { expansionExcludes = value; }
        }

        // private members of the base class
        bool reInitialize, rePrepare;           // exe system flags, trigger initialize and prepare functions in GUI Timer Cycle
        UniverseTimer uTimeLink;                // exe system universe timer object link
        ModelMessenger messagesLink;            // exe system message queue link   
        List<BaseModelClass> ModelEXEMap;
        List<BaseModelClass> localSubModels;
        List<BaseModelClass> localExeMap;       // list of (sub)models for execution system functions
        List<BaseModelClass> expansionExcludes; //  
        List<curveTag> props2plot;              // list of selected properties for plotting
        bool stepIn;
        int subSysIndex;
        public bool autoLoad;
        // execution system functions
        public abstract void Initialize();
        public abstract void Prepare();
        public abstract void Execute();
        // BuildRootExeMap() is an important function that traverses the entire system
        // graph creating a linear list of references (pointers) to all subsystems of
        // the complete connected graph.
        // Along the way some important indecies, flags, and node references are set:
        // - Index of each system node within the root execution map
        // - Flag indicating root node
        // - Link to root node
        public void buildRootExeMap(UniverseTimer exeUtime, ModelMessenger exeModMess)
        {
            List<BaseModelClass> NodesSteppedIn = new List<BaseModelClass>();			// list of nodes stepped in to
            BaseModelClass currentSteppedNode;										// link to currently stepped node
            BaseModelClass currentSubNode;                                           // link to currently subnode

            // clear the global/model list
            if (this.ModelEXEMap == null)
                this.ModelEXEMap = new List<BaseModelClass>();
            this.ModelEXEMap.Clear();

            // build the local list
            this.BuildLocalExeMap(exeUtime, exeModMess);

            // put universe timer and this root model at the front of the exe list
            this.ModelEXEMap.Add(this.UniverseTimer);
            this.ModelEXEMap.Add(this.localExeMap[0]);

            // determine whether to traverse sub systems (stepIn)			
            if (this.localExeMap.Count > 1)
            {
                this.stepIn = true;
                this.subSysIndex = 1;
                // add this node to the steppedin list
                NodesSteppedIn.Add(this);
            }

            // traverse the subs completely adding to root sys exe map
            while (this.stepIn)
            {
                // Add the next sub to the end of the root sys exe map
                currentSteppedNode = NodesSteppedIn[NodesSteppedIn.Count - 1];						// grabbed from end of stepped node list
                // check for exit, pop node from list if done processing
                if (currentSteppedNode.subSysIndex >= currentSteppedNode.localExeMap.Count)
                {
                    currentSteppedNode.stepIn = false;							// indicate no further need for step in
                    NodesSteppedIn.Remove(currentSteppedNode);					// remove currently stepped node from stepped list
                    if (NodesSteppedIn.Count > 0)
                        NodesSteppedIn[NodesSteppedIn.Count - 1].subSysIndex++;
                }
                else
                {
                    currentSubNode = currentSteppedNode.localExeMap[currentSteppedNode.subSysIndex];			// grabbed from end of local exemap of current stepped node
                    currentSubNode.BuildLocalExeMap(exeUtime, exeModMess);													// build local exe map first
                    this.ModelEXEMap.Add(currentSubNode);

                    // does the current sub have subs?			
                    if (currentSubNode.localExeMap.Count > 1)
                    {
                        currentSubNode.stepIn = true;				// indicate need for step in
                        currentSubNode.subSysIndex = 1;				// reset local exemap array index
                        NodesSteppedIn.Add(currentSubNode);			// add current sub to end of stepped list
                    }
                    // otherwise return to last steppedin node for completion
                    else
                    {
                        currentSubNode.stepIn = false;				// indicate no need for step in
                        currentSteppedNode.subSysIndex++;			// increment subsystem local exe map index

                        // check for exit, pop node from list if done processing
                        if (currentSteppedNode.subSysIndex >= currentSteppedNode.localExeMap.Count)
                        {
                            currentSteppedNode.stepIn = false;							// indicate no further need for step in
                            NodesSteppedIn.Remove(currentSteppedNode);					// remove currently stepped node from stepped list
                            if (NodesSteppedIn.Count > 0)
                                NodesSteppedIn[NodesSteppedIn.Count - 1].subSysIndex++;
                        }
                    }
                }
            }
        }
        // BuildLocalExeMap()
        // Create an ordered list of references to the contained subsystems of this system
        // Along the way, set an important index:
        // - Index of each system node within its parent execution map
        public virtual void BuildLocalExeMap(UniverseTimer exeUtime, ModelMessenger exeModMess)
        {
            if (this.localExeMap == null)
                this.localExeMap = new List<BaseModelClass>();
            this.localExeMap.Clear();

            this.UniverseTimer = exeUtime;
            this.MessagesLink = exeModMess;
            this.localExeMap.Add(this);
            
            if (this.localSubModels != null)
                if (this.localSubModels.Count > 0)
                    foreach (BaseModelClass subModel in this.localSubModels)
                    {
                        subModel.UniverseTimer = exeUtime;
                        subModel.MessagesLink = exeModMess;
                        this.localExeMap.Add(subModel);
                    }
                        
        }
        // GUI functions
        virtual public void ListPropertiestoTreeNode(System.Windows.Forms.TreeNode tNode)
        {
            // clear and name root node
            tNode.Nodes.Clear();
            if (tNode == tNode.TreeView.Nodes[0])
            {
                tNode.Name = this.Name;
                tNode.Text = tNode.Name;
            }

            // Create list of property categories
            List<string> PropCategories = new List<string>();
            string tempString;
            BaseModelClass tempNode;

            double tempValue;
            foreach (PropertyInfo pInfo in this.GetType().GetProperties())
            {
                tempString = ((CategoryAttribute)pInfo.GetCustomAttribute(typeof(CategoryAttribute))).Category.ToString();
                if (!PropCategories.Contains(tempString))
                {
                    PropCategories.Add(tempString);
                    tNode.Nodes.Add(tempString);
                    tNode.Nodes[tNode.Nodes.Count - 1].Name = tempString;
                    tNode.Nodes[tNode.Nodes.Count - 1].ForeColor = System.Drawing.Color.DarkBlue;
                }
            }

            // list properties of selected model
            foreach (PropertyInfo pInfo in this.GetType().GetProperties())
                if (pInfo.Name != "Name" && pInfo.Name != "UniverseTimer" && pInfo.Name != "MessagesLink" && pInfo.Name != "LocalSubModelList"
                    && pInfo.Name != "LocalModelExeMap" && pInfo.Name != "MainModelExeMap" && pInfo.Name != "Props2PlotList" && pInfo.Name != "ModelsNotExpanded")
                {
                    if (pInfo.PropertyType.BaseType == typeof(BaseModelClass))
                    {
                        try
                        {
                            tempNode = (BaseModelClass)pInfo.GetValue(this);
                            if (tempNode != null)
                            {
                                tempString = ((CategoryAttribute)pInfo.GetCustomAttribute(typeof(CategoryAttribute))).Category.ToString();
                                tNode.Nodes[tempString].Nodes.Add(pInfo.Name);
                                tNode.Nodes[tempString].Nodes[tNode.Nodes[tempString].Nodes.Count - 1].ForeColor = System.Drawing.Color.Green;
                                if (expansionExcludes == null)
                                    tempNode.ListPropertiestoTreeNode(tNode.Nodes[tempString].Nodes[tNode.Nodes[tempString].Nodes.Count - 1]);
                                else
                                    if (!expansionExcludes.Contains(tempNode))
                                    tempNode.ListPropertiestoTreeNode(tNode.Nodes[tempString].Nodes[tNode.Nodes[tempString].Nodes.Count - 1]);
                            }
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        try
                        {
                            if (pInfo.PropertyType != typeof(List<>))
                            {
                                tempValue = System.Convert.ToDouble(pInfo.GetValue(this));
                                if (tempValue == tempValue)
                                {
                                    tempString = ((CategoryAttribute)pInfo.GetCustomAttribute(typeof(CategoryAttribute))).Category.ToString();
                                    tNode.Nodes[tempString].Nodes.Add(pInfo.Name);
                                    tNode.Nodes[tempString].Nodes[tNode.Nodes[tempString].Nodes.Count - 1].Tag = new curveTag(pInfo, this);
                                    tNode.Nodes[tempString].Nodes[tNode.Nodes[tempString].Nodes.Count - 1].ForeColor = System.Drawing.Color.Red;
                                }
                            }

                        }
                        catch
                        {

                        }
                    }
                }

        }
    }
}
