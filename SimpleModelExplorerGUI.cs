using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Reflection;
using System.Linq.Expressions;


using ILNumerics;
using ILNumerics.Drawing;
using ILNumerics.Drawing.Plotting;
using ZedGraph;
using AxVisioViewer;

namespace SimpleModelExplorer
{
    public partial class SimpleModelExplorerGUI : Form
    {
        #region Members of the SimpleModelExplorerGUI Class
        Assembly baseLib;                       // Reference to base library assmbly
        Assembly modelLib;                      // Declaration of the model library compiled assembly
        AxViewer myVisioViewer;
        List<BaseModelClass> loadedModels;      // Declaration of the active model database
        double tempxMin, tempxMax, tempyMin, tempYMax;
        double totMinY, totMaxY;
        BaseModelClass selectedModel;           // Reference to the active model
        Type selectedType;                      // Type reference of selected model
        PropertyInfo[] availableProperties;     // Cached reflected property info collection for selected model
        Type nestedType;                        // Type reference of nested model
        PropertyInfo[] nestedProperties;        // Cached reflected property info collection for selected model's nested models
        Func<UniverseTimer, double> propertyTimeResolver;
        List<List<Double>> blockDataBuffers;    // Fast and Flexible data buffers for selectable latching with minimal executional impact
        curveTag globalDragDropCurveTag;
        //List<ILArray<float>> ILDataBuffers;          // IlNumerics array object, full length data buffers for plotting etcs
        //ILPlotCube thisPlotCube;
        //ILLegend thisLegend;
        string webpageTitle;
        //DataPostProcessing PostProcessingGUI;   // GUI for advanced data exploration

        UniverseTimer uTime;                    // Declaration of the Universe Timer Model
        ModelMessenger ModelMessages;           // Model Messenger class
        bool timerRunning, timerRunningLast;    // Flags indicating state of universe time (on/off) and the previous state
        bool ResetComplete, SignalSelected;
        bool newData;                // Flag from calculator to GUI that new data is ready to present

        bool bRunDuration, bApplyWindow;
        double RunDuration, lastTime;
        double PlotWindow;

        double SamplesPerInterval, SecondsPerInterval, SamplesPerSecond, SamplesPerWindow;

        //DateTime lastdate;
        //TimeSpan CalcTimeSpan;

