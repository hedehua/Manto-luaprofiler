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
            NetWorkServer.BeginListen("0.0.0.0", 2333);
            timer1.Enabled = true;
            boldFont = new Font(tvTaskList.DefaultCellStyle.Font, FontStyle.Bold);
        }

        protected override void OnClosed(EventArgs e)
        {
            Thread.Sleep(1000);
            base.OnClosed(e);
        }

        class Frame
        {
            private Dictionary<string, Sample> dict;
            private List<Sample> samples;
            public Sample AddSample(Sample sample)
            {
                if (dict == null) dict = new Dictionary<string, Sample>();
                if (samples == null) samples = new List<Sample>();
                Sample s;
                if (dict.TryGetValue(sample.name, out s))
                {
                    s.AddSample(sample);
                    return s;
                }

                dict.Add(sample.name, sample);
                samples.Add(sample);
                return sample;
            }
            public void Clear()
            {
                if (dict != null) dict.Clear();
                if (samples != null) samples.Clear();
            }
            public List<Sample> GetSamples() { return samples; }
        }

        #region refresh
        Queue<Sample> queue = new Queue<Sample>(32);
        Dictionary<string, TreeGridNode> nodeDict = new Dictionary<string, TreeGridNode>();
        List<Frame> frames = new List<Frame>();
        Dictionary<string, Series> timelineDic = new Dictionary<string, Series>();

        private Font boldFont;
        private int selectedFrame;
        private int lastPaintIndex = 0;
        private void OnReceiveSample(Sample sample)
        {
            lock (queue)
            {
                queue.Enqueue(sample);
            }
        }
        private void OnClientConnected() 
        {
            MessageBox.Show("连接成功");
        }

        private void OnSelectedFrameChanged(int index)
        {
            if (index == selectedFrame) return;
            selectedFrame = index;
            ClearFrameInfo();
            FillFormInfo();
        }

        private void FillFormInfo()
        {
            if (selectedFrame < 0 || selectedFrame >= frames.Count) return;

            Frame frame = frames[selectedFrame];
            List<Sample> samples = frame.GetSamples();
            if (samples == null) return;
            foreach (var item in samples)
            {
                TreeGridNode treeNode;
                if (!nodeDict.TryGetValue(item.fullName, out treeNode))
                {
                    treeNode = tvTaskList.Nodes.Add();
                    nodeDict.Add(item.fullName, treeNode);
                }
                DoFillChildFormInfo(item, treeNode);
            }
            tvTaskList.Refresh();
        }

        const float MaxMS = 1000.0f;
        private void FillTimeline()
        {
            Series GT = GetOrAddSeries("GT");
            for (; lastPaintIndex < frames.Count; lastPaintIndex++)
            {
                foreach (string name in timelineDic.Keys)
                {
                    Series series = GetOrAddSeries(name);
                    Frame frame = frames[lastPaintIndex];
                    double costTime = GetFunctionCost(frame, name);
                    series.Points.AddXY(lastPaintIndex, costTime / MaxMS);
                }
            }

        }
        private Series GetOrAddSeries(string name)
        {
            if (timelineDic.ContainsKey(name))
            {
                return timelineDic[name];
            }
            System.Random random = new System.Random();
            Series series = this.chart1.Series.Add(name);
            series.ChartType = SeriesChartType.Area;
            series.IsValueShownAsLabel = true;
            series.MarkerStyle = MarkerStyle.Square;
            series.Color = System.Drawing.Color.FromArgb(random.Next(255), random.Next(255), random.Next(255));
            series.BorderWidth = 1;
            series.IsXValueIndexed = true;
            timelineDic.Add(name, series);
            return series;
        }

        private double GetFunctionCost(Frame frame, string name = null)
        {
            List<Sample> samples = frame.GetSamples();
            double costTime = 0.0f;
            foreach (Sample sample in samples)
            {
                if (name == "GT" || sample.name == name)
                {
                    costTime += sample.costTime;
                }
            }
            return costTime;
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
            float totoalTime = (float)sampleNood.currentTime / 10000;
            treeNode.SetValues(null, sampleNood.name, GetMemoryString(sampleNood.costLuaGC), GetMemoryString(sampleNood.selfLuaGC), GetMemoryString(sampleNood.luaGC), (totoalTime / (float)sampleNood.calls).ToString("f2") + "ms", totoalTime.ToString("f2") + "ms", GetMemoryString(sampleNood.calls, ""));
            sampleNood.luaGC = 0;
            for (int i = 0, imax = sampleNood.childs.Count; i < imax; i++)
            {
                var item = sampleNood.childs[i];
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
            queue.Clear();
            frames.Clear();
            timelineDic.Clear();
            chart1.Series.Clear();

            ClearFrameInfo();
        }
        private void ClearFrameInfo() 
        {
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
                    int frameCount = sample.frameCount;
                    Frame frame = null;
                    if (frameCount >= frames.Count)
                    {
                        frame = new Frame();
                        frames.Add(frame);
                    }
                    else
                    {
                        frame = frames[frameCount];
                    }
                    frame.AddSample(sample);

                }
                FillTimeline();
            }
        }

        private void Chart1_MouseClick(object sender, MouseEventArgs e) 
        {
            Chart chart = (Chart)sender;
            HitTestResult result = chart.HitTest(e.X, e.Y);
            if (result.PointIndex >= 0) 
            {
                int frameIndex = result.PointIndex;
                OnSelectedFrameChanged(frameIndex);
            }
        }

        private void Chart1_MouseWheel(object sender, MouseEventArgs e)
        {
            Chart chart = (Chart)sender;
            Axis xAxis = chart.ChartAreas[0].AxisX;

            {
                double xMin = xAxis.ScaleView.ViewMinimum;
                double xMax = xAxis.ScaleView.ViewMaximum;

                if (e.Delta < 0)
                {
                    xAxis.ScaleView.ZoomReset();
                }
                else if (e.Delta > 0)
                {

                    double posXStart = xAxis.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 2;
                    double posXFinish = xAxis.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 2;

                    xAxis.ScaleView.Zoom(posXStart, posXFinish);

                }

            }
        }
    }
}