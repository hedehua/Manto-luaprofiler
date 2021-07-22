using AdvancedDataGridView;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using EasyHook;
using System.Runtime.Remoting;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows.Forms.DataVisualization.Charting;

namespace SparrowLuaProfiler
{
    public partial class ProfilerForm : Form
    {
        public ProfilerForm()
        {
            InitializeComponent();
            button1.Enabled = true;
            attachmentColumn.DefaultCellStyle.NullValue = null;

            NetWorkServer.RegisterOnReceiveSample(OnReceiveSample);
            NetWorkServer.RegisterOnClientConnected(OnClientConnected);
            NetWorkServer.RegisterOnClientDisconnected(OnClientDisconnected);
            NetWorkServer.BeginListen("0.0.0.0", 2333);
            timer1.Enabled = true;
            boldFont = new Font(tvTaskList.DefaultCellStyle.Font, FontStyle.Bold);
        }

        protected override void OnClosed(EventArgs e)
        {
            Thread.Sleep(100);
            base.OnClosed(e);
            System.Environment.Exit(0);
        }

        class Frame
        {
            private Dictionary<string, Sample> dict;
            private MList<Sample> samples;
            public bool AddSample(Sample sample)
            {
                if (dict == null) dict = new Dictionary<string, Sample>();
                if (samples == null) samples = new MList<Sample>(16);
                if (dict.ContainsKey(sample.name))
                {
                    return false;
                }
                long currentTime = GetCurrentTime();
                if (sample.currentTime < currentTime) 
                {
                    return false;
                }
                if (currentTime > 0 && sample.currentTime - currentTime > 300000)   // 30ms
                {
                    return false;
                }

                dict.Add(sample.name, sample);
                samples.Add(sample);
                return true ;
            }
            public void Clear()
            {
                if (dict != null) dict.Clear();
                if (samples != null) samples.Clear();
            }
            public long GetCurrentTime() 
            {
                if (samples.Count == 0) return 0;
                return samples[0].currentTime;
            }
            public MList<Sample> GetSamples() { return samples; }
        }

        #region refresh
        Queue<Sample> queue = new Queue<Sample>(32);
        Dictionary<string, TreeGridNode> nodeDict = new Dictionary<string, TreeGridNode>();
        List<Frame> frames = new List<Frame>();
        Dictionary<string, Series> timelineDic = new Dictionary<string, Series>();
        List<Sample> sampleRequest = new List<Sample>();

        private Font boldFont;
        private int selectedFrameIndex =-1;
        private int lastPaintIndex = 0;
        Sample[] selectedSamples;
        private void OnReceiveSample(Sample sample)
        {
            lock (queue)
            {
                queue.Enqueue(sample);
            }
            // Console.WriteLine("receive sample :"+ sample.name);
        }
        private void OnClientConnected() 
        {
            MessageBox.Show("连接成功");
        }

        private void OnClientDisconnected() 
        {
            MessageBox.Show("断开连接");
        }

        private void OnSelectedFrameChanged(int index)
        {
            if (index == selectedFrameIndex) return;
            selectedFrameIndex = index;
            ClearFrameInfo();
            FillFormInfo();
        }
        private int SortSample(Sample p1, Sample p2) 
        {
            if (p1.costTime > p2.costTime) return -1;
            if (p1.costTime < p2.costTime) return 1;
            return 0;
        }

        private void FillFormInfo()
        {
            if (selectedFrameIndex < 0 || selectedFrameIndex >= frames.Count) return;

            Frame frame = frames[selectedFrameIndex];
            selectedSamples = frame.GetSamples().ToArray();
            if (selectedSamples == null) return;
            Array.Sort(selectedSamples, SortSample);
            for (int i = 0; i < selectedSamples.Length; i++) 
            {
                Sample sample = selectedSamples[i];
                TreeGridNode treeNode;
                if (!nodeDict.TryGetValue(sample.fullName, out treeNode))
                {
                    treeNode = tvTaskList.Nodes.Add();
                    nodeDict.Add(sample.fullName, treeNode);
                }
                DoFillChildFormInfo(sample, treeNode);
                if (!sample.childrenFilled) 
                {
                    Console.WriteLine(string.Format("send cmd [{0}]{1} {2}", i, sample.seq, sample.name));
                    sampleRequest.Add(sample);
                    NetWorkServer.SendCmd((int)sample.seq);
                }
                
            }
            tvTaskList.Refresh();
        }

