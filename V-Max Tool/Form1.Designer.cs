﻿namespace V_Max_Tool
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
            this.f_load = new System.Windows.Forms.CheckBox();
            this.Out_Type = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.T_Info = new System.Windows.Forms.CheckBox();
            this.out_size = new System.Windows.Forms.ListBox();
            this.out_dif = new System.Windows.Forms.ListBox();
            this.ss = new System.Windows.Forms.ListBox();
            this.sf = new System.Windows.Forms.ListBox();
            this.out_track = new System.Windows.Forms.ListBox();
            this.outbox = new System.Windows.Forms.GroupBox();
            this.Out_density = new System.Windows.Forms.ListBox();
            this.out_rpm = new System.Windows.Forms.ListBox();
            this.Source = new System.Windows.Forms.Label();
            this.Output = new System.Windows.Forms.Label();
            this.inbox = new System.Windows.Forms.GroupBox();
            this.sd = new System.Windows.Forms.ListBox();
            this.strack = new System.Windows.Forms.ListBox();
            this.sl = new System.Windows.Forms.ListBox();
            this.Drag_pic = new System.Windows.Forms.PictureBox();
            this.V2_Custom = new System.Windows.Forms.CheckBox();
            this.V2_hlen = new System.Windows.Forms.NumericUpDown();
            this.V2_Add_Sync = new System.Windows.Forms.CheckBox();
            this.V2_Auto_Adj = new System.Windows.Forms.CheckBox();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.Main = new System.Windows.Forms.TabPage();
            this.Adj_cbm = new System.Windows.Forms.CheckBox();
            this.Adv_V2_Opts = new System.Windows.Forms.TabPage();
            this.Re_Align = new System.Windows.Forms.CheckBox();
            this.v2exp = new System.Windows.Forms.Label();
            this.v2adv = new System.Windows.Forms.Label();
            this.Adv_V3_Opts = new System.Windows.Forms.TabPage();
            this.v3exp = new System.Windows.Forms.Label();
            this.v3adv = new System.Windows.Forms.Label();
            this.V3_hlen = new System.Windows.Forms.NumericUpDown();
            this.V3_Auto_Adj = new System.Windows.Forms.CheckBox();
            this.V3_Custom = new System.Windows.Forms.CheckBox();
            this.Adv_ctrl = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.Img_opts = new System.Windows.Forms.GroupBox();
            this.Flat_Interp = new System.Windows.Forms.CheckBox();
            this.Cap_margins = new System.Windows.Forms.CheckBox();
            this.Show_sec = new System.Windows.Forms.CheckBox();
            this.Rev_View = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.Img_Q = new System.Windows.Forms.ComboBox();
            this.Img_zoom = new System.Windows.Forms.CheckBox();
            this.panPic = new System.Windows.Forms.Panel();
            this.coords = new System.Windows.Forms.Label();
            this.Disk_Image = new System.Windows.Forms.PictureBox();
            this.Img_style = new System.Windows.Forms.GroupBox();
            this.Flat_View = new System.Windows.Forms.RadioButton();
            this.Circle_View = new System.Windows.Forms.RadioButton();
            this.Save_Circle_btn = new System.Windows.Forms.Button();
            this.Img_View = new System.Windows.Forms.GroupBox();
            this.Out_view = new System.Windows.Forms.RadioButton();
            this.Src_view = new System.Windows.Forms.RadioButton();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.Track_Info = new System.Windows.Forms.ListBox();
            this.Save_Dialog = new System.Windows.Forms.SaveFileDialog();
            this.Circle_Render = new System.Windows.Forms.ProgressBar();
            this.outbox.SuspendLayout();
            this.inbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.V2_hlen)).BeginInit();
            this.Tabs.SuspendLayout();
            this.Main.SuspendLayout();
            this.Adv_V2_Opts.SuspendLayout();
            this.Adv_V3_Opts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.V3_hlen)).BeginInit();
            this.Adv_ctrl.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.Img_opts.SuspendLayout();
            this.panPic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Disk_Image)).BeginInit();
            this.Img_style.SuspendLayout();
            this.Img_View.SuspendLayout();
            this.tabPage1.SuspendLayout();
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
            this.out_size.BackColor = System.Drawing.Color.LightGray;
            this.out_size.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_size.FormattingEnabled = true;
            this.out_size.ItemHeight = 25;
            this.out_size.Location = new System.Drawing.Point(150, 30);
            this.out_size.Name = "out_size";
            this.out_size.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_size.Size = new System.Drawing.Size(100, 1002);
            this.out_size.TabIndex = 23;
            // 
            // out_dif
            // 
            this.out_dif.BackColor = System.Drawing.Color.LightGray;
            this.out_dif.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_dif.ForeColor = System.Drawing.Color.BlueViolet;
            this.out_dif.FormattingEnabled = true;
            this.out_dif.ItemHeight = 25;
            this.out_dif.Location = new System.Drawing.Point(247, 30);
            this.out_dif.Name = "out_dif";
            this.out_dif.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_dif.Size = new System.Drawing.Size(73, 1002);
            this.out_dif.TabIndex = 24;
            // 
            // ss
            // 
            this.ss.BackColor = System.Drawing.Color.Lavender;
            this.ss.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ss.FormattingEnabled = true;
            this.ss.ItemHeight = 25;
            this.ss.Location = new System.Drawing.Point(266, 30);
            this.ss.Name = "ss";
            this.ss.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.ss.Size = new System.Drawing.Size(62, 1002);
            this.ss.TabIndex = 25;
            // 
            // sf
            // 
            this.sf.BackColor = System.Drawing.Color.Lavender;
            this.sf.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sf.FormattingEnabled = true;
            this.sf.ItemHeight = 25;
            this.sf.Location = new System.Drawing.Point(153, 30);
            this.sf.Name = "sf";
            this.sf.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sf.Size = new System.Drawing.Size(115, 1002);
            this.sf.TabIndex = 26;
            // 
            // out_track
            // 
            this.out_track.BackColor = System.Drawing.Color.LightGray;
            this.out_track.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_track.ForeColor = System.Drawing.Color.Blue;
            this.out_track.FormattingEnabled = true;
            this.out_track.ItemHeight = 25;
            this.out_track.Location = new System.Drawing.Point(15, 30);
            this.out_track.Name = "out_track";
            this.out_track.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_track.Size = new System.Drawing.Size(53, 1002);
            this.out_track.TabIndex = 29;
            // 
            // outbox
            // 
            this.outbox.BackColor = System.Drawing.Color.LightGray;
            this.outbox.Controls.Add(this.Out_density);
            this.outbox.Controls.Add(this.out_rpm);
            this.outbox.Controls.Add(this.out_track);
            this.outbox.Controls.Add(this.out_dif);
            this.outbox.Controls.Add(this.out_size);
            this.outbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.outbox.ForeColor = System.Drawing.Color.Indigo;
            this.outbox.Location = new System.Drawing.Point(476, 245);
            this.outbox.Name = "outbox";
            this.outbox.Size = new System.Drawing.Size(445, 1047);
            this.outbox.TabIndex = 22;
            this.outbox.TabStop = false;
            this.outbox.Text = "Track/ RPM /    Size    /  Diff  / Density";
            // 
            // Out_density
            // 
            this.Out_density.BackColor = System.Drawing.Color.LightGray;
            this.Out_density.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Out_density.FormattingEnabled = true;
            this.Out_density.ItemHeight = 25;
            this.Out_density.Location = new System.Drawing.Point(316, 30);
            this.Out_density.Name = "Out_density";
            this.Out_density.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.Out_density.Size = new System.Drawing.Size(111, 1002);
            this.Out_density.TabIndex = 33;
            // 
            // out_rpm
            // 
            this.out_rpm.BackColor = System.Drawing.Color.LightGray;
            this.out_rpm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_rpm.ForeColor = System.Drawing.Color.ForestGreen;
            this.out_rpm.FormattingEnabled = true;
            this.out_rpm.ItemHeight = 25;
            this.out_rpm.Location = new System.Drawing.Point(67, 30);
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
            this.Output.Location = new System.Drawing.Point(509, 213);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(76, 25);
            this.Output.TabIndex = 24;
            this.Output.Text = "Output";
            // 
            // inbox
            // 
            this.inbox.BackColor = System.Drawing.Color.Lavender;
            this.inbox.Controls.Add(this.sd);
            this.inbox.Controls.Add(this.strack);
            this.inbox.Controls.Add(this.sf);
            this.inbox.Controls.Add(this.ss);
            this.inbox.Controls.Add(this.sl);
            this.inbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.inbox.ForeColor = System.Drawing.Color.Indigo;
            this.inbox.Location = new System.Drawing.Point(21, 245);
            this.inbox.Name = "inbox";
            this.inbox.Size = new System.Drawing.Size(396, 1047);
            this.inbox.TabIndex = 25;
            this.inbox.TabStop = false;
            this.inbox.Text = "Trk / Size / Format / Sectors / Dens";
            // 
            // sd
            // 
            this.sd.BackColor = System.Drawing.Color.Lavender;
            this.sd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sd.FormattingEnabled = true;
            this.sd.ItemHeight = 25;
            this.sd.Location = new System.Drawing.Point(323, 30);
            this.sd.Name = "sd";
            this.sd.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sd.Size = new System.Drawing.Size(48, 1002);
            this.sd.TabIndex = 32;
            // 
            // strack
            // 
            this.strack.BackColor = System.Drawing.Color.Lavender;
            this.strack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.strack.ForeColor = System.Drawing.Color.Blue;
            this.strack.FormattingEnabled = true;
            this.strack.ItemHeight = 25;
            this.strack.Location = new System.Drawing.Point(14, 30);
            this.strack.Name = "strack";
            this.strack.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.strack.Size = new System.Drawing.Size(53, 1002);
            this.strack.TabIndex = 31;
            // 
            // sl
            // 
            this.sl.BackColor = System.Drawing.Color.Lavender;
            this.sl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sl.FormattingEnabled = true;
            this.sl.ItemHeight = 25;
            this.sl.Location = new System.Drawing.Point(67, 30);
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
            this.Drag_pic.Location = new System.Drawing.Point(27, 275);
            this.Drag_pic.Name = "Drag_pic";
            this.Drag_pic.Size = new System.Drawing.Size(863, 1002);
            this.Drag_pic.TabIndex = 27;
            this.Drag_pic.TabStop = false;
            // 
            // V2_Custom
            // 
            this.V2_Custom.AutoSize = true;
            this.V2_Custom.Location = new System.Drawing.Point(114, 76);
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
            this.V2_hlen.Location = new System.Drawing.Point(415, 74);
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
            this.V2_hlen.ValueChanged += new System.EventHandler(this.V2_hlen_ValueChanged);
            // 
            // V2_Add_Sync
            // 
            this.V2_Add_Sync.AutoSize = true;
            this.V2_Add_Sync.Location = new System.Drawing.Point(114, 111);
            this.V2_Add_Sync.Name = "V2_Add_Sync";
            this.V2_Add_Sync.Size = new System.Drawing.Size(311, 29);
            this.V2_Add_Sync.TabIndex = 32;
            this.V2_Add_Sync.Text = "Add sync to syncless tracks";
            this.V2_Add_Sync.UseVisualStyleBackColor = true;
            this.V2_Add_Sync.CheckedChanged += new System.EventHandler(this.V2_Add_Sync_CheckedChanged);
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
            this.Tabs.Location = new System.Drawing.Point(21, 10);
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
            // Adv_V2_Opts
            // 
            this.Adv_V2_Opts.BackColor = System.Drawing.Color.Gainsboro;
            this.Adv_V2_Opts.Controls.Add(this.Re_Align);
            this.Adv_V2_Opts.Controls.Add(this.v2exp);
            this.Adv_V2_Opts.Controls.Add(this.v2adv);
            this.Adv_V2_Opts.Controls.Add(this.V2_Auto_Adj);
            this.Adv_V2_Opts.Controls.Add(this.V2_Custom);
            this.Adv_V2_Opts.Controls.Add(this.V2_hlen);
            this.Adv_V2_Opts.Controls.Add(this.V2_Add_Sync);
            this.Adv_V2_Opts.Location = new System.Drawing.Point(8, 39);
            this.Adv_V2_Opts.Name = "Adv_V2_Opts";
            this.Adv_V2_Opts.Padding = new System.Windows.Forms.Padding(3);
            this.Adv_V2_Opts.Size = new System.Drawing.Size(784, 146);
            this.Adv_V2_Opts.TabIndex = 1;
            this.Adv_V2_Opts.Text = "V-Max v2 Advanced";
            // 
            // Re_Align
            // 
            this.Re_Align.AutoSize = true;
            this.Re_Align.Location = new System.Drawing.Point(6, 41);
            this.Re_Align.Name = "Re_Align";
            this.Re_Align.Size = new System.Drawing.Size(199, 29);
            this.Re_Align.TabIndex = 38;
            this.Re_Align.Text = "Re-Align Loader";
            this.Re_Align.UseVisualStyleBackColor = true;
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
            this.v2adv.Location = new System.Drawing.Point(257, 41);
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
            this.v3adv.Location = new System.Drawing.Point(58, 44);
            this.v3adv.Name = "v3adv";
            this.v3adv.Size = new System.Drawing.Size(240, 25);
            this.v3adv.TabIndex = 38;
            this.v3adv.Text = "Advanced Users ONLY!";
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
            this.V3_hlen.ValueChanged += new System.EventHandler(this.V3_hlen_ValueChanged);
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
            // Adv_ctrl
            // 
            this.Adv_ctrl.Controls.Add(this.tabPage2);
            this.Adv_ctrl.Controls.Add(this.tabPage1);
            this.Adv_ctrl.Location = new System.Drawing.Point(949, 12);
            this.Adv_ctrl.Name = "Adv_ctrl";
            this.Adv_ctrl.SelectedIndex = 0;
            this.Adv_ctrl.Size = new System.Drawing.Size(1167, 1339);
            this.Adv_ctrl.TabIndex = 36;
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.DimGray;
            this.tabPage2.Controls.Add(this.Img_opts);
            this.tabPage2.Controls.Add(this.panPic);
            this.tabPage2.Controls.Add(this.Img_style);
            this.tabPage2.Controls.Add(this.Save_Circle_btn);
            this.tabPage2.Controls.Add(this.Img_View);
            this.tabPage2.Location = new System.Drawing.Point(8, 39);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1151, 1292);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Visualizer";
            // 
            // Img_opts
            // 
            this.Img_opts.Controls.Add(this.Flat_Interp);
            this.Img_opts.Controls.Add(this.Cap_margins);
            this.Img_opts.Controls.Add(this.Show_sec);
            this.Img_opts.Controls.Add(this.Rev_View);
            this.Img_opts.Controls.Add(this.label4);
            this.Img_opts.Controls.Add(this.Img_Q);
            this.Img_opts.Controls.Add(this.Img_zoom);
            this.Img_opts.ForeColor = System.Drawing.Color.Gainsboro;
            this.Img_opts.Location = new System.Drawing.Point(349, 1149);
            this.Img_opts.Name = "Img_opts";
            this.Img_opts.Size = new System.Drawing.Size(454, 136);
            this.Img_opts.TabIndex = 50;
            this.Img_opts.TabStop = false;
            this.Img_opts.Text = "View Options";
            // 
            // Flat_Interp
            // 
            this.Flat_Interp.AutoSize = true;
            this.Flat_Interp.ForeColor = System.Drawing.Color.Silver;
            this.Flat_Interp.Location = new System.Drawing.Point(6, 27);
            this.Flat_Interp.Name = "Flat_Interp";
            this.Flat_Interp.Size = new System.Drawing.Size(145, 29);
            this.Flat_Interp.TabIndex = 47;
            this.Flat_Interp.Text = "Interpolate";
            this.Flat_Interp.UseVisualStyleBackColor = true;
            this.Flat_Interp.CheckedChanged += new System.EventHandler(this.Flat_Interp_CheckedChanged);
            // 
            // Cap_margins
            // 
            this.Cap_margins.AutoSize = true;
            this.Cap_margins.ForeColor = System.Drawing.Color.Silver;
            this.Cap_margins.Location = new System.Drawing.Point(6, 97);
            this.Cap_margins.Name = "Cap_margins";
            this.Cap_margins.Size = new System.Drawing.Size(199, 29);
            this.Cap_margins.TabIndex = 40;
            this.Cap_margins.Text = "Density Margins";
            this.Cap_margins.UseVisualStyleBackColor = true;
            this.Cap_margins.CheckedChanged += new System.EventHandler(this.Adv_Ctrl_SelectedIndexChanged);
            // 
            // Show_sec
            // 
            this.Show_sec.AutoSize = true;
            this.Show_sec.ForeColor = System.Drawing.Color.Silver;
            this.Show_sec.Location = new System.Drawing.Point(6, 62);
            this.Show_sec.Name = "Show_sec";
            this.Show_sec.Size = new System.Drawing.Size(207, 29);
            this.Show_sec.TabIndex = 41;
            this.Show_sec.Text = "Highlight Sectors";
            this.Show_sec.UseVisualStyleBackColor = true;
            this.Show_sec.CheckedChanged += new System.EventHandler(this.Adv_Ctrl_SelectedIndexChanged);
            // 
            // Rev_View
            // 
            this.Rev_View.AutoSize = true;
            this.Rev_View.ForeColor = System.Drawing.Color.Silver;
            this.Rev_View.Location = new System.Drawing.Point(239, 62);
            this.Rev_View.Name = "Rev_View";
            this.Rev_View.Size = new System.Drawing.Size(182, 29);
            this.Rev_View.TabIndex = 46;
            this.Rev_View.Text = "Alternate View";
            this.Rev_View.UseVisualStyleBackColor = true;
            this.Rev_View.CheckedChanged += new System.EventHandler(this.Rev_View_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.Silver;
            this.label4.Location = new System.Drawing.Point(78, 27);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(155, 25);
            this.label4.TabIndex = 45;
            this.label4.Text = "Render Quality";
            // 
            // Img_Q
            // 
            this.Img_Q.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Img_Q.FormattingEnabled = true;
            this.Img_Q.Location = new System.Drawing.Point(239, 23);
            this.Img_Q.Name = "Img_Q";
            this.Img_Q.Size = new System.Drawing.Size(149, 33);
            this.Img_Q.TabIndex = 44;
            this.Img_Q.SelectedIndexChanged += new System.EventHandler(this.Adv_Ctrl_SelectedIndexChanged);
            // 
            // Img_zoom
            // 
            this.Img_zoom.AutoSize = true;
            this.Img_zoom.ForeColor = System.Drawing.Color.Silver;
            this.Img_zoom.Location = new System.Drawing.Point(239, 97);
            this.Img_zoom.Name = "Img_zoom";
            this.Img_zoom.Size = new System.Drawing.Size(162, 29);
            this.Img_zoom.TabIndex = 37;
            this.Img_zoom.Text = "Zoom Image";
            this.Img_zoom.UseVisualStyleBackColor = true;
            this.Img_zoom.CheckedChanged += new System.EventHandler(this.ImageZoom_CheckedChanged);
            // 
            // panPic
            // 
            this.panPic.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.panPic.Controls.Add(this.Circle_Render);
            this.panPic.Controls.Add(this.coords);
            this.panPic.Controls.Add(this.Disk_Image);
            this.panPic.Location = new System.Drawing.Point(2, 3);
            this.panPic.Name = "panPic";
            this.panPic.Size = new System.Drawing.Size(1143, 1143);
            this.panPic.TabIndex = 37;
            // 
            // coords
            // 
            this.coords.AutoSize = true;
            this.coords.BackColor = System.Drawing.Color.Transparent;
            this.coords.ForeColor = System.Drawing.Color.Silver;
            this.coords.Location = new System.Drawing.Point(891, 8);
            this.coords.Name = "coords";
            this.coords.Size = new System.Drawing.Size(70, 25);
            this.coords.TabIndex = 44;
            this.coords.Text = "label3";
            // 
            // Disk_Image
            // 
            this.Disk_Image.BackColor = System.Drawing.Color.Transparent;
            this.Disk_Image.Location = new System.Drawing.Point(0, 3);
            this.Disk_Image.Name = "Disk_Image";
            this.Disk_Image.Size = new System.Drawing.Size(1140, 1140);
            this.Disk_Image.TabIndex = 0;
            this.Disk_Image.TabStop = false;
            this.Disk_Image.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Disk_Image_MouseDown);
            this.Disk_Image.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Disk_Image_MouseMove);
            this.Disk_Image.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Disk_Image_MouseUp);
            // 
            // Img_style
            // 
            this.Img_style.Controls.Add(this.Flat_View);
            this.Img_style.Controls.Add(this.Circle_View);
            this.Img_style.ForeColor = System.Drawing.Color.Gainsboro;
            this.Img_style.Location = new System.Drawing.Point(6, 1149);
            this.Img_style.Name = "Img_style";
            this.Img_style.Size = new System.Drawing.Size(177, 137);
            this.Img_style.TabIndex = 49;
            this.Img_style.TabStop = false;
            this.Img_style.Text = "Style";
            // 
            // Flat_View
            // 
            this.Flat_View.AutoSize = true;
            this.Flat_View.ForeColor = System.Drawing.Color.Silver;
            this.Flat_View.Location = new System.Drawing.Point(6, 73);
            this.Flat_View.Name = "Flat_View";
            this.Flat_View.Size = new System.Drawing.Size(150, 29);
            this.Flat_View.TabIndex = 1;
            this.Flat_View.TabStop = true;
            this.Flat_View.Text = "Flat Tracks";
            this.Flat_View.UseVisualStyleBackColor = true;
            this.Flat_View.CheckedChanged += new System.EventHandler(this.Circle_View_CheckedChanged);
            // 
            // Circle_View
            // 
            this.Circle_View.AutoSize = true;
            this.Circle_View.ForeColor = System.Drawing.Color.Silver;
            this.Circle_View.Location = new System.Drawing.Point(6, 38);
            this.Circle_View.Name = "Circle_View";
            this.Circle_View.Size = new System.Drawing.Size(156, 29);
            this.Circle_View.TabIndex = 0;
            this.Circle_View.TabStop = true;
            this.Circle_View.Text = "Disk Tracks";
            this.Circle_View.UseVisualStyleBackColor = true;
            this.Circle_View.CheckedChanged += new System.EventHandler(this.Circle_View_CheckedChanged);
            // 
            // Save_Circle_btn
            // 
            this.Save_Circle_btn.Location = new System.Drawing.Point(988, 1163);
            this.Save_Circle_btn.Name = "Save_Circle_btn";
            this.Save_Circle_btn.Size = new System.Drawing.Size(154, 38);
            this.Save_Circle_btn.TabIndex = 47;
            this.Save_Circle_btn.Text = "Save Image";
            this.Save_Circle_btn.UseVisualStyleBackColor = true;
            this.Save_Circle_btn.Click += new System.EventHandler(this.Save_Image_Click);
            // 
            // Img_View
            // 
            this.Img_View.Controls.Add(this.Out_view);
            this.Img_View.Controls.Add(this.Src_view);
            this.Img_View.ForeColor = System.Drawing.Color.Gainsboro;
            this.Img_View.Location = new System.Drawing.Point(189, 1149);
            this.Img_View.Name = "Img_View";
            this.Img_View.Size = new System.Drawing.Size(154, 136);
            this.Img_View.TabIndex = 48;
            this.Img_View.TabStop = false;
            this.Img_View.Text = "Image";
            // 
            // Out_view
            // 
            this.Out_view.AutoSize = true;
            this.Out_view.ForeColor = System.Drawing.Color.Silver;
            this.Out_view.Location = new System.Drawing.Point(23, 37);
            this.Out_view.Name = "Out_view";
            this.Out_view.Size = new System.Drawing.Size(107, 29);
            this.Out_view.TabIndex = 40;
            this.Out_view.TabStop = true;
            this.Out_view.Text = "Output";
            this.Out_view.UseVisualStyleBackColor = true;
            this.Out_view.CheckedChanged += new System.EventHandler(this.Src_view_CheckedChanged);
            // 
            // Src_view
            // 
            this.Src_view.AutoSize = true;
            this.Src_view.ForeColor = System.Drawing.Color.Silver;
            this.Src_view.Location = new System.Drawing.Point(23, 72);
            this.Src_view.Name = "Src_view";
            this.Src_view.Size = new System.Drawing.Size(111, 29);
            this.Src_view.TabIndex = 41;
            this.Src_view.TabStop = true;
            this.Src_view.Text = "Source";
            this.Src_view.UseVisualStyleBackColor = true;
            this.Src_view.CheckedChanged += new System.EventHandler(this.Src_view_CheckedChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.Track_Info);
            this.tabPage1.Location = new System.Drawing.Point(8, 39);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1151, 1292);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "Track Info";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // Track_Info
            // 
            this.Track_Info.BackColor = System.Drawing.Color.DarkGray;
            this.Track_Info.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Track_Info.ForeColor = System.Drawing.Color.Black;
            this.Track_Info.FormattingEnabled = true;
            this.Track_Info.HorizontalScrollbar = true;
            this.Track_Info.ItemHeight = 29;
            this.Track_Info.Location = new System.Drawing.Point(6, 3);
            this.Track_Info.Name = "Track_Info";
            this.Track_Info.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.Track_Info.Size = new System.Drawing.Size(1139, 1280);
            this.Track_Info.TabIndex = 11;
            // 
            // Circle_Render
            // 
            this.Circle_Render.Location = new System.Drawing.Point(447, 560);
            this.Circle_Render.Name = "Circle_Render";
            this.Circle_Render.Size = new System.Drawing.Size(251, 26);
            this.Circle_Render.TabIndex = 51;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2122, 1362);
            this.Controls.Add(this.Adv_ctrl);
            this.Controls.Add(this.Tabs);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Drag_pic);
            this.Controls.Add(this.inbox);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.Source);
            this.Controls.Add(this.outbox);
            this.Controls.Add(this.T_Info);
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
            this.Adv_ctrl.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.Img_opts.ResumeLayout(false);
            this.Img_opts.PerformLayout();
            this.panPic.ResumeLayout(false);
            this.panPic.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Disk_Image)).EndInit();
            this.Img_style.ResumeLayout(false);
            this.Img_style.PerformLayout();
            this.Img_View.ResumeLayout(false);
            this.Img_View.PerformLayout();
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox f_load;
        private System.Windows.Forms.ComboBox Out_Type;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox T_Info;
        private System.Windows.Forms.ListBox out_size;
        private System.Windows.Forms.ListBox out_dif;
        private System.Windows.Forms.ListBox ss;
        private System.Windows.Forms.ListBox sf;
        private System.Windows.Forms.ListBox out_track;
        private System.Windows.Forms.GroupBox outbox;
        private System.Windows.Forms.Label Source;
        private System.Windows.Forms.Label Output;
        private System.Windows.Forms.GroupBox inbox;
        private System.Windows.Forms.ListBox strack;
        private System.Windows.Forms.ListBox sl;
        private System.Windows.Forms.PictureBox Drag_pic;
        private System.Windows.Forms.CheckBox V2_Custom;
        private System.Windows.Forms.NumericUpDown V2_hlen;
        private System.Windows.Forms.CheckBox V2_Add_Sync;
        private System.Windows.Forms.CheckBox V2_Auto_Adj;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage Main;
        private System.Windows.Forms.TabPage Adv_V2_Opts;
        private System.Windows.Forms.TabPage Adv_V3_Opts;
        private System.Windows.Forms.NumericUpDown V3_hlen;
        private System.Windows.Forms.CheckBox V3_Auto_Adj;
        private System.Windows.Forms.CheckBox V3_Custom;
        private System.Windows.Forms.ListBox sd;
        private System.Windows.Forms.ListBox out_rpm;
        private System.Windows.Forms.Label v2adv;
        private System.Windows.Forms.Label v2exp;
        private System.Windows.Forms.Label v3exp;
        private System.Windows.Forms.Label v3adv;
        private System.Windows.Forms.CheckBox Adj_cbm;
        private System.Windows.Forms.CheckBox Re_Align;
        private System.Windows.Forms.ListBox Out_density;
        private System.Windows.Forms.TabControl Adv_ctrl;
        private System.Windows.Forms.ListBox Track_Info;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Panel panPic;
        private System.Windows.Forms.PictureBox Disk_Image;
        private System.Windows.Forms.SaveFileDialog Save_Dialog;
        private System.Windows.Forms.Label coords;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button Save_Circle_btn;
        private System.Windows.Forms.GroupBox Img_opts;
        private System.Windows.Forms.CheckBox Cap_margins;
        private System.Windows.Forms.CheckBox Show_sec;
        private System.Windows.Forms.CheckBox Rev_View;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox Img_Q;
        private System.Windows.Forms.CheckBox Img_zoom;
        private System.Windows.Forms.GroupBox Img_style;
        private System.Windows.Forms.RadioButton Flat_View;
        private System.Windows.Forms.RadioButton Circle_View;
        private System.Windows.Forms.GroupBox Img_View;
        private System.Windows.Forms.RadioButton Out_view;
        private System.Windows.Forms.RadioButton Src_view;
        private System.Windows.Forms.CheckBox Flat_Interp;
        private System.Windows.Forms.ProgressBar Circle_Render;
    }
}

