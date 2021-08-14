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
            clearBtn.Enabled = true;
            attachmentColumn.DefaultCellStyle.NullValue = null;

            NetWorkServer.RegisterOnReceiveSample(OnReceiveSample);
            NetWorkServer.RegisterOnClientConnected(OnClientConnected);
            NetWorkServer.RegisterOnClientDisconnected(OnClientDisconnected);
            NetWorkServer.BeginListen("0.0.0.0", 2333);
            timer1.Enabled = true;
            regularFont1 = new Font(tvTaskList.DefaultCellStyle.Font, FontStyle.Regular);
        }

        protected override void OnClosed(EventArgs e)
        {
            // TODO: disconnect
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
                return true;
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
        Queue<NetBase> queue = new Queue<NetBase>(32);
        Dictionary<string, TreeGridNode> nodeDict = new Dictionary<string, TreeGridNode>();

        List<Frame> frames = new List<Frame>();
        List<LuaFullMemory> luaFullMemories = new List<LuaFullMemory>();
        List<LuaRefInfo> currentPageMemoryInfos = new List<LuaRefInfo>();

        List<Sample> sampleRequest = new List<Sample>();

        private Font regularFont1;
        private int selectedFrameIndex =-1;
        private int lastPaintIndex = 0;
        Sample[] selectedSamples;
        private void OnReceiveSample(NetBase netBase)
        {
            lock (queue)
            {
                queue.Enqueue(netBase);
            }
        }
        private void OnSysInfoChange(SysInfo info) 
        {
            m_displayInfo[1] = info.state_created ? "游戏已启动" : "游戏尚未启动";
            m_displayInfo[2] = info.hooked ? "已设置钩子" : "未设置钩子";
            m_displayInfo[3] = info.memory_hooked ? "内存监控开启" : "内存监控未开启";
            m_displayInfoChange = true;
      
            captureBtn.Enabled = recordMemBox.Checked && info.state_created;
            recordMemBox.Checked = info.memory_hooked;
            if (!info.state_created) 
            {
                playBtn.Enabled = false;
                pauseBtn.Enabled = false;
                recordMemBox.Enabled = true;
                return;
            }
            recordMemBox.Enabled = false;
            
            if (info.hooked)
            {
                playBtn.Enabled = false;
                pauseBtn.Enabled = true;
                return;
            }
            playBtn.Enabled = true;
            pauseBtn.Enabled = false;
        }
        private string[] m_displayInfo = new string[4];
        private bool m_displayInfoChange = false;
        private bool m_connected = false;
        private string DisplayInfo 
        {
            get 
            {
                string output = "";
                for (int i = 0; i < m_displayInfo.Length; i++) 
                {
                    if (string.IsNullOrEmpty(m_displayInfo[i])) continue;
                    output += m_displayInfo[i] +"| ";
                }
                return output; 
            }

        }
        private void RefreshDisplayInfo() 
        {
            if (m_displayInfoChange) 
            {
                this.tips.Text = DisplayInfo;
                m_displayInfoChange = false;
            }
        }
        private void OnClientConnected(string address) 
        {
            m_displayInfo[0] = string.Format("连接成功:{0}", address); ;
            m_displayInfoChange = true;
            m_connected = true;
            MessageBox.Show(DisplayInfo);
        }

        private void OnClientDisconnected() 
        {
            m_displayInfo[0] = "断开连接";
            m_connected = false;
            this.recordMemBox.Enabled = false;
            MessageBox.Show(DisplayInfo);
        }

        private void OnSelectedFrameChanged(int index)
        {
            if (index == selectedFrameIndex) return;
            selectedFrameIndex = index;
            ClearFrameInfo();
            FillCPUFormInfo();
        }
        private int SortSample(Sample p1, Sample p2) 
        {
            if (p1.costTime > p2.costTime) return -1;
            if (p1.costTime < p2.costTime) return 1;
            return 0;
        }
        private void FillMemFormInfo(int index) 
        {
            LuaFullMemory luaFullMemory = luaFullMemories[index];
            FileStream fs = new FileStream(luaFullMemory.fullPath, FileMode.Open);
            BinaryReader br = new BinaryReader(fs);
            int count = br.ReadInt32();
            // serialize every time, because it may cost too many memory.
            currentPageMemoryInfos.Clear();
            for (int i = 0; i < count; i++)
            {
                LuaRefInfo luaRefInfo = NetUtil.DeserializeLuaRefInfo(br);
                currentPageMemoryInfos.Add(luaRefInfo);
            }
            fs.Close();

            TreeGridView gridView = GetOrCreatePage(index, System.IO.Path.GetFileName(luaFullMemory.fullPath));
            gridView.Nodes.Clear();
            for (int i = 0; i < currentPageMemoryInfos.Count && i < 100; i++)
            {
                LuaRefInfo luaRefInfo = currentPageMemoryInfos[i];
                TreeGridNode treeNode = gridView.Nodes.Add();

                treeNode.SetValues(luaRefInfo.name, luaRefInfo.addr.ToString("X"), GetMemoryString(luaRefInfo.size));
            }
            gridView.Refresh();
        }

        private void FillCPUFormInfo()
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
                    NetWorkServer.SendCmd(Cmd.seq, (int)sample.seq);
                }
                
            }

            tvTaskList.Refresh();
            this.SelectTab(0);
        }

        private void SelectTab(int index) 
        {
            this.tabControl1.SelectTab(index);
            RefreshButtonDueToTabChange(index);
        }
        private void RefreshButtonDueToTabChange(int index) 
        {
            if (index > 0)
            {
                if (markedTabIndex == index)
                {
                    this.markBtn.Text = "已标记";
                }
                else
                {
                    this.markBtn.Text = "标记";
                }
                this.markBtn.Enabled = true;
            }
            else
            {
                this.markBtn.Enabled = false;
            }
            if (index > 0 && markedTabIndex > 0 && markedTabIndex != index) 
            {
                this.memoryDiffBtn.Enabled = true;
            }
            else
            {
                this.memoryDiffBtn.Enabled = false;
            }
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
        double maxMemAlloc = 0;
        private void FillTimeline()
        {
            string name = "All";
            Series cpuSeries = this.chart1.Series[name];
            Series memSeries = this.chart2.Series[name];
            // 少画一个，保证数据已经全部填充
            for (; lastPaintIndex < frames.Count - 1; lastPaintIndex++) 
            {
                Frame frame = frames[lastPaintIndex];
                double costTime = GetFrameCostTime(frame, name);
                cpuSeries.Points.AddXY(lastPaintIndex, costTime);
                maxCostTime = costTime > maxCostTime ? costTime : maxCostTime;

                double alloc = GetMemAlloc(frame);
                memSeries.Points.AddXY(lastPaintIndex, alloc);
                maxMemAlloc = alloc > maxMemAlloc ? alloc : maxMemAlloc;
            }

        }

        private double GetMemAlloc(Frame frame) 
        {
            MList<Sample> samples = frame.GetSamples();
            int alloc = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                Sample sample = samples[i];
                alloc += sample.costLuaGC;
            }
            return alloc / (double)MaxB;
        }

        private double GetFrameCostTime(Frame frame, string name = null)
        {
            MList<Sample> samples = frame.GetSamples();
            int costTime = 0;
            for (int i = 0; i < samples.Count; i++) 
            {
                Sample sample = samples[i];
                if (name == "All" || sample.name == name)
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
            treeNode.DefaultCellStyle.Font = regularFont1;
            float totoalTime = (float)sampleNood.costTime / MaxMS;
            float intrnalCostTime = (float)sampleNood.internalCostTime / MaxMS;
            treeNode.SetValues(null, sampleNood.name, totoalTime.ToString("f3"), (totoalTime / (float)sampleNood.calls).ToString("f3"),  sampleNood.calls.ToString(), GetMemoryString(sampleNood.costLuaGC), GetMemoryString(sampleNood.costMonoGC));
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
            Thread.Sleep(100);
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
            playBtn.Enabled = false;
            pauseBtn.Enabled = false;
        }
        private void playBtn_Click(object sender, EventArgs e) 
        {
            NetWorkServer.SendCmd(Cmd.input, 1);
        }
        private void pauseBtn_Click(object sender, EventArgs e) 
        {
            NetWorkServer.SendCmd(Cmd.input, 0);
        }

        private void recordMemBox_Changed(object sender, EventArgs e) 
        {
            CheckBox box = (CheckBox)sender;
            NetWorkServer.SendCmd(Cmd.input, box.Checked ? 3 : 2);
        }

        private void captureBtn_Click(object sender, EventArgs e) 
        {
            NetWorkServer.SendCmd(Cmd.input, 4);
        }

        private int markedTabIndex = 0;
        private List<LuaRefInfo> markedLuaRefInfo = null;
        private void markBtn_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex > 0) 
            {
                markedTabIndex = tabControl1.SelectedIndex;
                markedLuaRefInfo = new List<LuaRefInfo>();
                for (int i = 0; i < currentPageMemoryInfos.Count; i++) 
                {
                    markedLuaRefInfo.Add(currentPageMemoryInfos[i]);
                }
                RefreshButtonDueToTabChange(tabControl1.SelectedIndex);
            }
        }

        private void memDiffBtn_Click(object sender, EventArgs e) 
        {
            if (markedLuaRefInfo == null) 
            {
                MessageBox.Show("尚未标记需要对比的项");
                return;
            }
            if (markedTabIndex > 0 && tabControl1.SelectedIndex > 0 && markedTabIndex != tabControl1.SelectedIndex) 
            {
                Dictionary<long, LuaRefInfo> diff = new Dictionary<long, LuaRefInfo>();
                for (int i = 0; i < currentPageMemoryInfos.Count; i++)
                {
                    LuaRefInfo luaRefInfo = currentPageMemoryInfos[i];
                    diff.Add(luaRefInfo.addr, luaRefInfo);
                }
                for (int i = 0; i < markedLuaRefInfo.Count; i++) 
                {
                    LuaRefInfo luaRefInfo = currentPageMemoryInfos[i];
                    diff.Remove(luaRefInfo.addr);
                }

                // 1. write to local file
                string fullPath = string.Format("{0}/diff-{1}-{2}_{3}.luam", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"), markedTabIndex, tabControl1.SelectedIndex);
                FileStream fs = new FileStream(fullPath, FileMode.Create);
                MBinaryWriter bw = new MBinaryWriter(fs);
                bw.Write(diff.Count);      // header <int> count;
                foreach (var item in diff)
                {
                    NetUtil.Serialize(item.Value, bw);
                }
                fs.Close();

                // 2. add new tab to display
                LuaFullMemory luaFullMemory = new LuaFullMemory(fullPath);
                AddMemoryReport(luaFullMemory);

                MessageBox.Show(string.Format("新增数据:{0}", diff.Count));

                return;
            }
            MessageBox.Show("无效的对比");
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
        private void ClearAll()
        {
            selectedFrameIndex = -1;
            markedTabIndex = 0;
            lastPaintIndex = 0;
            maxCostTime = 0;
            maxMemAlloc = 0;
            queue.Clear();
            frames.Clear();
            markedLuaRefInfo = null;
            currentPageMemoryInfos.Clear();

            chart1.Series.Clear();
            chart2.Series.Clear();

            ClearFrameInfo();
            ClearAllPages();
        }
        private void ClearAllPages() 
        {
            for (int i = 0; i < gridViews.Count; i++) 
            {
                gridViews[i].Nodes.Clear();
            }
            gridViews.Clear();
            for (int i = 0; i < tabPages.Count; i++) 
            {
                this.Controls.Remove(tabPages[i]);
            }
            this.tabPages.Clear();
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
            while (true)
            {
                NetBase package = null;
                lock (queue) 
                {
                    if (queue.Count == 0) break;
                    if (queue.Count > 0) package = queue.Dequeue();
                }
                
                if (package is Sample)
                {
                    Sample sample = package as Sample;
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
                if (package is SysInfo)
                {
                    SysInfo sysInfo = package as SysInfo;
                    OnSysInfoChange(sysInfo);
                }
                if (package is LuaFullMemory) 
                {
                    AddMemoryReport(package as LuaFullMemory);
                }
            }
            FillTimeline();
            RefreshDisplayInfo();
        }

        private void AddMemoryReport( LuaFullMemory luaFullMemory) 
        {
            luaFullMemories.Add(luaFullMemory);
            int index = luaFullMemories.Count - 1;
            FillMemFormInfo(index);
            SelectTab(index + 1);
        }

        private List<TabPage> tabPages = new List<TabPage>();
        private List<TreeGridView> gridViews = new List<TreeGridView>();
        private TreeGridView GetOrCreatePage(int index,string name = null) 
        {
            if(index >=0 && index < gridViews.Count)
            {
                return gridViews[index];
            }
            return AddPage(name);
        }
        private TreeGridView AddPage(string name = null) 
        {
            System.Windows.Forms.TabPage tabPage = new TabPage();
            tabPage.Location = new System.Drawing.Point(1, 22);
            tabPage.Name = string.Format("tabPage{0}", tabPages.Count);
            tabPage.Padding = new System.Windows.Forms.Padding(0);
            tabPage.TabIndex = tabPages.Count + 1;
            tabPage.Text = string.IsNullOrEmpty(name) ? string.Format("memory{0}", tabPages.Count) : name;
            tabPage.UseVisualStyleBackColor = true;

            TreeGridColumn memoryview = new TreeGridColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn addr = new System.Windows.Forms.DataGridViewTextBoxColumn();
            System.Windows.Forms.DataGridViewTextBoxColumn memoryAlloc = new System.Windows.Forms.DataGridViewTextBoxColumn();

            memoryview.DefaultNodeImage = null;
            memoryview.FillWeight = 450F;
            memoryview.HeaderText = "OverView";
            memoryview.MaxInputLength = 3000;
            memoryview.Name = "overview";
            memoryview.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            memoryview.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            addr.HeaderText = "address";
            addr.Name = "address";
            addr.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            memoryAlloc.HeaderText = "Alloc";
            memoryAlloc.Name = "memoryAlloc";
            memoryAlloc.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;


            TreeGridView gridView = new AdvancedDataGridView.TreeGridView();
            gridView.AllowUserToAddRows = false;
            gridView.AllowUserToDeleteRows = false;
            gridView.AllowUserToOrderColumns = true;
            gridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            gridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            gridView.BackgroundColor = System.Drawing.Color.White;
            gridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            gridView.ColumnHeadersHeight = 20;
            gridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            memoryview,
            addr,
            memoryAlloc});
            gridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            gridView.ImageList = null;
            gridView.Location = new System.Drawing.Point(0, 0);
            gridView.Name = "memoryList";
            gridView.RowHeadersVisible = false;
            gridView.RowHeadersWidth = 20;

            gridView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            gridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            gridView.Size = new System.Drawing.Size(200, 410);
            gridView.TabIndex = 0;

            tabPage.Controls.Add(gridView);
            tabPages.Add(tabPage);
            gridViews.Add(gridView);
            this.tabControl1.Controls.Add(tabPage);
            return gridView;
        }

        private int pointIndexOnMouseOver = 0;
        private void Chart1_GetToolTipText(object sender, ToolTipEventArgs e)
        {
            Chart chart = sender as Chart;
            pointIndexOnMouseOver = e.HitTestResult.PointIndex;
            if (e.HitTestResult.ChartElementType == ChartElementType.DataPoint)
            {
                DataPoint dp = e.HitTestResult.Series.Points[pointIndexOnMouseOver];
                if (chart.Name == "cpuChart")
                {
                    e.Text = string.Format("Frame: {0}\nDuration: {1:F3}ms", dp.XValue, dp.YValues[0]);
                }
                else
                {
                    e.Text = string.Format("Frame: {0}\nAlloc: {1:F3}", dp.XValue, GetMemoryString((long)dp.YValues[0] * MaxB));
                }
                
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
            double max = chart.Name == "cpuChart" ? maxCostTime : maxMemAlloc;
            max += 1;

            {
                yAxis.Maximum = Math.Min(Math.Max(e.Delta > 0 ? yAxis.Maximum -= 0.25 : yAxis.Maximum += 0.25, 1), Math.Ceiling(max < 1 ? 1 : max));
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
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e) 
        {
            TabControl tabControl = sender as TabControl;
            Console.WriteLine("select:" + tabControl.SelectedIndex);
            if (tabControl.SelectedIndex > 0)  // skip the first one
            {
                FillMemFormInfo(tabControl.SelectedIndex - 1);
            }
            RefreshButtonDueToTabChange(tabControl.SelectedIndex);
        }
    }
}