        BaseModelClass lastSelectedModel;
        int lastTimerInterval;
        double lastResolution;
        int updatecount;
        #endregion
        #region Main Execution System Functions
        // constructor
        public SimpleModelExplorerGUI()
        {
            // Run Visual Studio generated constructor code
            InitializeComponent();

            // Ensure the model library file is located and loads without error, otherwise infinite loop!!!!!!!
            //while(modelLib==null)
            //    DLLDialogButton_Click(null, null);

            // Initialize the block data buffers
            blockDataBuffers = new List<List<double>>();

            //ILDataBuffers = new List<ILArray<float>>();
            //thisPlotCube = new ILPlotCube();
            //thisLegend = new ILLegend("test one");
            //thisPlotCube.Add(thisLegend);
            //ilPanel1.Scene.Add(thisPlotCube);
            //ilPanel1.ShowUIControls = true;



            // Create reference to Base Library
            baseLib = Assembly.GetAssembly(typeof(BaseModelClass));

            // Initialize the Loaded Model Database and Universe Time
            uTime = new UniverseTimer();
            loadedModels = new List<BaseModelClass>();

            // Show the available librarys and models
            ListModels(baseLib);
            //ListModels(modelLib);

            this.plotsTabControl.SelectedIndex = 4;           
            webBrowser1.Visible = false;

            // Set and indicate time resolution
            veryLowdt01sToolStripMenuItem_Click(null, null);

            // Set initial plot window
            applyPlotWindow_Click(null, null);
            
            // Create the Model Messagner
            ModelMessages = new ModelMessenger();
        }
        // Action performed cyclically with each expiration of the windows timer object
        private void timer1_Tick(object sender, EventArgs e)
        {
            
            #region Run the execution system if a model is selected
            if (selectedModel != null)
            {
                // Detect Rising Edge
                if (timerRunning && !timerRunningLast)
                {
                    this.LibraryNModelTabControl.SelectedIndex = 2;

                    // Prepare block data buffers
                    blockDataBuffers.Clear();
                    blockDataBuffers.Capacity = this.chart1.Series.Count + 1;
                    blockDataBuffers.Add(new List<double>((int)(((float)this.GUI_Timer.Interval) / 1000 / uTime.Resolution)));

                    // Compile an expression to get property value
                    ParameterExpression arg = Expression.Parameter(uTime.GetType(), "Time");
                    Expression expr = Expression.Property(arg, "Time");
                    propertyTimeResolver = Expression.Lambda<Func<UniverseTimer, double>>(expr, arg).Compile();

                    // Add a block data buffer for each curve in the graphpane
                    foreach (CurveItem curve in this.ZedgraphControl1.GraphPane.CurveList)
                        blockDataBuffers.Add(new List<double>(blockDataBuffers[0].Capacity));

                    // latch time for "run duration" control
                    lastTime = uTime.Time;

                }

                // Present the simulation environment variables on change or once per second
                if (selectedModel != lastSelectedModel || GUI_Timer.Interval != lastTimerInterval || uTime.Resolution != lastResolution)
                    printDataSummary();
                if (updatecount > 1000 / GUI_Timer.Interval)
                {
                    printDataSummary();
                    updatecount = 0;
                }
                else
                    updatecount++;


                // Capture history
                lastSelectedModel = selectedModel;
                lastTimerInterval = GUI_Timer.Interval;
                lastResolution = uTime.Resolution;

                // Present the Data
                if (newData)
                {
                    if (this.ZedgraphControl1.GraphPane.Legend.IsVisible)
                    {
                        this.ZedgraphControl1.GraphPane.Legend.IsVisible = false;
                        //this.ZedgraphControl1.GraphPane.Y2Axis.Title = "";
                        //this.ZedgraphControl1.GraphPane.YAxis.Title = "";
                        //foreach (CurveItem curve in this.ZedgraphControl1.GraphPane.CurveList)
                        //    if (curve.IsY2Axis)
                        //        this.ZedgraphControl1.GraphPane.Y2Axis.Title += ((curveTag)(curve.Tag)).pinfo.Name + " ";
                        //    else
                        //        this.ZedgraphControl1.GraphPane.YAxis.Title += ((curveTag)(curve.Tag)).pinfo.Name + " ";
                    }
                        

                    // Add block data to chart series
                    //this.chart1.SuspendLayout();
                    int timeLen = blockDataBuffers[0].Count;
                    int plotLen = this.ZedgraphControl1.GraphPane.CurveList[0].Points.Count;//this.chart1.Series[0].Points.Count;//ILDataBuffers[0].Size.ToIntArray()[1];
                    int XAxisIndex = 0;
                    int SeriesIndex = 1;
                    foreach (LineItem curve in this.ZedgraphControl1.GraphPane.CurveList)
                    {
                        // loop through all samples adding to plots                        
                        int timeIndex = 0;
                        foreach (double xValue in blockDataBuffers[XAxisIndex])
                        {
                            // ZedGraph
                            this.ZedgraphControl1.GraphPane.CurveList[SeriesIndex - 1].AddPoint(xValue, blockDataBuffers[SeriesIndex][timeIndex]);
                            
                            // handle plot windowing
                            if (bApplyWindow)
                            {
                                // ZedGraph
                                while (this.ZedgraphControl1.GraphPane.CurveList[SeriesIndex - 1].NPts > SamplesPerWindow)
                                    this.ZedgraphControl1.GraphPane.CurveList[SeriesIndex - 1].Points.RemoveAt(0);

                            }
                            timeIndex++;

                        }

                        // Scale Charts
                        //totMinY = 1e6; totMaxY = -1e6;
                        //curve.Points.GetRange(ref tempxMin, ref tempxMax, ref tempyMin, ref tempYMax, false, false, false);
                        //if(totMinY > tempyMin * 1.1)
                        //    totMinY = tempyMin * 1.1;                        
                        //if (totMaxY < tempYMax* 1.1)
                        //    totMaxY = tempYMax*1.1;

                        // Clear the buffers
                        blockDataBuffers[SeriesIndex].Clear();
                        SeriesIndex++;
                    }
                    // Clear the uverse time buffer
                    blockDataBuffers[0].Clear();

                    // Scale the plots
                    this.ZedgraphControl1.GraphPane.XAxis.Min = this.ZedgraphControl1.GraphPane.CurveList[0].Points[0].X;
                    this.ZedgraphControl1.GraphPane.XAxis.Max = this.ZedgraphControl1.GraphPane.CurveList[0].Points[this.ZedgraphControl1.GraphPane.CurveList[0].Points.Count - 1].X;
                    //if (totMinY == 0.0)
                    //    this.ZedgraphControl1.GraphPane.YAxis.Min = -0.1;
                    //else
                    //    this.ZedgraphControl1.GraphPane.YAxis.Min = totMinY;
                    //if (totMaxY == 0.0)
                    //    this.ZedgraphControl1.GraphPane.YAxis.Max = 0.1;
                    //else
                    //    this.ZedgraphControl1.GraphPane.YAxis.Max = totMaxY;
                    

                    // Reset the flag
                    newData = false;


                    this.ZedgraphControl1.AxisChange();

                    // Check for Expired duration
                    if (bRunDuration)
                        if (uTime.Time >= lastTime + RunDuration)
                            stopToolStripMenuItem_Click(null, null);

                    ////ILLinePlot thisillineplot;
                    ////ILArray<float> thisillinedata;
                    //int XAxisIndex = 0;
                    //int SeriesIndex = 1;
                    //foreach (Series plotSeries in this.chart1.Series )
                    //{
                    //    // Create link to IL numerics data buffer
                    //    //thisillinedata = ILDataBuffers[SeriesIndex - 1];

                    //    // Suspend plot updates for .NET Chart
                    //    //plotSeries.Points.SuspendUpdates();

                    //    // loop through all samples adding to plots                        
                    //    int timeIndex = 0;
                    //    foreach (double xValue in blockDataBuffers[XAxisIndex])
                    //    {
                    //        // .NET Chart
                    //        plotSeries.Points.AddXY(xValue, blockDataBuffers[SeriesIndex][timeIndex]);

                    //        // ZedGraph
                    //        this.ZedgraphControl1.GraphPane.CurveList[SeriesIndex - 1].AddPoint(xValue, blockDataBuffers[SeriesIndex][timeIndex]);

                    //        //// IlNumerics (added from from end first to expedite memory allocation
                    //        //thisillinedata[0, plotLen + timeLen - 1 - timeIndex] = (float)blockDataBuffers[XAxisIndex][timeLen - 1 - timeIndex];
                    //        //thisillinedata[1, plotLen + timeLen - 1 - timeIndex] = (float)blockDataBuffers[SeriesIndex][timeLen - 1 - timeIndex];
                    //        //thisillinedata[2, plotLen + timeLen - 1 - timeIndex] = 0;

                    //        // handle plot windowing
                    //        if (bApplyWindow)
                    //        {
                    //            // .NET Chart
                    //            while (plotSeries.Points.Count > SamplesPerWindow)
                    //                plotSeries.Points.RemoveAt(0);

                    //            // ZedGraph
                    //            while (this.ZedgraphControl1.GraphPane.CurveList[SeriesIndex - 1].NPts > SamplesPerWindow)
                    //                this.ZedgraphControl1.GraphPane.CurveList[SeriesIndex - 1].Points.RemoveAt(0);

                    //        }


                    //        timeIndex++;

                    //    }

                    //    // Scale Charts
                    //    chart1.ChartAreas[0].AxisX.Minimum = chart1.Series[0].Points[0].XValue;
                    //    chart1.ChartAreas[0].AxisX.Maximum = chart1.Series[0].Points[chart1.Series[0].Points.Count-1].XValue;

                    //    // Scale Charts
                    //    this.ZedgraphControl1.GraphPane.XAxis.Min = this.ZedgraphControl1.GraphPane.CurveList[0].Points[0].X;
                    //    this.ZedgraphControl1.GraphPane.XAxis.Max = this.ZedgraphControl1.GraphPane.CurveList[0].Points[this.ZedgraphControl1.GraphPane.CurveList[0].Points.Count-1].X;

                    //    // IlNumerics
                    //    //thisillineplot = ((ILLinePlot)this.ilPanel1.Scene.First<ILPlotCube>().Children[SeriesIndex]);
                    //    //thisillineplot.Line.Positions.Update(ILDataBuffers[SeriesIndex - 1]);

                    //    // Clear the buffers
                    //    blockDataBuffers[SeriesIndex].Clear();
                    //    SeriesIndex++;

                    //    //plotSeries.Points.ResumeUpdates();
                    //}
                    //// Clear the uverse time buffer
                    //blockDataBuffers[0].Clear();

                    //// Reset the flag
                    //newData = false;

                    //// Refresh Plot Graphics
                    ////this.chart1.ChartAreas[0].AxisX.IsStartedFromZero = false;
                    ////this.chart1.ResumeLayout();

                    //this.ZedgraphControl1.AxisChange();
                    ////this.ilPanel1.Scene.First<ILPlotCube>().Reset();

                    //// Check for Expired duration
                    //if (bRunDuration)
                    //    if (uTime.Time >= lastTime + RunDuration)
                    //        stopToolStripMenuItem_Click(null, null);

                }
                // Latch GUI Time
                uTime.GUITime = DateTime.Now;

                // Model Execution Cycle (initialization/reset)
                if (selectedModel.ReInitialize && timerRunning)
                {
                    // Initialize and Trigger for Prepare in next Calculation Cycle           
                    foreach (BaseModelClass exeModel in selectedModel.MainModelExeMap)
                    {
                        exeModel.Initialize();
                        exeModel.ReInitialize = false;
                        exeModel.RePrepare = true;
                    }
                    selectedModel.ReInitialize = false;
                    selectedModel.RePrepare = true;

                }

                // Prepare is run in the calculation thread for the simple explorer

                // Model Execution Cycle (block data calculation)
                if (!this.CalculationThread.IsBusy && timerRunning)
                {
                    // call the execution thread
                    this.CalculationThread.RunWorkerAsync();
                }

            }
            #endregion
            // capture history of timerRunning variable
            timerRunningLast = timerRunning;
            this.ZedgraphControl1.Refresh();
            //this.ilPanel1.Refresh();

        }
        // this is the caculation function that is run in a separte thread asynchronously
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //DateTime currentDate = DateTime.Now;