        private void RefreshFormInfo(Sample sample) 
        {
            TreeGridNode treeNode;
            if (nodeDict.TryGetValue(sample.fullName, out treeNode))
            {
                treeNode.Nodes.Clear();
                DoFillChildFormInfo(sample, treeNode);
                tvTaskList.Refresh();
            }
        }

        const int MaxMS = 10000;
        double maxCostTime = 0;
        private void FillTimeline()
        {
            GetOrCreateTimelineNode("GT");
            // 少画一个，保证数据已经全部填充
            for (; lastPaintIndex < frames.Count - 1; lastPaintIndex++) 
            {
                foreach (string name in timelineDic.Keys)
                {
                    Series series = GetOrCreateTimelineNode(name);
                    Frame frame = frames[lastPaintIndex];
                    double costTime = GetFrameCostTime(frame, name);
                    series.Points.AddXY(lastPaintIndex, costTime);
                    maxCostTime = costTime > maxCostTime ? costTime : maxCostTime;
                }
            }

        }
        private Series GetOrCreateTimelineNode(string name)
        {
            Series series1;
            if (timelineDic.TryGetValue(name,out series1)) 
            {
                return series1;
            }
            System.Random random = new System.Random();
            Series series = this.chart1.Series.Add(name);
            series.ChartType = SeriesChartType.Column;
            series.IsValueShownAsLabel = true;
            series.MarkerStyle = MarkerStyle.None;
            series.Color = System.Drawing.Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
            series.BorderWidth = 1;
            series.IsXValueIndexed = true;
            series.IsValueShownAsLabel = false;
         
            timelineDic.Add(name, series);
            return series;
        }

        private double GetFrameCostTime(Frame frame, string name = null)
        {
            MList<Sample> samples = frame.GetSamples();
            int costTime = 0;
            for (int i = 0; i < samples.Count; i++) 
            {
                Sample sample = samples[i];
                if (name == "GT" || sample.name == name)
                {
                    costTime += sample.costTime;
                }
            }
            return costTime/ (double)MaxMS;
        }

        const long MaxB = 1024;
        const long MaxK = MaxB * 1024;
        const long MaxM = MaxK * 1024;
        const long MaxG = MaxM * 1024;

        public static string GetMemoryString(long value, string unit = "B")
        {
            string result = null;
            int sign = Math.Sign(value);

            value = Math.Abs(value);
            if (value < MaxB)
            {
                result = string.Format("{0}{1}", value, unit);
            }
            else if (value < MaxK)
            {
                result = string.Format("{0:N2}K{1}", (float)value / MaxB, unit);
            }
            else if (value < MaxM)
            {
                result = string.Format("{0:N2}M{1}", (float)value / MaxK, unit);
            }
            else if (value < MaxG)
            {
                result = string.Format("{0:N2}G{1}", (float)value / MaxM, unit);
            }
            if (sign < 0)
            {
                result = "-" + result;
            }
            return result;
        }

        private void DoFillChildFormInfo(Sample sampleNood, TreeGridNode treeNode)
        {
            treeNode.DefaultCellStyle.Font = boldFont;
            float totoalTime = (float)sampleNood.costTime / MaxMS;
            float intrnalCostTime = (float)sampleNood.internalCostTime / MaxMS;
            treeNode.SetValues(null, sampleNood.name, totoalTime.ToString("f3"), (totoalTime / (float)sampleNood.calls).ToString("f3"),  sampleNood.calls.ToString(), GetMemoryString(sampleNood.costLuaGC), GetMemoryString(sampleNood.costMonoGC));
            sampleNood.luaGC = 0;
            Sample[] samples = sampleNood.childs.ToArray();
            Array.Sort(samples,SortSample);
            for (int i = 0, imax = samples.Length; i < imax; i++)
            {
                var item = samples[i];
                TreeGridNode node;
                if (!nodeDict.TryGetValue(item.fullName, out node))
                {
                    node = treeNode.Nodes.Add();
                    nodeDict.Add(item.fullName, node);
                }
                DoFillChildFormInfo(item, node);
            }
        }
        #endregion

