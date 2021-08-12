using AdvancedDataGridView;

namespace SparrowLuaProfiler
{
    partial class ProfilerForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfilerForm));
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chart2 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tvTaskList = new AdvancedDataGridView.TreeGridView();
            this.memoryList = new AdvancedDataGridView.TreeGridView();
            this.imageStrip = new System.Windows.Forms.ImageList(this.components);
            this.injectButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tips = new System.Windows.Forms.Label();
            this.processCom = new System.Windows.Forms.ComboBox();
            this.deattachBtn = new System.Windows.Forms.Button();
            this.playBtn = new System.Windows.Forms.Button();
            this.pauseBtn = new System.Windows.Forms.Button();
            this.clearBtn = new System.Windows.Forms.Button();
            this.captureBtn = new System.Windows.Forms.Button();
            this.markBtn = new System.Windows.Forms.Button();
            this.memoryDiffBtn = new System.Windows.Forms.Button();
            //this.searchBox = new System.Windows.Forms.TextBox();
            //this.searchBtn = new System.Windows.Forms.Button();
     
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.attachmentColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.overview = new AdvancedDataGridView.TreeGridColumn();
            this.luaGC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.monoGC = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.averageTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.totalTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.totalCalls = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.memoryview = new TreeGridColumn();
            this.memoryAlloc = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();

            ((System.ComponentModel.ISupportInitialize)(this.tvTaskList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoryList)).BeginInit();
            this.SuspendLayout();
            // 
            // injectButton
            // 
            this.injectButton.Location = new System.Drawing.Point(186, 3);
            this.injectButton.Name = "injectButton";
            this.injectButton.Size = new System.Drawing.Size(84, 23);
            this.injectButton.TabIndex = 1;
            this.injectButton.Text = "连接";
            this.injectButton.UseVisualStyleBackColor = true;
            this.injectButton.Click += new System.EventHandler(this.injectButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 12);
            this.label1.TabIndex = 3;
            this.label1.Text = "进程名";

            // 
            // processCom
            // 
            this.processCom.FormattingEnabled = true;
            this.processCom.Location = new System.Drawing.Point(59, 6);
            this.processCom.Name = "processCom";
            this.processCom.Size = new System.Drawing.Size(121, 20);
            this.processCom.TabIndex = 4;
            this.processCom.TextUpdate += new System.EventHandler(this.OnProcessTextChange);
            // 
            // deattachBtn
            // 
            this.deattachBtn.Enabled = false;
            this.deattachBtn.Location = new System.Drawing.Point(279, 3);
            this.deattachBtn.Name = "deattachBtn";
            this.deattachBtn.Size = new System.Drawing.Size(84, 23);
            this.deattachBtn.TabIndex = 5;
            this.deattachBtn.Text = "断开";
            this.deattachBtn.UseVisualStyleBackColor = true;
            this.deattachBtn.Click += new System.EventHandler(this.deattachBtn_Click);

            ///
            // playBtn
            ///
            this.playBtn.Enabled = false;
            this.playBtn.Location = new System.Drawing.Point(389, 3);
            this.playBtn.Name = "playBtn";
            this.playBtn.Size = new System.Drawing.Size(84, 23);
            this.playBtn.TabIndex = 5;
            this.playBtn.Text = "开始";
            this.playBtn.UseVisualStyleBackColor = true;
            this.playBtn.Click += new System.EventHandler(this.playBtn_Click);

            ///
            // pauseBtn
            ///
            this.pauseBtn.Enabled = false;
            this.pauseBtn.Location = new System.Drawing.Point(479, 3);
            this.pauseBtn.Name = "playBtn";
            this.pauseBtn.Size = new System.Drawing.Size(84, 23);
            this.pauseBtn.TabIndex = 5;
            this.pauseBtn.Text = "暂停";
            this.pauseBtn.UseVisualStyleBackColor = true;
            this.pauseBtn.Click += new System.EventHandler(this.pauseBtn_Click);

            ///
            // captureBt
            ///
            this.captureBtn.Enabled = true;
            this.captureBtn.Location = new System.Drawing.Point(589, 3);
            this.captureBtn.Name = "captureBtn";
            this.captureBtn.Size = new System.Drawing.Size(84, 23);
            //this.captureBtn.TabIndex = 5;
            this.captureBtn.Text = "内存快照";
            this.captureBtn.UseVisualStyleBackColor = true;
            this.captureBtn.Click += new System.EventHandler(this.captureBtn_Click);

            ///
            // markBtn
            ///
            this.markBtn.Enabled = true;
            this.markBtn.Location = new System.Drawing.Point(1075, 3);
            this.markBtn.Name = "markBtn";
            this.markBtn.Size = new System.Drawing.Size(84, 23);
            this.markBtn.Text = "标记";
            this.markBtn.UseVisualStyleBackColor = true;
            this.markBtn.Click += new System.EventHandler(this.markBtn_Click);

            ///
            // memDiffBtn
            ///
            this.memoryDiffBtn.Enabled = true;
            this.memoryDiffBtn.Location = new System.Drawing.Point(1165, 3);
            this.memoryDiffBtn.Name = "memoryDiffBtn";
            this.memoryDiffBtn.Size = new System.Drawing.Size(84, 23);
            this.memoryDiffBtn.Text = "内存对比";
            this.memoryDiffBtn.UseVisualStyleBackColor = true;
            this.memoryDiffBtn.Click += new System.EventHandler(this.memDiffBtn_Click);

            // 
            // searchBox
            // 
            //this.searchBox.Location = new System.Drawing.Point(1165, 5);
            //this.searchBox.Name = "searchBox";
            //this.searchBox.Size = new System.Drawing.Size(136, 21);
            //this.searchBox.TabIndex = 6;
            //// 
            //// searchBtn
            //// 
            //this.searchBtn.Location = new System.Drawing.Point(1320, 3);
            //this.searchBtn.Name = "searchBtn";
            //this.searchBtn.Size = new System.Drawing.Size(84, 23);
            //this.searchBtn.TabIndex = 7;
            //this.searchBtn.Text = "搜索";
            //this.searchBtn.UseVisualStyleBackColor = true;

            // 
            // clearBtn
            // 
            this.clearBtn.Enabled = false;
            this.clearBtn.Location = new System.Drawing.Point(1300, 3);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(84, 23);
            this.clearBtn.TabIndex = 8;
            this.clearBtn.Text = "清空";
            this.clearBtn.UseVisualStyleBackColor = true;
            this.clearBtn.Click += new System.EventHandler(this.clearBtn_Click);

            this.tips.AutoSize = true;
            this.tips.Location = new System.Drawing.Point(1, 728);
            this.tips.Name = "tips";
            this.tips.Size = new System.Drawing.Size(300, 12);
            this.tips.ForeColor = System.Drawing.Color.Gray;
            this.tips.TabIndex = 3;
            this.tips.Text = "";

            // 
            // attachmentColumn
            // 
            this.attachmentColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            this.attachmentColumn.DefaultCellStyle = dataGridViewCellStyle1;
            this.attachmentColumn.FillWeight = 51.53443F;
            this.attachmentColumn.HeaderText = "";
            this.attachmentColumn.MinimumWidth = 25;
            this.attachmentColumn.Name = "attachmentColumn";
            this.attachmentColumn.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.attachmentColumn.Width = 25;

            // 
            // chart1
            // 
            this.chart1.Location = new System.Drawing.Point(1, 32);
            this.chart1.Name = "cpuChart";
            this.chart1.Size = new System.Drawing.Size(1416, 128);
            this.chart1.TabIndex = 9;
            this.chart1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));

            System.Windows.Forms.DataVisualization.Charting.ChartArea area = this.chart1.ChartAreas.Add("ChartArea");
    
            this.chart1.MouseWheel += Chart1_MouseWheel;
            this.chart1.MouseClick += Chart1_MouseClick;
            this.chart1.GetToolTipText += Chart1_GetToolTipText;

            area.InnerPlotPosition.Auto = true;
            area.IsSameFontSizeForAllAxes = true;
            
            area.CursorX.AutoScroll = true;
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = false;

            area.AxisX.MinorGrid.LineColor = System.Drawing.Color.LightGray; 
            area.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            area.AxisX.ScaleView.Zoomable = true;   
            area.AxisX.ScaleView.Position = 0;
            area.AxisX.ScaleView.Size = 10;
            area.AxisX.Minimum = 0.0f;

            area.AxisX.ScrollBar.Enabled = true;
            area.AxisX.ScrollBar.ButtonColor = System.Drawing.Color.LightGray;
            area.AxisX.ScrollBar.Size = 12;
            area.AxisX.ScrollBar.ButtonStyle = System.Windows.Forms.DataVisualization.Charting.ScrollBarButtonStyles.None;
            area.AxisX.ScrollBar.IsPositionedInside = true;
            area.AxisX.LabelStyle.Enabled = false;


            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisY.MinorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisY.LabelStyle.Enabled = true;
            area.AxisY.IsMarginVisible = false;
            area.AxisY.Minimum = 0.0f;
            area.AxisY.Maximum = 10.0f;
            area.AxisY.ScaleView.Zoomable = true;
            area.AxisY.LabelAutoFitStyle = System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.DecreaseFont;
            area.AxisY.IsLabelAutoFit = false;
            area.AxisY.LabelStyle.Format = "{00.00}";
            area.AxisY.TitleAlignment = System.Drawing.StringAlignment.Center;
            area.AxisY.Title = "Cpu(ms)";

            System.Random random = new System.Random();

            System.Windows.Forms.DataVisualization.Charting.Series series = this.chart1.Series.Add("All");
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            series.IsValueShownAsLabel = true;
            series.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.None;
            series.Color = System.Drawing.Color.FromArgb(255, 116, 175, 104);
            series.BorderWidth = 1;
            series.IsXValueIndexed = true;
            series.IsValueShownAsLabel = false;

            /// 
            /// chart2
            /// ///
            this.chart2.Location = new System.Drawing.Point(1, 160);
            this.chart2.Name = "memChart";
            this.chart2.Size = new System.Drawing.Size(1416, 128);
            this.chart2.TabIndex = 10;
            this.chart2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));

            area = this.chart2.ChartAreas.Add("ChartArea");

            this.chart2.MouseWheel += Chart1_MouseWheel;
            this.chart2.MouseClick += Chart1_MouseClick;
            this.chart2.GetToolTipText += Chart1_GetToolTipText;

            area.InnerPlotPosition.Auto = true;
            area.IsSameFontSizeForAllAxes = true;

            area.CursorX.AutoScroll = true;
            area.CursorX.IsUserEnabled = true;
            area.CursorX.IsUserSelectionEnabled = false;

            area.AxisX.MinorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;

            area.AxisX.ScaleView.Zoomable = true;
            area.AxisX.ScaleView.Position = 0;
            area.AxisX.ScaleView.Size = 10;
            area.AxisX.Minimum = 0.0f;

            area.AxisX.ScrollBar.Enabled = true;
            area.AxisX.ScrollBar.ButtonColor = System.Drawing.Color.LightGray;
            area.AxisX.ScrollBar.Size = 12;
            area.AxisX.ScrollBar.ButtonStyle = System.Windows.Forms.DataVisualization.Charting.ScrollBarButtonStyles.None;
            area.AxisX.ScrollBar.IsPositionedInside = true;
            area.AxisX.LabelStyle.Enabled = false;

            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisY.MinorGrid.LineColor = System.Drawing.Color.LightGray;
            area.AxisY.LabelStyle.Enabled = true;
            area.AxisY.IsMarginVisible = false;
            area.AxisY.Minimum = 0.0f;
            area.AxisY.Maximum = 10.0f;
            area.AxisY.ScaleView.Zoomable = true;
            area.AxisY.LabelAutoFitStyle = System.Windows.Forms.DataVisualization.Charting.LabelAutoFitStyles.StaggeredLabels;
            area.AxisY.IsLabelAutoFit = false;
            area.AxisY.LabelStyle.Format = "{00.00}";
            area.AxisY.TitleAlignment = System.Drawing.StringAlignment.Center;
            area.AxisY.Title = "Memory(kb)";

            series = this.chart2.Series.Add("All");
            series.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Column;
            series.IsValueShownAsLabel = true;
            series.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.None;
            series.Color = System.Drawing.Color.FromArgb(255, 116, 175, 104);
            series.BorderWidth = 1;
            series.IsXValueIndexed = true;
            series.IsValueShownAsLabel = false;

            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(0, 288);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1416, 436);
            this.tabControl1.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top ;
            this.tabControl1.TabIndex = 0;
            
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(1, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(0);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "cpu";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(1, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(0);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "memory";
            this.tabPage2.UseVisualStyleBackColor = true;


            // tvTaskList
            // 
            this.tvTaskList.RowEnter += GridView_RowEnter;
            this.tvTaskList.AllowUserToAddRows = false;
            this.tvTaskList.AllowUserToDeleteRows = false;
            this.tvTaskList.AllowUserToOrderColumns = true;
            this.tvTaskList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top )
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvTaskList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.tvTaskList.BackgroundColor = System.Drawing.Color.White;
            this.tvTaskList.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.tvTaskList.ColumnHeadersHeight = 20;
            this.tvTaskList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.attachmentColumn,
            this.overview,
            this.totalTime,
            this.averageTime,
            this.totalCalls,
            this.luaGC,
            this.monoGC});
            this.tvTaskList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.tvTaskList.ImageList = null;
            this.tvTaskList.Location = new System.Drawing.Point(0, 0);
            this.tvTaskList.Name = "tvTaskList";
            this.tvTaskList.RowHeadersVisible = false;
            this.tvTaskList.RowHeadersWidth = 20;

            this.tvTaskList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tvTaskList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.tvTaskList.Size = new System.Drawing.Size(200, 410);
            this.tvTaskList.TabIndex = 0;

            ///
            /// memorylist
            /// 
            this.memoryList.AllowUserToAddRows = false;
            this.memoryList.AllowUserToDeleteRows = false;
            this.memoryList.AllowUserToOrderColumns = true;
            this.memoryList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.memoryList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.memoryList.BackgroundColor = System.Drawing.Color.White;
            this.memoryList.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.memoryList.ColumnHeadersHeight = 20;
            this.memoryList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.memoryview,
            this.memoryAlloc});
            this.memoryList.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.memoryList.ImageList = null;
            this.memoryList.Location = new System.Drawing.Point(0, 0);
            this.memoryList.Name = "memoryList";
            this.memoryList.RowHeadersVisible = false;
            this.memoryList.RowHeadersWidth = 20;

            this.memoryList.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.memoryList.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.memoryList.Size = new System.Drawing.Size(200, 410);
            this.memoryList.TabIndex = 0;


            // 
            // imageStrip
            // 
            this.imageStrip.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.imageStrip.ImageSize = new System.Drawing.Size(16, 16);
            this.imageStrip.TransparentColor = System.Drawing.Color.Transparent;

          
            // 
            // timer1
            // 
            this.timer1.Interval = 100; // 100ms
            this.timer1.Tick += new System.EventHandler(this.Timer1_Tick);

            // 
            // overview
            // 
            this.overview.DefaultNodeImage = null;
            this.overview.FillWeight = 450F;
            this.overview.HeaderText = "OverView";
            this.overview.MaxInputLength = 3000;
            this.overview.Name = "overview";
            this.overview.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.overview.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            
            // 
            // luaGC
            // 
            this.luaGC.HeaderText = "GC";
            this.luaGC.Name = "luaGC";
            this.luaGC.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            // 
            // monoGC
            // 
            this.monoGC.HeaderText = "MonoGC";
            this.monoGC.Name = "monoGC";
            this.monoGC.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            // 
            // averageTime
            // 
            this.averageTime.HeaderText = "AverageTime(ms)";
            this.averageTime.Name = "averageTime";
            this.averageTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // totalTime
            // 
            this.totalTime.HeaderText = "TotalTime（ms)";
            this.totalTime.Name = "totalTime";
            this.totalTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // totalCalls
            // 
            this.totalCalls.HeaderText = "TotalCalls";
            this.totalCalls.Name = "totalCalls";
            this.totalCalls.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            // 
            // overview
            // 
            this.memoryview.DefaultNodeImage = null;
            this.memoryview.FillWeight = 450F;
            this.memoryview.HeaderText = "OverView";
            this.memoryview.MaxInputLength = 3000;
            this.memoryview.Name = "overview";
            this.memoryview.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.memoryview.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            // 
            // totalCalls
            // 
            this.memoryAlloc.HeaderText = "Alloc";
            this.memoryAlloc.Name = "memoryAlloc";
            this.memoryAlloc.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;

            // 
            // ProfilerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.ClientSize = new System.Drawing.Size(1416, 577);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.clearBtn);
            //this.Controls.Add(this.searchBtn);
            //this.Controls.Add(this.searchBox);
            this.Controls.Add(this.deattachBtn);
            this.Controls.Add(this.processCom);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.injectButton);
            this.Controls.Add(this.playBtn);
            this.Controls.Add(this.pauseBtn);
            this.Controls.Add(this.captureBtn);
            this.Controls.Add(this.memoryDiffBtn);
            this.Controls.Add(this.markBtn);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.chart2);
            this.Controls.Add(this.tvTaskList);
            this.Controls.Add(this.memoryList);
            this.Controls.Add(this.tips);

            this.tabPage1.Controls.Add(this.tvTaskList);
            this.tabPage2.Controls.Add(this.memoryList);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ProfilerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LuaProfiler";
            ((System.ComponentModel.ISupportInitialize)(this.tvTaskList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.memoryList)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TreeGridView tvTaskList;
        private TreeGridView memoryList;
        private System.Windows.Forms.ImageList imageStrip;
        private System.Windows.Forms.Button injectButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label tips;
        private System.Windows.Forms.ComboBox processCom;
        private System.Windows.Forms.Button deattachBtn;
        private System.Windows.Forms.Button playBtn;
        private System.Windows.Forms.Button pauseBtn;
        private System.Windows.Forms.Button captureBtn;
        private System.Windows.Forms.Button memoryDiffBtn;
        private System.Windows.Forms.Button clearBtn;
        private System.Windows.Forms.Button markBtn;

        //private System.Windows.Forms.TextBox searchBox;
        //private System.Windows.Forms.Button searchBtn;


        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.DataVisualization.Charting.Chart chart2;
        private System.Windows.Forms.DataGridViewImageColumn attachmentColumn;
        private TreeGridColumn overview;

        private TreeGridColumn memoryview;
        private System.Windows.Forms.DataGridViewTextBoxColumn memoryAlloc;

        private System.Windows.Forms.DataGridViewTextBoxColumn luaGC;
        private System.Windows.Forms.DataGridViewTextBoxColumn monoGC;
        private System.Windows.Forms.DataGridViewTextBoxColumn averageTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn totalTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn totalCalls;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
       
        
    }
}