            for (int t = 0; t < blockDataBuffers[0].Capacity; t++)
            {
                // Prepare
                if (selectedModel.RePrepare)
                {
                    foreach (BaseModelClass exeModel in selectedModel.MainModelExeMap)
                        if (exeModel.RePrepare)
                        {
                            exeModel.Prepare();
                            exeModel.RePrepare = false;
                        }
                    selectedModel.RePrepare = false;
                }

                // Execute
                foreach (BaseModelClass exeModel in selectedModel.MainModelExeMap)
                    exeModel.Execute();

                // Latch Time
                blockDataBuffers[0].Add(propertyTimeResolver(uTime));

                // Latch Properties
                int index = 1;
                foreach (CurveItem curve in this.ZedgraphControl1.GraphPane.CurveList)
                {
                    // Latched Properties
                    try { blockDataBuffers[index].Add(System.Convert.ToDouble(((curveTag)curve.Tag).pinfo.GetValue(((curveTag)curve.Tag).callingModel))); }
                    catch { blockDataBuffers[index].Add(0.0); }
                    index++;
                }
            }

            // SEt the flag
            newData = true;

            //lastdate = DateTime.Now;
            //CalcTimeSpan = TimeSpan.FromTicks(lastdate.Ticks - currentDate.Ticks);
        }
        // Action performed when start universe time menu item is clicked
        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!timerRunning && selectedModel != null && SignalSelected)
            {
                timerRunning = true;
                this.ModelMessages.NewMessage(null, uTime.Time, MessageType.Warning, "Universe Time Started");
                ResetComplete = false;
                tabControl2.SelectedIndex = 0;
            }
            else if (selectedModel == null)
                MessageBox.Show("Please Select a Model for Exploration, then Start Universe Time");
            else if (!SignalSelected)
                MessageBox.Show("Please Select at Least one Property for Plotting, then Start Universe Time");
        }
        // Action performed when stop universe time menu item is clicked
        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (timerRunning)
            {
                timerRunning = false;
                this.ModelMessages.NewMessage(null, uTime.Time, MessageType.Warning, "Universe Time Stopped");
            }
                
            bRunDuration = false;
        }
        // Action performed when reset universe time menu item is clicked
        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!timerRunning)
            {
                //// De Select all signals
                //this.treeView3.Nodes[0].Checked = false;
                //foreach (TreeNode tnode2 in this.treeView3.Nodes[0].Nodes)
                //    tnode2.Checked = false;

                // Clear Messages
                MessangerTextBox.Clear();

                // Set flag to initialize the model
                if(selectedModel!=null)
                {
                    selectedModel.ReInitialize = true;
                    if (selectedModel.Props2PlotList == null)
                        selectedModel.Props2PlotList = new List<curveTag>();
                    selectedModel.Props2PlotList.Clear();
                }
                
                // Reset the plots
                setupPlots();
                ResetComplete = true;

            }
            else
                MessageBox.Show("Please stop Universe Time before Resetting");



        }
        #endregion
        #region Callbacks of Main Form
        // performed when resolution menu items are clicked
        private void veryLowdt01sToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sender != null && ResetComplete)
            {
                string menuText = ((ToolStripMenuItem)sender).Text;
                foreach (ToolStripMenuItem mItem in this.timeResolutionToolStripMenuItem.DropDownItems)
                {
                    mItem.Checked = false;
                    if (menuText.Contains("0.1"))
                        uTime.Resolution = 0.1;
                    else if (menuText.Contains("0.01"))
                        uTime.Resolution = 0.01;
                    else if (menuText.Contains("0.001"))
                        uTime.Resolution = 0.001;
                    else if (menuText.Contains("0.0001"))
                        uTime.Resolution = 0.0001;
                    else if (menuText.Contains("0.00001"))
                        uTime.Resolution = 0.00001;

                    if (menuText == mItem.Text)
                        mItem.Checked = true;

                    SecondsPerInterval = (((float)this.GUI_Timer.Interval) / 1000);
                    SamplesPerInterval = ((int)(((float)this.GUI_Timer.Interval) / 1000 / uTime.Resolution));
                    SamplesPerSecond = SamplesPerInterval / SecondsPerInterval;
                }
            }
            else if(sender == null)
            {
                uTime.Resolution = 0.1;
                SecondsPerInterval = (((float)this.GUI_Timer.Interval) / 1000);
                SamplesPerInterval = ((int)(((float)this.GUI_Timer.Interval) / 1000 / uTime.Resolution));
                SamplesPerSecond = SamplesPerInterval / SecondsPerInterval;
            }
            else
            {
                MessageBox.Show("Please Stop and Reset Universe Time, Then Change Time Resolution:)");
            }



        }
        // Action Performed when load model menu item is clicked to Load model from library to exe system
        private void LoadModeltoSysCallback(object modelNode, System.EventArgs clickEventInfo)
        {
            // Create Instance of Selected Object
            Type myType = (Type)((ToolStripItem)modelNode).Tag;
            BaseModelClass myModel = (BaseModelClass)Activator.CreateInstance(myType);

            if (!loadedModels.Contains(loadedModels.FindLast(x => x.Name == myType.FullName)))
            {
                loadedModels.Add(myModel);
                this.LoadedModelTreeView.Nodes.Add(myType.FullName);
                this.LoadedModelTreeView.Nodes[this.LoadedModelTreeView.Nodes.Count - 1].Tag = loadedModels[loadedModels.Count - 1];
                this.LibraryNModelTabControl.SelectedIndex = 0;
                loadedModels[loadedModels.Count - 1].buildRootExeMap(this.uTime, this.ModelMessages);
                LoadedModelTreeView.SelectedNode = LoadedModelTreeView.Nodes[LoadedModelTreeView.Nodes.Count - 1];

                
            }



        }
        // Action Performed when loaded model is selected for examination
        private void LoadedModelTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!timerRunning)
            {
                // set the selected object of the property grid view
                selectedModel = (BaseModelClass)this.LoadedModelTreeView.SelectedNode.Tag;
                this.propertyGrid1.SelectedObject = selectedModel;

                this.ModelPropertiesTreeView.Nodes.Clear();
                this.ModelPropertiesTreeView.Nodes.Add(new TreeNode());
                selectedModel.ListPropertiestoTreeNode(this.ModelPropertiesTreeView.Nodes[0]);
                this.ModelPropertiesTreeView.Nodes[0].Expand();
                // reset universe and clear plots
                resetToolStripMenuItem_Click(null, null);

                tabControl2.SelectedIndex = 1;
                myVisioViewer.Visible = false;
                myVisioViewer.Unload();
                webBrowser1.Visible = true;
            }


        }
        // Action performed when mouse button pressed in property treeview
        private void ModelPropertiesTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            // if there is a node where the mouse down occured
            ModelPropertiesTreeView.SelectedNode = ((TreeView)sender).GetNodeAt(e.X, e.Y);

            // determine left click
            if (e.Button == MouseButtons.Left)
            {
                // determine if selected node is valid for plotting
                if (ModelPropertiesTreeView.SelectedNode != null)
                {
                    // get linked property info from selected node
                    curveTag senderCurveTag = (curveTag)ModelPropertiesTreeView.SelectedNode.Tag;
                    if (selectedModel.Props2PlotList == null)
                        selectedModel.Props2PlotList = new List<curveTag>();

                    // if this is valid curve tag data
                    if (senderCurveTag != null)
                    {
                        // and its not of the model type
                        if (senderCurveTag.pinfo.GetType().BaseType != typeof(BaseModelClass))
                        {
                            // setup for dragdrop
                            ((TreeView)sender).DoDragDrop(senderCurveTag, DragDropEffects.Copy);
                            plotsTabControl.SelectedIndex = 1;
                        }

                    }


                }
            }
        }
        // call back executed when dragged property enters plot area
        private void plotsTabControl_DragOver(object sender, DragEventArgs e)
        {
            if (selectedModel.Props2PlotList.Contains((curveTag)(e.Data.GetData(typeof(curveTag)))))
                e.Effect = DragDropEffects.None;
            else
            {
                e.Effect = DragDropEffects.Copy;
                plotsTabControl.SelectedIndex = 1;
            }
                
            
        }
        // callback excuted when dragged property is drop in plot area
        private void plotsTabControl_DragDrop(object sender, DragEventArgs e)
        {
            // TODO: make robust to form motion and multiple monitors and reset button
            selectedModel.Props2PlotList.Add((curveTag)(e.Data.GetData(typeof(curveTag))));
            if (e.X >= plotsTabControl.Size.Width / 2)
                selectedModel.Props2PlotList[selectedModel.Props2PlotList.Count - 1].isForY2Axis = true;
            else
                selectedModel.Props2PlotList[selectedModel.Props2PlotList.Count - 1].isForY2Axis = false;


            setupPlots();
            tabControl2.SelectedIndex = 1;
            myVisioViewer.Visible = false;
            myVisioViewer.Unload();
            webBrowser1.Visible = true;
        }
        // callback executed when the mouse is clicked in the selected properties treeview
        private void PlotPropsTreeView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                selectedModel.Props2PlotList.Remove(((curveTag)((TreeNode)((TreeView)sender).GetNodeAt(e.X, e.Y)).Tag));
                setupPlots();
            }
            if (e.Button == MouseButtons.Left)
            {
                foreach (LineItem curve in this.ZedgraphControl1.GraphPane.CurveList)
                {
                    curve.Line.Width = 1.0F;
                }
                ModelPropertiesTreeView.SelectedNode =  ModelPropertiesTreeView.GetNodeAt(e.X,e.Y);
                if( ((curveTag)((TreeNode)((TreeView)sender).GetNodeAt(e.X, e.Y)).Tag) != null)
                    ((LineItem)((curveTag)((TreeNode)((TreeView)sender).GetNodeAt(e.X, e.Y)).Tag).LinktoLineItem).Line.Width = 2.0F;
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            webpageTitle = webBrowser1.DocumentTitle;
            if (webpageTitle == "Mechatronics Engineering Through Innovative Solutions | InMechaSol")
                webBrowser1.Visible = true;
            else
                webBrowser1.Visible = false;
            webBrowser1.DocumentCompleted -= webBrowser1_DocumentCompleted;
        }

        private void plotsTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (plotsTabControl.SelectedIndex != 4 && plotsTabControl.SelectedIndex != 1)
                plotsTabControl.SelectedIndex = 4;
        }

        private void SimpleModelExplorerGUI_Load(object sender, EventArgs e)
        {
            myVisioViewer = new AxViewer();
            this.ModelDescPage.Controls.Add(myVisioViewer);
            myVisioViewer.CreateControl();
            myVisioViewer.Dock = DockStyle.Fill;
            myVisioViewer.Visible = false;
            if (File.Exists("SimpleModelExplorer.vsdx"))
                myVisioViewer.Load("SimpleModelExplorer.vsdx");
            else
                MessageBox.Show("Failed to Load Block Diagrams for Base Model Library (SimpleModelExplorer)");
        }

        // callback executed when mouse button is pressed down in model library treeview
        private void ModelLibraryTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            // determine left click
            if (e.Button == MouseButtons.Left)
            {
                ModelLibraryTreeView.SelectedNode = ((TreeView)sender).GetNodeAt(e.X, e.Y);
                // determine if selected node is valid
                if (ModelLibraryTreeView.SelectedNode != null)
                {
                    if (ModelLibraryTreeView.SelectedNode.ContextMenuStrip != null)
                    {
                        if (ModelLibraryTreeView.SelectedNode.ContextMenuStrip.Items[0].Tag != null)
                            // setup for dragdrop
                            ((TreeView)sender).DoDragDrop(ModelLibraryTreeView.SelectedNode.ContextMenuStrip.Items[0], DragDropEffects.Copy);
                    }
                    else
                    {
                        // Show clicked model
                        plotsTabControl.SelectedIndex = 4;
                        myVisioViewer.Visible = true;
                        webBrowser1.Visible = false;
                        if (myVisioViewer.DocumentLoaded)
                            myVisioViewer.Unload();
                        if (File.Exists("C:\\InMechaSol\\SimpleModelConcept\\" + ModelLibraryTreeView.SelectedNode.Name + "\\" + ModelLibraryTreeView.SelectedNode.Name + ".vsdx"))
                            myVisioViewer.Load("C:\\InMechaSol\\SimpleModelConcept\\" + ModelLibraryTreeView.SelectedNode.Name + "\\" + ModelLibraryTreeView.SelectedNode.Name + ".vsdx");

                        myVisioViewer.Invalidate();

                    }   



                }
            }
        }
        // callback executed when object is dragged over properties treeview
        private void ModelPropertiesTreeView_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(typeof(ToolStripMenuItem)) != null)
            {
                if (e.Data.GetData(typeof(ToolStripMenuItem)).GetType() == typeof(ToolStripMenuItem))
                    e.Effect = DragDropEffects.Copy;
            }

            else
                e.Effect = DragDropEffects.None;
        }
        // callback executed when dragged object is dropped over properties treeview
        private void ModelPropertiesTreeView_DragDrop(object sender, DragEventArgs e)
        {
            LoadModeltoSysCallback(ModelLibraryTreeView.SelectedNode.ContextMenuStrip.Items[0], null);
            plotsTabControl.SelectedIndex = 1;
        }
        // callback executed when a property is changed in the property browser
        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            selectedModel.RePrepare = true;
        }
        // Action performed when the menu item for model libraries is clicked
        private void DLLDialogButton_Click(object sender, EventArgs e)
        {
            // Create some variables to assist with file name portablity etc
            //string internalsavetofilename = "FullNameToModelDLL.deleteme";
            string modelFileName = "";
            OpenModelDLL_Dialog.InitialDirectory = "C:\\InMechaSol\\SimpleExplorerConcept";
            OpenModelDLL_Dialog.ShowDialog();
            modelFileName = OpenModelDLL_Dialog.FileName;

            try
            {
                modelLib = Assembly.LoadFrom(modelFileName);
                if (modelLib != null)
                    ListModels(modelLib);
                else
                    MessageBox.Show("An acceptable compiled .dll has not been selected for the model library");
            }
            catch(Exception ee) { MessageBox.Show("An acceptable compiled .dll has not been selected for the model library\n"+ee.Message+"\n"+ee.StackTrace); }
        }
        // Action performed when run continuous button clicked
        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            try
            {
                double requestedDuration = Convert.ToDouble(this.toolStripTextBox1.Text);
                if (requestedDuration != requestedDuration) // this is a technique for detecting NAN, NAN never equals anything
                {
                    RunDuration = ((double)this.GUI_Timer.Interval) / 1000;
                }
                else
                    RunDuration = requestedDuration;
            }
            catch
            {
                RunDuration = ((double)this.GUI_Timer.Interval) / 1000;
            }


            RunDuration = Math.Max(RunDuration, ((double)this.GUI_Timer.Interval) / 1000);

            this.toolStripTextBox1.Text = RunDuration.ToString();
            bRunDuration = true;
            startToolStripMenuItem_Click(null, null);
        }
        // Action performed when apply window button clicked
        private void applyPlotWindow_Click(object sender, EventArgs e)
        {
            // Parse the text box to requested window
            try
            {
                double requestedWindow = Convert.ToDouble(this.toolStripTextBox2.Text);
                if (requestedWindow != requestedWindow)
                    PlotWindow = ((double)this.GUI_Timer.Interval) / 100; // ten gui intervals
                else
                    PlotWindow = requestedWindow;
            }
            catch { PlotWindow = ((double)this.GUI_Timer.Interval) / 100; }

            // Disable if window == zero
            if (PlotWindow == 0)
                bApplyWindow = false;
            else // enable otherwise
            {
                PlotWindow = Math.Max(PlotWindow, ((double)this.GUI_Timer.Interval) / 1000);
                bApplyWindow = true;
            }
            this.toolStripTextBox2.Text = PlotWindow.ToString();
            if (bApplyWindow)
                SamplesPerWindow = SamplesPerSecond * PlotWindow;
            else
                SamplesPerWindow = 0;
        }
        // Action performed when the post processing menu item is clicked
        private void postProcessingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //// if the exe sys is not running
            //if (!timerRunning)
            //{
            //    // launch the post processing GUI...
            //    // ...If the GUI is already open, close it
            //    if (PostProcessingGUI != null)
            //    {
            //        PostProcessingGUI.Close();
            //        PostProcessingGUI.Dispose();
            //    }

            //    // Open a new GUI instance
            //    PostProcessingGUI = new DataPostProcessing();
            //    PostProcessingGUI.Visible = true;
            //}



        }
        #endregion
        #region Helper Functions
        // helper function to list all available models from a particular assembly
        private void ListModels(Assembly thisASM)
        {
            // Show the available librarys and models
            TreeNode tempNode = new TreeNode(thisASM.GetName().Name.ToString());
            tempNode.Name = tempNode.Text;
            tempNode.ToolTipText = thisASM.FullName;

            

            //foreach(Module subModule in thisASM.Modules)
            //{
            //    // Create Module Node
            //    ;
            //    // Populate Module Node
            //}
            if (!ModelLibraryTreeView.Nodes.ContainsKey(tempNode.Name))
            {
                foreach (TypeInfo model in thisASM.DefinedTypes)
                {
                    if (!model.IsAbstract && ((model.BaseType == typeof(BaseModelClass)) || (model.GetType() == typeof(BaseModelClass))))
                    {
                        tempNode.Nodes.Add(model.Name);
                        tempNode.LastNode.ToolTipText = "Right Click or Drag N Drop to Load in Explorer"; ;
                        tempNode.LastNode.ContextMenuStrip = new ContextMenuStrip();
                        tempNode.LastNode.ContextMenuStrip.Items.Add("Load Model to Execution System");
                        tempNode.LastNode.ContextMenuStrip.Items[0].Click += new System.EventHandler(LoadModeltoSysCallback);
                        tempNode.LastNode.ContextMenuStrip.Items[0].Tag = thisASM.GetType(model.FullName);
                        // Auto load to system
                        //LoadModeltoSysCallback(tempNode.LastNode.ContextMenuStrip.Items[0], null);
                    }
                }

                if (tempNode.Nodes.Count > 0)
                {
                    this.ModelLibraryTreeView.Nodes.Add(tempNode);
                    this.LibraryNModelTabControl.SelectedIndex = 1;
                    this.LibraryNModelTabControl.SelectedIndex = 1;
                    //this.ModelLibraryTreeView.ExpandAll();                    
                }
                else
                    MessageBox.Show("An acceptable compiled .dll has not been selected for the model library");

            }

        }
        // Helper function
        private void setupPlots()
        {
            int signalIndex = -1;
            Color[] plotColors = { Color.Red, Color.Blue, Color.Black, Color.Brown, Color.DarkCyan, Color.LimeGreen, Color.Purple, Color.Green, Color.HotPink, Color.ForestGreen };

            ////////////
            // Clear existing charts, series, etc            

            // .NET Chart
            //this.chart1.Series.Clear();
            //this.chart1.Legends.Clear();

            // ZedGraph
            this.ZedgraphControl1.Visible = true;

            this.ZedgraphControl1.GraphPane.CurveList.Clear();
            this.ZedgraphControl1.GraphPane.Title = "InMechaSol";


            // IlNumerics
            //while (thisPlotCube.Children.Count > 1)
            //    thisPlotCube.Children.RemoveAt(thisPlotCube.Children.Count - 1);
            //while (thisLegend.Items.Children.Count > 0)
            //    thisLegend.Items.Remove(thisLegend.Items.Children[thisLegend.Items.Children.Count-1]);
            //ILDataBuffers.Clear();

            // loop through checked properties of selectedmodel
            if(selectedModel!=null)
            {
                if (selectedModel.Props2PlotList != null)
                {

                    this.ZedgraphControl1.GraphPane.XAxis.Title = "Simulated Time (sec)";
                    this.ZedgraphControl1.GraphPane.YAxis.Title = "Y1 Axis";
                    this.ZedgraphControl1.GraphPane.YAxis.IsShowMinorGrid = false;
                    this.ZedgraphControl1.GraphPane.YAxis.IsShowGrid = false;

                    this.ZedgraphControl1.GraphPane.Legend.IsVisible = true;

                    this.ZedgraphControl1.GraphPane.Y2Axis.IsVisible = true;
                    this.ZedgraphControl1.GraphPane.Y2Axis.Title = "Y2 Axis";
                    this.ZedgraphControl1.GraphPane.Y2Axis.IsShowGrid = false;
                    this.ZedgraphControl1.GraphPane.Y2Axis.IsShowMinorGrid = false;

                    SelectedPropertiesTreeView.Nodes.Clear();
                    SelectedPropertiesTreeView.Nodes.Add("Y1 Axis");
                    SelectedPropertiesTreeView.Nodes.Add("Y2 Axis");
                    foreach (curveTag CurveInfo in selectedModel.Props2PlotList)
                    {
                        // Increment Signal Index
                        signalIndex += 1;

                        // .NET Chart
                        //chart1.Series.Add(pInfo.Name);
                        //chart1.Series[chart1.Series.Count - 1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
                        //chart1.Series[chart1.Series.Count - 1].Tag = pInfo.Tag;
                        //chart1.Series[chart1.Series.Count - 1].Color = plotColors[signalIndex % 10];
                        //this.chart1.Legends.Add(tNode.Text);

                        // ZedGraph
                        this.ZedgraphControl1.GraphPane.AddCurve(CurveInfo.pinfo.Name, new PointPairList(), plotColors[signalIndex % 10], SymbolType.None);
                        this.ZedgraphControl1.GraphPane.CurveList[this.ZedgraphControl1.GraphPane.CurveList.Count - 1].Tag = CurveInfo;
                        CurveInfo.LinktoLineItem = this.ZedgraphControl1.GraphPane.CurveList[this.ZedgraphControl1.GraphPane.CurveList.Count - 1];
                        this.ZedgraphControl1.GraphPane.CurveList[this.ZedgraphControl1.GraphPane.CurveList.Count - 1].IsY2Axis = CurveInfo.isForY2Axis;

                        // IlNumerics
                        //ILDataBuffers.Add(ILMath.zeros<float>(3, 0));
                        //thisPlotCube.Add(new ILLinePlot(ILDataBuffers[ILDataBuffers.Count - 1], lineColor: plotColors[signalIndex % 10]));
                        //thisLegend.Items.Insert(signalIndex, new ILLegendItem((ILLinePlot)thisPlotCube.Children[this.thisPlotCube.Children.Count - 1], tNode.Text));


                        if (!this.ZedgraphControl1.GraphPane.CurveList[this.ZedgraphControl1.GraphPane.CurveList.Count - 1].IsY2Axis)
                        {
                            SelectedPropertiesTreeView.Nodes[0].Nodes.Add(CurveInfo.pinfo.Name);
                            SelectedPropertiesTreeView.Nodes[0].Nodes[SelectedPropertiesTreeView.Nodes[0].Nodes.Count - 1].Tag = CurveInfo;
                            SelectedPropertiesTreeView.Nodes[0].Nodes[SelectedPropertiesTreeView.Nodes[0].Nodes.Count - 1].ToolTipText = "Right Click to Remove from Plot";
                            SelectedPropertiesTreeView.Nodes[0].Nodes[SelectedPropertiesTreeView.Nodes[0].Nodes.Count - 1].ForeColor = this.ZedgraphControl1.GraphPane.CurveList[this.ZedgraphControl1.GraphPane.CurveList.Count - 1].Color;
                        }

                            
                        else
                        {
                            SelectedPropertiesTreeView.Nodes[1].Nodes.Add(CurveInfo.pinfo.Name);
                            SelectedPropertiesTreeView.Nodes[1].Nodes[SelectedPropertiesTreeView.Nodes[1].Nodes.Count - 1].Tag = CurveInfo;
                            SelectedPropertiesTreeView.Nodes[1].Nodes[SelectedPropertiesTreeView.Nodes[1].Nodes.Count - 1].ToolTipText = "Right Click to Remove from Plot";
                            SelectedPropertiesTreeView.Nodes[1].Nodes[SelectedPropertiesTreeView.Nodes[1].Nodes.Count - 1].ForeColor = this.ZedgraphControl1.GraphPane.CurveList[this.ZedgraphControl1.GraphPane.CurveList.Count - 1].Color;
                        }
                            

                        
                        

                    }
                }
                else
                {
                    this.ZedgraphControl1.GraphPane.XAxis.Title = "It Can be Done";
                }
            }
            
            SignalSelected = signalIndex > -1;
            if (SignalSelected)
                this.ZedgraphControl1.GraphPane.IsShowTitle = false; //.Title = "Properties of " + selectedModel.Name;
            else
            {
                this.ZedgraphControl1.GraphPane.XAxis.Title = "It Can be Done";
            }
            SelectedPropertiesTreeView.ExpandAll();
        }
        // Helper function to print summary data
        private void printDataSummary()
        {
            // Update Messages
            while (this.ModelMessages.NewMessages > 0)
            {
                this.MessangerTextBox.SelectionColor = ModelMessages.RemoveMessageColor();
                this.MessangerTextBox.AppendText(ModelMessages.RemoveMessage());
                this.MessangerTextBox.ScrollToCaret();
            }


            if (selectedModel != null)
                richTextBox1.Text = "Active Model: " + selectedModel.Name;
            else
                richTextBox1.Text = "Active Model: none selected";

            this.richTextBox1.Text += " \nSeconds per GUI Interval: " + SecondsPerInterval.ToString();
            this.richTextBox1.Text += " \nSamples per GUI Interval: " + SamplesPerInterval.ToString();
            this.richTextBox1.Text += " \nSamples per Second: " + SamplesPerSecond.ToString();

            if (bApplyWindow)
                this.richTextBox1.Text += " \nSamples per Window: " + SamplesPerWindow.ToString();


            //if(CalcTimeSpan.Ticks==0)
            //    this.richTextBox1.Text += " \n\nLast Execution Time: < 100 (nsec)";
            //else
            //    this.richTextBox1.Text += " \n\nLast Execution Time: " + (CalcTimeSpan.Ticks * 100).ToString() + " (nsec)";

            if (this.ZedgraphControl1.GraphPane.CurveList.Count > 0)
            {
                this.richTextBox1.Text += " \n\nTotal Samples: " + this.ZedgraphControl1.GraphPane.CurveList[0].Points.Count.ToString();
                this.richTextBox1.Text += " \nNumber Selected Properties: " + this.ZedgraphControl1.GraphPane.CurveList.Count.ToString();
                this.richTextBox1.Text += " \nTotal MB Binary Data: " + ((this.ZedgraphControl1.GraphPane.CurveList.Count + 1) * (this.ZedgraphControl1.GraphPane.CurveList[0].Points.Count) * 8 / 1e6).ToString();
            }
            else
            {
                this.richTextBox1.Text += " \n\nTotal Samples: 0";
                this.richTextBox1.Text += " \nNumber Select Properties: 0";
                this.richTextBox1.Text += " \nTotal MB Binary Data: 0";
            }
        }
        #endregion
    }
}