        #region click
        public void OnProcessTextChange(object sender, EventArgs e)
        {
            string origin = processCom.Text;
            string text = processCom.Text.ToLower();
            processCom.Items.Clear();
            var pArray = Process.GetProcesses();
            for (int i = 0, imax = pArray.Length; i < imax; i++)
            {
                if (pArray[i].ProcessName.ToLower().Contains(text))
                {
                    processCom.Items.Add(pArray[i].ProcessName);
                }
            }
            processCom.DroppedDown = true;
            processCom.Text = origin;
            processCom.SelectionStart = processCom.Text.Length;
            //processCom.Show();
        }

        private void injectButton_Click(object sender, EventArgs e)
        {
            Process[] process = Process.GetProcessesByName(processCom.Text);
            if (process.Length > 0)
            {
                var p = Process.GetProcessById(process.FirstOrDefault().Id);
                if (p == null)
                {
                    MessageBox.Show("指定的进程不存在!");
                    return;
                }

                if (IsWin64Emulator(p.Id) != IsWin64Emulator(Process.GetCurrentProcess().Id))
                {
                    var currentPlat = IsWin64Emulator(Process.GetCurrentProcess().Id) ? 64 : 32;
                    var targetPlat = IsWin64Emulator(p.Id) ? 64 : 32;
                    MessageBox.Show(string.Format("当前程序是{0}位程序，目标进程是{1}位程序，请调整编译选项重新编译后重试！", currentPlat, targetPlat));
                    return;
                }

                if (!RegGACAssembly())
                {
                    return;
                }
                InstallHookInternal(p.Id);
            }
            else
            {
                MessageBox.Show("该进程不存在！");
            }
        }

