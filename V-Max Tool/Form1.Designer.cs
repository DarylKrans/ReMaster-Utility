namespace V_Max_Tool
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.listBox3 = new System.Windows.Forms.ListBox();
            this.f_load = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.Out_Type = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.T_Info = new System.Windows.Forms.CheckBox();
            this.out_size = new System.Windows.Forms.ListBox();
            this.out_dif = new System.Windows.Forms.ListBox();
            this.out_sec = new System.Windows.Forms.ListBox();
            this.out_fmt = new System.Windows.Forms.ListBox();
            this.out_track = new System.Windows.Forms.ListBox();
            this.outbox = new System.Windows.Forms.GroupBox();
            this.out_rpm = new System.Windows.Forms.ListBox();
            this.Source = new System.Windows.Forms.Label();
            this.Output = new System.Windows.Forms.Label();
            this.inbox = new System.Windows.Forms.GroupBox();
            this.sd = new System.Windows.Forms.ListBox();
            this.strack = new System.Windows.Forms.ListBox();
            this.ss = new System.Windows.Forms.ListBox();
            this.sl = new System.Windows.Forms.ListBox();
            this.Drag_pic = new System.Windows.Forms.PictureBox();
            this.V2_Custom = new System.Windows.Forms.CheckBox();
            this.V2_hlen = new System.Windows.Forms.NumericUpDown();
            this.V2_Apply = new System.Windows.Forms.Button();
            this.V2_Add_Sync = new System.Windows.Forms.CheckBox();
            this.V2_Auto_Adj = new System.Windows.Forms.CheckBox();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.Main = new System.Windows.Forms.TabPage();
            this.Adv_V2_Opts = new System.Windows.Forms.TabPage();
            this.V2_rb = new System.Windows.Forms.CheckBox();
            this.RebuildV2 = new System.Windows.Forms.Button();
            this.v2exp = new System.Windows.Forms.Label();
            this.v2adv = new System.Windows.Forms.Label();
            this.Adv_V3_Opts = new System.Windows.Forms.TabPage();
            this.v3exp = new System.Windows.Forms.Label();
            this.v3adv = new System.Windows.Forms.Label();
            this.V3_Apply = new System.Windows.Forms.Button();
            this.V3_hlen = new System.Windows.Forms.NumericUpDown();
            this.V3_Auto_Adj = new System.Windows.Forms.CheckBox();
            this.V3_Custom = new System.Windows.Forms.CheckBox();
            this.Adj_cbm = new System.Windows.Forms.CheckBox();
            this.outbox.SuspendLayout();
            this.inbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.V2_hlen)).BeginInit();
            this.Tabs.SuspendLayout();
            this.Main.SuspendLayout();
            this.Adv_V2_Opts.SuspendLayout();
            this.Adv_V3_Opts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.V3_hlen)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(825, 149);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(96, 43);
            this.button1.TabIndex = 0;
            this.button1.Text = "Save";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Make);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 89);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "label1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 113);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(70, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "label2";
            // 
            // listBox3
            // 
            this.listBox3.BackColor = System.Drawing.Color.DarkGray;
            this.listBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBox3.ForeColor = System.Drawing.Color.Black;
            this.listBox3.FormattingEnabled = true;
            this.listBox3.ItemHeight = 29;
            this.listBox3.Location = new System.Drawing.Point(949, 49);
            this.listBox3.Name = "listBox3";
            this.listBox3.Size = new System.Drawing.Size(969, 1251);
            this.listBox3.TabIndex = 10;
            // 
            // f_load
            // 
            this.f_load.AutoSize = true;
            this.f_load.Location = new System.Drawing.Point(11, 57);
            this.f_load.Name = "f_load";
            this.f_load.Size = new System.Drawing.Size(206, 29);
            this.f_load.TabIndex = 12;
            this.f_load.Text = "Fix Loader Track";
            this.f_load.UseVisualStyleBackColor = true;
            this.f_load.CheckedChanged += new System.EventHandler(this.F_load_CheckedChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(944, 17);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(332, 29);
            this.label7.TabIndex = 13;
            this.label7.Text = "Track Information (source file)";
            // 
            // Out_Type
            // 
            this.Out_Type.AllowDrop = true;
            this.Out_Type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Out_Type.FormattingEnabled = true;
            this.Out_Type.Location = new System.Drawing.Point(183, 6);
            this.Out_Type.Name = "Out_Type";
            this.Out_Type.Size = new System.Drawing.Size(174, 33);
            this.Out_Type.TabIndex = 14;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(6, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(171, 25);
            this.label8.TabIndex = 15;
            this.label8.Text = "Output File Type";
            // 
            // T_Info
            // 
            this.T_Info.AutoSize = true;
            this.T_Info.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.T_Info.Location = new System.Drawing.Point(678, 209);
            this.T_Info.Name = "T_Info";
            this.T_Info.Size = new System.Drawing.Size(225, 29);
            this.T_Info.TabIndex = 16;
            this.T_Info.Text = "Verbose Track Info";
            this.T_Info.UseVisualStyleBackColor = true;
            this.T_Info.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // out_size
            // 
            this.out_size.BackColor = System.Drawing.Color.Gainsboro;
            this.out_size.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_size.FormattingEnabled = true;
            this.out_size.ItemHeight = 25;
            this.out_size.Location = new System.Drawing.Point(140, 30);
            this.out_size.Name = "out_size";
            this.out_size.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_size.Size = new System.Drawing.Size(100, 1002);
            this.out_size.TabIndex = 23;
            // 
            // out_dif
            // 
            this.out_dif.BackColor = System.Drawing.Color.Gainsboro;
            this.out_dif.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_dif.ForeColor = System.Drawing.Color.BlueViolet;
            this.out_dif.FormattingEnabled = true;
            this.out_dif.ItemHeight = 25;
            this.out_dif.Location = new System.Drawing.Point(237, 30);
            this.out_dif.Name = "out_dif";
            this.out_dif.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_dif.Size = new System.Drawing.Size(73, 1002);
            this.out_dif.TabIndex = 24;
            // 
            // out_sec
            // 
            this.out_sec.BackColor = System.Drawing.Color.Gainsboro;
            this.out_sec.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_sec.FormattingEnabled = true;
            this.out_sec.ItemHeight = 25;
            this.out_sec.Location = new System.Drawing.Point(306, 30);
            this.out_sec.Name = "out_sec";
            this.out_sec.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_sec.Size = new System.Drawing.Size(62, 1002);
            this.out_sec.TabIndex = 25;
            // 
            // out_fmt
            // 
            this.out_fmt.BackColor = System.Drawing.Color.Gainsboro;
            this.out_fmt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_fmt.FormattingEnabled = true;
            this.out_fmt.ItemHeight = 25;
            this.out_fmt.Location = new System.Drawing.Point(364, 30);
            this.out_fmt.Name = "out_fmt";
            this.out_fmt.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_fmt.Size = new System.Drawing.Size(145, 1002);
            this.out_fmt.TabIndex = 26;
            // 
            // out_track
            // 
            this.out_track.BackColor = System.Drawing.Color.Gainsboro;
            this.out_track.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_track.ForeColor = System.Drawing.Color.Blue;
            this.out_track.FormattingEnabled = true;
            this.out_track.ItemHeight = 25;
            this.out_track.Location = new System.Drawing.Point(5, 30);
            this.out_track.Name = "out_track";
            this.out_track.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_track.Size = new System.Drawing.Size(53, 1002);
            this.out_track.TabIndex = 29;
            // 
            // outbox
            // 
            this.outbox.BackColor = System.Drawing.Color.Gainsboro;
            this.outbox.Controls.Add(this.out_rpm);
            this.outbox.Controls.Add(this.out_track);
            this.outbox.Controls.Add(this.out_fmt);
            this.outbox.Controls.Add(this.out_sec);
            this.outbox.Controls.Add(this.out_dif);
            this.outbox.Controls.Add(this.out_size);
            this.outbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.outbox.ForeColor = System.Drawing.Color.Indigo;
            this.outbox.Location = new System.Drawing.Point(398, 245);
            this.outbox.Name = "outbox";
            this.outbox.Size = new System.Drawing.Size(521, 1047);
            this.outbox.TabIndex = 22;
            this.outbox.TabStop = false;
            this.outbox.Text = "Track/ RPM /  Size  /  Diff  / Sectors /   Format / ";
            // 
            // out_rpm
            // 
            this.out_rpm.BackColor = System.Drawing.Color.Gainsboro;
            this.out_rpm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_rpm.ForeColor = System.Drawing.Color.ForestGreen;
            this.out_rpm.FormattingEnabled = true;
            this.out_rpm.ItemHeight = 25;
            this.out_rpm.Location = new System.Drawing.Point(57, 30);
            this.out_rpm.Name = "out_rpm";
            this.out_rpm.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_rpm.Size = new System.Drawing.Size(86, 1002);
            this.out_rpm.TabIndex = 30;
            // 
            // Source
            // 
            this.Source.AutoSize = true;
            this.Source.Location = new System.Drawing.Point(22, 214);
            this.Source.Name = "Source";
            this.Source.Size = new System.Drawing.Size(121, 25);
            this.Source.TabIndex = 23;
            this.Source.Text = "Source File";
            // 
            // Output
            // 
            this.Output.AutoSize = true;
            this.Output.Location = new System.Drawing.Point(381, 214);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(76, 25);
            this.Output.TabIndex = 24;
            this.Output.Text = "Output";
            // 
            // inbox
            // 
            this.inbox.BackColor = System.Drawing.Color.Gainsboro;
            this.inbox.Controls.Add(this.sd);
            this.inbox.Controls.Add(this.strack);
            this.inbox.Controls.Add(this.ss);
            this.inbox.Controls.Add(this.sl);
            this.inbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.inbox.ForeColor = System.Drawing.Color.Indigo;
            this.inbox.Location = new System.Drawing.Point(21, 245);
            this.inbox.Name = "inbox";
            this.inbox.Size = new System.Drawing.Size(319, 1047);
            this.inbox.TabIndex = 25;
            this.inbox.TabStop = false;
            this.inbox.Text = "Track / Size / Start / Density";
            // 
            // sd
            // 
            this.sd.BackColor = System.Drawing.Color.Gainsboro;
            this.sd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sd.FormattingEnabled = true;
            this.sd.ItemHeight = 25;
            this.sd.Location = new System.Drawing.Point(222, 30);
            this.sd.Name = "sd";
            this.sd.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sd.Size = new System.Drawing.Size(82, 1002);
            this.sd.TabIndex = 32;
            // 
            // strack
            // 
            this.strack.BackColor = System.Drawing.Color.Gainsboro;
            this.strack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.strack.ForeColor = System.Drawing.Color.Blue;
            this.strack.FormattingEnabled = true;
            this.strack.ItemHeight = 25;
            this.strack.Location = new System.Drawing.Point(6, 30);
            this.strack.Name = "strack";
            this.strack.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.strack.Size = new System.Drawing.Size(53, 1002);
            this.strack.TabIndex = 31;
            // 
            // ss
            // 
            this.ss.BackColor = System.Drawing.Color.Gainsboro;
            this.ss.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ss.FormattingEnabled = true;
            this.ss.ItemHeight = 25;
            this.ss.Location = new System.Drawing.Point(143, 30);
            this.ss.Name = "ss";
            this.ss.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.ss.Size = new System.Drawing.Size(82, 1002);
            this.ss.TabIndex = 30;
            // 
            // sl
            // 
            this.sl.BackColor = System.Drawing.Color.Gainsboro;
            this.sl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sl.FormattingEnabled = true;
            this.sl.ItemHeight = 25;
            this.sl.Location = new System.Drawing.Point(59, 30);
            this.sl.Name = "sl";
            this.sl.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sl.Size = new System.Drawing.Size(90, 1002);
            this.sl.TabIndex = 29;
            // 
            // Drag_pic
            // 
            this.Drag_pic.BackColor = System.Drawing.Color.Transparent;
            this.Drag_pic.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Drag_pic.BackgroundImage")));
            this.Drag_pic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Drag_pic.InitialImage = null;
            this.Drag_pic.Location = new System.Drawing.Point(27, 266);
            this.Drag_pic.Name = "Drag_pic";
            this.Drag_pic.Size = new System.Drawing.Size(863, 1011);
            this.Drag_pic.TabIndex = 27;
            this.Drag_pic.TabStop = false;
            // 
            // V2_Custom
            // 
            this.V2_Custom.AutoSize = true;
            this.V2_Custom.Location = new System.Drawing.Point(5, 74);
            this.V2_Custom.Name = "V2_Custom";
            this.V2_Custom.Size = new System.Drawing.Size(295, 29);
            this.V2_Custom.TabIndex = 28;
            this.V2_Custom.Text = "Use custom header length";
            this.V2_Custom.UseVisualStyleBackColor = true;
            this.V2_Custom.CheckedChanged += new System.EventHandler(this.V2_Custom_CheckedChanged);
            // 
            // V2_hlen
            // 
            this.V2_hlen.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.V2_hlen.Location = new System.Drawing.Point(306, 72);
            this.V2_hlen.Maximum = new decimal(new int[] {
            36,
            0,
            0,
            0});
            this.V2_hlen.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.V2_hlen.Name = "V2_hlen";
            this.V2_hlen.Size = new System.Drawing.Size(120, 31);
            this.V2_hlen.TabIndex = 29;
            this.V2_hlen.Value = new decimal(new int[] {
            18,
            0,
            0,
            0});
            // 
            // V2_Apply
            // 
            this.V2_Apply.Location = new System.Drawing.Point(678, 99);
            this.V2_Apply.Name = "V2_Apply";
            this.V2_Apply.Size = new System.Drawing.Size(100, 44);
            this.V2_Apply.TabIndex = 30;
            this.V2_Apply.Text = "Apply";
            this.V2_Apply.UseVisualStyleBackColor = true;
            this.V2_Apply.Click += new System.EventHandler(this.V2_Apply_Click);
            // 
            // V2_Add_Sync
            // 
            this.V2_Add_Sync.AutoSize = true;
            this.V2_Add_Sync.Location = new System.Drawing.Point(5, 109);
            this.V2_Add_Sync.Name = "V2_Add_Sync";
            this.V2_Add_Sync.Size = new System.Drawing.Size(311, 29);
            this.V2_Add_Sync.TabIndex = 32;
            this.V2_Add_Sync.Text = "Add sync to syncless tracks";
            this.V2_Add_Sync.UseVisualStyleBackColor = true;
            // 
            // V2_Auto_Adj
            // 
            this.V2_Auto_Adj.AutoSize = true;
            this.V2_Auto_Adj.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.V2_Auto_Adj.Location = new System.Drawing.Point(6, 6);
            this.V2_Auto_Adj.Name = "V2_Auto_Adj";
            this.V2_Auto_Adj.Size = new System.Drawing.Size(491, 29);
            this.V2_Auto_Adj.TabIndex = 33;
            this.V2_Auto_Adj.Text = "Auto Adjust tracks to fit track density (300 rpm)";
            this.V2_Auto_Adj.UseVisualStyleBackColor = true;
            this.V2_Auto_Adj.CheckedChanged += new System.EventHandler(this.AutoAdj_CheckedChanged);
            // 
            // Tabs
            // 
            this.Tabs.Controls.Add(this.Main);
            this.Tabs.Controls.Add(this.Adv_V2_Opts);
            this.Tabs.Controls.Add(this.Adv_V3_Opts);
            this.Tabs.Location = new System.Drawing.Point(19, 10);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(800, 193);
            this.Tabs.TabIndex = 35;
            // 
            // Main
            // 
            this.Main.BackColor = System.Drawing.Color.Gainsboro;
            this.Main.Controls.Add(this.Adj_cbm);
            this.Main.Controls.Add(this.label1);
            this.Main.Controls.Add(this.label2);
            this.Main.Controls.Add(this.f_load);
            this.Main.Controls.Add(this.Out_Type);
            this.Main.Controls.Add(this.label8);
            this.Main.Location = new System.Drawing.Point(8, 39);
            this.Main.Name = "Main";
            this.Main.Padding = new System.Windows.Forms.Padding(3);
            this.Main.Size = new System.Drawing.Size(784, 146);
            this.Main.TabIndex = 0;
            this.Main.Text = "Main";
            // 
            // Adv_V2_Opts
            // 
            this.Adv_V2_Opts.BackColor = System.Drawing.Color.Gainsboro;
            this.Adv_V2_Opts.Controls.Add(this.V2_rb);
            this.Adv_V2_Opts.Controls.Add(this.RebuildV2);
            this.Adv_V2_Opts.Controls.Add(this.v2exp);
            this.Adv_V2_Opts.Controls.Add(this.v2adv);
            this.Adv_V2_Opts.Controls.Add(this.V2_Auto_Adj);
            this.Adv_V2_Opts.Controls.Add(this.V2_Custom);
            this.Adv_V2_Opts.Controls.Add(this.V2_Apply);
            this.Adv_V2_Opts.Controls.Add(this.V2_hlen);
            this.Adv_V2_Opts.Controls.Add(this.V2_Add_Sync);
            this.Adv_V2_Opts.Location = new System.Drawing.Point(8, 39);
            this.Adv_V2_Opts.Name = "Adv_V2_Opts";
            this.Adv_V2_Opts.Padding = new System.Windows.Forms.Padding(3);
            this.Adv_V2_Opts.Size = new System.Drawing.Size(784, 146);
            this.Adv_V2_Opts.TabIndex = 1;
            this.Adv_V2_Opts.Text = "V-Max v2 Advanced";
            // 
            // V2_rb
            // 
            this.V2_rb.AutoSize = true;
            this.V2_rb.Location = new System.Drawing.Point(336, 109);
            this.V2_rb.Name = "V2_rb";
            this.V2_rb.Size = new System.Drawing.Size(188, 29);
            this.V2_rb.TabIndex = 37;
            this.V2_rb.Text = "Rebuild Tracks";
            this.V2_rb.UseVisualStyleBackColor = true;
            this.V2_rb.CheckedChanged += new System.EventHandler(this.V2_rb_CheckedChanged);
            // 
            // RebuildV2
            // 
            this.RebuildV2.Location = new System.Drawing.Point(479, 41);
            this.RebuildV2.Name = "RebuildV2";
            this.RebuildV2.Size = new System.Drawing.Size(132, 43);
            this.RebuildV2.TabIndex = 36;
            this.RebuildV2.Text = "Don\'t Use";
            this.RebuildV2.UseVisualStyleBackColor = true;
            this.RebuildV2.Click += new System.EventHandler(this.RebuildV2_Click);
            // 
            // v2exp
            // 
            this.v2exp.AutoSize = true;
            this.v2exp.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.v2exp.Location = new System.Drawing.Point(503, 7);
            this.v2exp.Name = "v2exp";
            this.v2exp.Size = new System.Drawing.Size(137, 25);
            this.v2exp.TabIndex = 35;
            this.v2exp.Text = "Experimental";
            // 
            // v2adv
            // 
            this.v2adv.AutoSize = true;
            this.v2adv.Location = new System.Drawing.Point(50, 38);
            this.v2adv.Name = "v2adv";
            this.v2adv.Size = new System.Drawing.Size(240, 25);
            this.v2adv.TabIndex = 34;
            this.v2adv.Text = "Advanced Users ONLY!";
            // 
            // Adv_V3_Opts
            // 
            this.Adv_V3_Opts.BackColor = System.Drawing.Color.Gainsboro;
            this.Adv_V3_Opts.Controls.Add(this.v3exp);
            this.Adv_V3_Opts.Controls.Add(this.v3adv);
            this.Adv_V3_Opts.Controls.Add(this.V3_Apply);
            this.Adv_V3_Opts.Controls.Add(this.V3_hlen);
            this.Adv_V3_Opts.Controls.Add(this.V3_Auto_Adj);
            this.Adv_V3_Opts.Controls.Add(this.V3_Custom);
            this.Adv_V3_Opts.Location = new System.Drawing.Point(8, 39);
            this.Adv_V3_Opts.Name = "Adv_V3_Opts";
            this.Adv_V3_Opts.Padding = new System.Windows.Forms.Padding(3);
            this.Adv_V3_Opts.Size = new System.Drawing.Size(784, 146);
            this.Adv_V3_Opts.TabIndex = 2;
            this.Adv_V3_Opts.Text = "V-Max v3 Advanced";
            // 
            // v3exp
            // 
            this.v3exp.AutoSize = true;
            this.v3exp.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.v3exp.Location = new System.Drawing.Point(507, 7);
            this.v3exp.Name = "v3exp";
            this.v3exp.Size = new System.Drawing.Size(137, 25);
            this.v3exp.TabIndex = 39;
            this.v3exp.Text = "Experimental";
            // 
            // v3adv
            // 
            this.v3adv.AutoSize = true;
            this.v3adv.Location = new System.Drawing.Point(50, 72);
            this.v3adv.Name = "v3adv";
            this.v3adv.Size = new System.Drawing.Size(240, 25);
            this.v3adv.TabIndex = 38;
            this.v3adv.Text = "Advanced Users ONLY!";
            // 
            // V3_Apply
            // 
            this.V3_Apply.Location = new System.Drawing.Point(678, 99);
            this.V3_Apply.Name = "V3_Apply";
            this.V3_Apply.Size = new System.Drawing.Size(100, 44);
            this.V3_Apply.TabIndex = 37;
            this.V3_Apply.Text = "Apply";
            this.V3_Apply.UseVisualStyleBackColor = true;
            this.V3_Apply.Click += new System.EventHandler(this.V3_Apply_Click);
            // 
            // V3_hlen
            // 
            this.V3_hlen.Location = new System.Drawing.Point(307, 101);
            this.V3_hlen.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
            this.V3_hlen.Minimum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.V3_hlen.Name = "V3_hlen";
            this.V3_hlen.Size = new System.Drawing.Size(120, 31);
            this.V3_hlen.TabIndex = 36;
            this.V3_hlen.Value = new decimal(new int[] {
            6,
            0,
            0,
            0});
            // 
            // V3_Auto_Adj
            // 
            this.V3_Auto_Adj.AutoSize = true;
            this.V3_Auto_Adj.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.V3_Auto_Adj.Location = new System.Drawing.Point(6, 6);
            this.V3_Auto_Adj.Name = "V3_Auto_Adj";
            this.V3_Auto_Adj.Size = new System.Drawing.Size(491, 29);
            this.V3_Auto_Adj.TabIndex = 35;
            this.V3_Auto_Adj.Text = "Auto Adjust tracks to fit track density (300 rpm)";
            this.V3_Auto_Adj.UseVisualStyleBackColor = true;
            this.V3_Auto_Adj.CheckedChanged += new System.EventHandler(this.V3_Auto_Adj_CheckedChanged);
            // 
            // V3_Custom
            // 
            this.V3_Custom.AutoSize = true;
            this.V3_Custom.Location = new System.Drawing.Point(6, 103);
            this.V3_Custom.Name = "V3_Custom";
            this.V3_Custom.Size = new System.Drawing.Size(295, 29);
            this.V3_Custom.TabIndex = 34;
            this.V3_Custom.Text = "Use custom header length";
            this.V3_Custom.UseVisualStyleBackColor = true;
            this.V3_Custom.CheckedChanged += new System.EventHandler(this.V3_Custom_CheckedChanged);
            // 
            // Adj_cbm
            // 
            this.Adj_cbm.AutoSize = true;
            this.Adj_cbm.Location = new System.Drawing.Point(393, 9);
            this.Adj_cbm.Name = "Adj_cbm";
            this.Adj_cbm.Size = new System.Drawing.Size(343, 29);
            this.Adj_cbm.TabIndex = 16;
            this.Adj_cbm.Text = "Adjust CBM tracks to fit density";
            this.Adj_cbm.UseVisualStyleBackColor = true;
            this.Adj_cbm.CheckedChanged += new System.EventHandler(this.Adj_cbm_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(933, 1334);
            this.Controls.Add(this.Tabs);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Drag_pic);
            this.Controls.Add(this.inbox);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.Source);
            this.Controls.Add(this.outbox);
            this.Controls.Add(this.T_Info);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.listBox3);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "V-Max Sync Tool";
            this.outbox.ResumeLayout(false);
            this.inbox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.V2_hlen)).EndInit();
            this.Tabs.ResumeLayout(false);
            this.Main.ResumeLayout(false);
            this.Main.PerformLayout();
            this.Adv_V2_Opts.ResumeLayout(false);
            this.Adv_V2_Opts.PerformLayout();
            this.Adv_V3_Opts.ResumeLayout(false);
            this.Adv_V3_Opts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.V3_hlen)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBox3;
        private System.Windows.Forms.CheckBox f_load;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox Out_Type;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox T_Info;
        private System.Windows.Forms.ListBox out_size;
        private System.Windows.Forms.ListBox out_dif;
        private System.Windows.Forms.ListBox out_sec;
        private System.Windows.Forms.ListBox out_fmt;
        private System.Windows.Forms.ListBox out_track;
        private System.Windows.Forms.GroupBox outbox;
        private System.Windows.Forms.Label Source;
        private System.Windows.Forms.Label Output;
        private System.Windows.Forms.GroupBox inbox;
        private System.Windows.Forms.ListBox strack;
        private System.Windows.Forms.ListBox ss;
        private System.Windows.Forms.ListBox sl;
        private System.Windows.Forms.PictureBox Drag_pic;
        private System.Windows.Forms.CheckBox V2_Custom;
        private System.Windows.Forms.NumericUpDown V2_hlen;
        private System.Windows.Forms.Button V2_Apply;
        private System.Windows.Forms.CheckBox V2_Add_Sync;
        private System.Windows.Forms.CheckBox V2_Auto_Adj;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage Main;
        private System.Windows.Forms.TabPage Adv_V2_Opts;
        private System.Windows.Forms.TabPage Adv_V3_Opts;
        private System.Windows.Forms.Button V3_Apply;
        private System.Windows.Forms.NumericUpDown V3_hlen;
        private System.Windows.Forms.CheckBox V3_Auto_Adj;
        private System.Windows.Forms.CheckBox V3_Custom;
        private System.Windows.Forms.ListBox sd;
        private System.Windows.Forms.ListBox out_rpm;
        private System.Windows.Forms.Label v2adv;
        private System.Windows.Forms.Label v2exp;
        private System.Windows.Forms.Label v3exp;
        private System.Windows.Forms.Label v3adv;
        private System.Windows.Forms.Button RebuildV2;
        private System.Windows.Forms.CheckBox V2_rb;
        private System.Windows.Forms.CheckBox Adj_cbm;
    }
}