        private void deattachBtn_Click(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            NetWorkServer.CloseClient();
            Thread.Sleep(100);
            NativeAPI.GacUninstallAssemblies
            (
                new string[] { "HookLib.dll" }
                , "A simple ProcessMonitor based on EasyHook!",
                base64Str
            );
            
            Thread.Sleep(100);

            MessageBox.Show("已解除");
            injectButton.Enabled = true;
            deattachBtn.Enabled = false;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
        private void ClearAll()
        {
            selectedFrameIndex = -1;
            lastPaintIndex = 0;
            maxCostTime = 0;
            queue.Clear();
            frames.Clear();
            timelineDic.Clear();
            chart1.Series.Clear();

            ClearFrameInfo();
        }
        private void ClearFrameInfo() 
        {
            selectedSamples = null;
            nodeDict.Clear();
            tvTaskList.Nodes.Clear();
        }
        #endregion

        #region GAC
        private static string dictPath = AppDomain.CurrentDomain.BaseDirectory;
        private string base64Str = GenBase64Str();

        public static string GenBase64Str()
        {
            Byte[] IdentData = new Byte[30];
            new RNGCryptoServiceProvider().GetBytes(IdentData);
            return Convert.ToBase64String(IdentData);
        }

        private bool RegGACAssembly()
        {
            if (!NativeAPI.RhIsAdministrator())
            {
                return false;
            }

            try
            {
                try
                {
                    NativeAPI.GacUninstallAssemblies
                    (
                        new string[] { "HookLib.dll" }
                        , "A simple ProcessMonitor based on EasyHook!",
                        base64Str
                    );
                    Thread.Sleep(100);
                }
                catch { }

                NativeAPI.GacInstallAssemblies
                (
                    new string[] { Path.Combine(dictPath, "EasyHook.dll"), Path.Combine(dictPath, "HookLib.dll") }
                    , "A simple ProcessMonitor based on EasyHook!",
                    base64Str
                );
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return false;
            }

            return true;
        }

        private bool InstallHookInternal(int processId)
        {
            try
            {
                var parameter = new HookParameter
                {
                    Msg = "已经成功注入目标进程",
                    HostProcessId = RemoteHooking.GetCurrentProcessId()
                };
                RemoteHooking.Inject(
                    processId,
                    InjectionOptions.Default,
                    typeof(HookParameter).Assembly.Location,
                    typeof(HookParameter).Assembly.Location,
                    string.Empty,
                    parameter
                );
            }
            catch (Exception ex)
            {
                Debug.Print(ex.ToString());
                return false;
            }
            injectButton.Enabled = false;
            deattachBtn.Enabled = true;
            return true;
        }

        private static bool IsWin64Emulator(int processId)
        {
            var process = Process.GetProcessById(processId);
            if (process == null)
                return false;

            if ((Environment.OSVersion.Version.Major > 5)
                || ((Environment.OSVersion.Version.Major == 5) && (Environment.OSVersion.Version.Minor >= 1)))
            {
                bool retVal;

                return !(IsWow64Process(process.Handle, out retVal) && retVal);
            }

            return false; // not on 64-bit Windows Emulator
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        #endregion

        private void Timer1_Tick(object sender, EventArgs e)
        {

            lock (queue)
            {
                while (queue.Count > 0)
                {
                    Sample sample = queue.Dequeue();

                    // 先在请求列表中查找
                    for (int i = sampleRequest.Count - 1; i >= 0; i--)
                    {
                        if (sampleRequest[i].seq == sample.seq)
                        {
                            sampleRequest[i].childs = sample.childs;
                            sample = sampleRequest[i];
                            sample.childrenFilled = true;
                            sampleRequest.RemoveAt(i);
                        }
                    }
                    if (sample.childrenFilled) 
                    {
                        Console.WriteLine(string.Format("recv {0} {1} child:{2}", sample.seq, sample.name, sample.childs.Count));
                        RefreshFormInfo(sample);
                        continue;
                    }
                 
                    Frame frame = null;
                    // 尝试加载最后一个帧
                    if (frames.Count > 0) 
                    {
                        frame = frames[frames.Count - 1];
                        if (frame.AddSample(sample)) continue;
                    }

                    frame = new Frame();
                    frames.Add(frame);
                    frame.AddSample(sample);

                }
                FillTimeline();
            }
        }
        private int pointIndexOnMouseOver = 0;
        private void Chart1_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            pointIndexOnMouseOver = e.HitTestResult.PointIndex;
            if (e.HitTestResult.ChartElementType == ChartElementType.DataPoint)
            {
                DataPoint dp = e.HitTestResult.Series.Points[pointIndexOnMouseOver];
                e.Text = string.Format("Frame: {0}\nDuration: {1:F3}ms", dp.XValue, dp.YValues[0]);
            }
        }

        private void Chart1_MouseClick(object sender, MouseEventArgs e) 
        {

            if (pointIndexOnMouseOver >= 0) 
            {
                OnSelectedFrameChanged(pointIndexOnMouseOver);
                return;
            }
            
            Chart chart = (Chart)sender;
            HitTestResult test = chart.HitTest(e.X, e.Y);
            if (test.ChartElementType == ChartElementType.PlottingArea || test.ChartElementType == ChartElementType.Gridlines)
            {
                HitTestResult[] results = chart.HitTest(e.X, chart.Size.Height - 18, true, ChartElementType.DataPoint);
                foreach (HitTestResult result in results)
                {
                    if (result.ChartElementType == ChartElementType.DataPoint && result.PointIndex >= 0)
                    {
                        int frameIndex = result.PointIndex;
                        OnSelectedFrameChanged(frameIndex);
                    }
                }
            }
        }

        private void Chart1_MouseWheel(object sender, MouseEventArgs e)
        {
            Chart chart = (Chart)sender;
            Axis xAxis = chart.ChartAreas[0].AxisX;
            Axis yAxis = chart.ChartAreas[0].AxisY;

            {
                yAxis.Maximum = Math.Min(Math.Max(e.Delta > 0 ? yAxis.Maximum -= 0.25 : yAxis.Maximum += 0.25, 1), Math.Ceiling(maxCostTime < 1 ? 1 : maxCostTime));
            }
      
            {
                double xMin = xAxis.ScaleView.ViewMinimum;
                double xMax = xAxis.ScaleView.ViewMaximum;
                double currentSize = xMax - xMin;
                double mousePoint = xAxis.PixelPositionToValue(e.X);
                double xratio = (mousePoint - xMin) / currentSize;
                double newSize = e.Delta > 0 ? currentSize / 1.2f : currentSize * 1.2f ;

                double posXStart = Math.Max(mousePoint - newSize * xratio, 0);
                double posXEnd = mousePoint + newSize * (1 - xratio);
                xAxis.ScaleView.Zoom(posXStart, posXEnd);
            }
        }
        private void GridView_RowEnter(object sender, DataGridViewCellEventArgs e) 
        {
            Console.WriteLine("Row Enter "+e.RowIndex);
        }
    }
}