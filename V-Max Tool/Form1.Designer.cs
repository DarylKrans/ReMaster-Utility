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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.f_load = new System.Windows.Forms.CheckBox();
            this.V2_Custom = new System.Windows.Forms.CheckBox();
            this.V2_hlen = new System.Windows.Forms.NumericUpDown();
            this.V2_Add_Sync = new System.Windows.Forms.CheckBox();
            this.V2_Auto_Adj = new System.Windows.Forms.CheckBox();
            this.Tabs = new System.Windows.Forms.TabControl();
            this.Main = new System.Windows.Forms.TabPage();
            this.Import_File = new System.Windows.Forms.GroupBox();
            this.Import_Progress_Bar = new System.Windows.Forms.ProgressBar();
            this.label5 = new System.Windows.Forms.Label();
            this.Save_Disk = new System.Windows.Forms.Button();
            this.VBS_info = new System.Windows.Forms.Panel();
            this.Dir_View = new System.Windows.Forms.CheckBox();
            this.Cust_Density = new System.Windows.Forms.Label();
            this.VM_Ver = new System.Windows.Forms.Label();
            this.Reg_info = new System.Windows.Forms.Panel();
            this.VMax_Tracks = new System.Windows.Forms.Label();
            this.CBM_Tracks = new System.Windows.Forms.Label();
            this.Loader_Track = new System.Windows.Forms.Label();
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
            this.M_render = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.Flat_Render = new System.Windows.Forms.ProgressBar();
            this.Circle_Render = new System.Windows.Forms.ProgressBar();
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.Dir_screen = new System.Windows.Forms.RichTextBox();
            this.Drag_pic = new System.Windows.Forms.PictureBox();
            this.Out_density = new System.Windows.Forms.ListBox();
            this.out_rpm = new System.Windows.Forms.ListBox();
            this.sd = new System.Windows.Forms.ListBox();
            this.out_track = new System.Windows.Forms.ListBox();
            this.out_dif = new System.Windows.Forms.ListBox();
            this.Output = new System.Windows.Forms.Label();
            this.out_size = new System.Windows.Forms.ListBox();
            this.Source = new System.Windows.Forms.Label();
            this.strack = new System.Windows.Forms.ListBox();
            this.sl = new System.Windows.Forms.ListBox();
            this.ss = new System.Windows.Forms.ListBox();
            this.sf = new System.Windows.Forms.ListBox();
            ((System.ComponentModel.ISupportInitialize)(this.V2_hlen)).BeginInit();
            this.Tabs.SuspendLayout();
            this.Main.SuspendLayout();
            this.Import_File.SuspendLayout();
            this.VBS_info.SuspendLayout();
            this.Reg_info.SuspendLayout();
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
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(106, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "File name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(192, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "Track / header info";
            // 
            // f_load
            // 
            this.f_load.AutoSize = true;
            this.f_load.Location = new System.Drawing.Point(508, 29);
            this.f_load.Name = "f_load";
            this.f_load.Size = new System.Drawing.Size(206, 29);
            this.f_load.TabIndex = 12;
            this.f_load.Text = "Fix Loader Track";
            this.f_load.UseVisualStyleBackColor = true;
            this.f_load.CheckedChanged += new System.EventHandler(this.F_load_CheckedChanged);
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
            this.Tabs.Appearance = System.Windows.Forms.TabAppearance.Buttons;
            this.Tabs.Controls.Add(this.Main);
            this.Tabs.Controls.Add(this.Adv_V2_Opts);
            this.Tabs.Controls.Add(this.Adv_V3_Opts);
            this.Tabs.Location = new System.Drawing.Point(12, 10);
            this.Tabs.Name = "Tabs";
            this.Tabs.SelectedIndex = 0;
            this.Tabs.Size = new System.Drawing.Size(916, 210);
            this.Tabs.TabIndex = 35;
            // 
            // Main
            // 
            this.Main.BackColor = System.Drawing.Color.Gainsboro;
            this.Main.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Main.Controls.Add(this.Import_File);
            this.Main.Controls.Add(this.Save_Disk);
            this.Main.Controls.Add(this.f_load);
            this.Main.Controls.Add(this.VBS_info);
            this.Main.Controls.Add(this.Reg_info);
            this.Main.Controls.Add(this.Adj_cbm);
            this.Main.Controls.Add(this.label1);
            this.Main.Controls.Add(this.label2);
            this.Main.Location = new System.Drawing.Point(4, 37);
            this.Main.Name = "Main";
            this.Main.Padding = new System.Windows.Forms.Padding(3);
            this.Main.Size = new System.Drawing.Size(908, 169);
            this.Main.TabIndex = 0;
            this.Main.Text = "File Info";
            // 
            // Import_File
            // 
            this.Import_File.BackColor = System.Drawing.Color.Gainsboro;
            this.Import_File.Controls.Add(this.Import_Progress_Bar);
            this.Import_File.Controls.Add(this.label5);
            this.Import_File.ForeColor = System.Drawing.Color.Black;
            this.Import_File.Location = new System.Drawing.Point(22, 61);
            this.Import_File.Name = "Import_File";
            this.Import_File.Size = new System.Drawing.Size(870, 92);
            this.Import_File.TabIndex = 59;
            this.Import_File.TabStop = false;
            this.Import_File.Text = "Analyzing Tracks";
            // 
            // Import_Progress_Bar
            // 
            this.Import_Progress_Bar.Location = new System.Drawing.Point(6, 30);
            this.Import_Progress_Bar.Name = "Import_Progress_Bar";
            this.Import_Progress_Bar.Size = new System.Drawing.Size(858, 26);
            this.Import_Progress_Bar.TabIndex = 39;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.Location = new System.Drawing.Point(6, 59);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(137, 25);
            this.label5.TabIndex = 38;
            this.label5.Text = "Processing...";
            // 
            // Save_Disk
            // 
            this.Save_Disk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.Save_Disk.Location = new System.Drawing.Point(789, 101);
            this.Save_Disk.Name = "Save_Disk";
            this.Save_Disk.Size = new System.Drawing.Size(104, 43);
            this.Save_Disk.TabIndex = 59;
            this.Save_Disk.Text = "Export";
            this.Save_Disk.UseVisualStyleBackColor = true;
            this.Save_Disk.Click += new System.EventHandler(this.Make);
            // 
            // VBS_info
            // 
            this.VBS_info.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.VBS_info.Controls.Add(this.Dir_View);
            this.VBS_info.Controls.Add(this.Cust_Density);
            this.VBS_info.Controls.Add(this.VM_Ver);
            this.VBS_info.Location = new System.Drawing.Point(386, 61);
            this.VBS_info.Name = "VBS_info";
            this.VBS_info.Size = new System.Drawing.Size(382, 83);
            this.VBS_info.TabIndex = 22;
            // 
            // Dir_View
            // 
            this.Dir_View.AutoSize = true;
            this.Dir_View.Location = new System.Drawing.Point(3, 49);
            this.Dir_View.Name = "Dir_View";
            this.Dir_View.Size = new System.Drawing.Size(182, 29);
            this.Dir_View.TabIndex = 60;
            this.Dir_View.Text = "View Directory";
            this.Dir_View.UseVisualStyleBackColor = true;
            this.Dir_View.CheckedChanged += new System.EventHandler(this.Dir_View_CheckedChanged);
            // 
            // Cust_Density
            // 
            this.Cust_Density.AutoSize = true;
            this.Cust_Density.Location = new System.Drawing.Point(3, 25);
            this.Cust_Density.Name = "Cust_Density";
            this.Cust_Density.Size = new System.Drawing.Size(173, 25);
            this.Cust_Density.TabIndex = 1;
            this.Cust_Density.Text = "Track Densities :";
            // 
            // VM_Ver
            // 
            this.VM_Ver.AutoSize = true;
            this.VM_Ver.Location = new System.Drawing.Point(3, 0);
            this.VM_Ver.Name = "VM_Ver";
            this.VM_Ver.Size = new System.Drawing.Size(165, 25);
            this.VM_Ver.TabIndex = 0;
            this.VM_Ver.Text = "V-Max Version :";
            // 
            // Reg_info
            // 
            this.Reg_info.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Reg_info.Controls.Add(this.VMax_Tracks);
            this.Reg_info.Controls.Add(this.CBM_Tracks);
            this.Reg_info.Controls.Add(this.Loader_Track);
            this.Reg_info.Location = new System.Drawing.Point(23, 61);
            this.Reg_info.Name = "Reg_info";
            this.Reg_info.Size = new System.Drawing.Size(357, 83);
            this.Reg_info.TabIndex = 21;
            // 
            // VMax_Tracks
            // 
            this.VMax_Tracks.AutoSize = true;
            this.VMax_Tracks.Location = new System.Drawing.Point(7, 53);
            this.VMax_Tracks.Name = "VMax_Tracks";
            this.VMax_Tracks.Size = new System.Drawing.Size(138, 25);
            this.VMax_Tracks.TabIndex = 23;
            this.VMax_Tracks.Text = "VMax Tracks";
            // 
            // CBM_Tracks
            // 
            this.CBM_Tracks.AutoSize = true;
            this.CBM_Tracks.Location = new System.Drawing.Point(6, 0);
            this.CBM_Tracks.Name = "CBM_Tracks";
            this.CBM_Tracks.Size = new System.Drawing.Size(130, 25);
            this.CBM_Tracks.TabIndex = 22;
            this.CBM_Tracks.Text = "CBM Tracks";
            // 
            // Loader_Track
            // 
            this.Loader_Track.AutoSize = true;
            this.Loader_Track.Location = new System.Drawing.Point(6, 25);
            this.Loader_Track.Name = "Loader_Track";
            this.Loader_Track.Size = new System.Drawing.Size(139, 25);
            this.Loader_Track.TabIndex = 21;
            this.Loader_Track.Text = "Loader Track";
            // 
            // Adj_cbm
            // 
            this.Adj_cbm.AutoSize = true;
            this.Adj_cbm.Location = new System.Drawing.Point(268, 2);
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
            this.Adv_V2_Opts.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Adv_V2_Opts.Controls.Add(this.Re_Align);
            this.Adv_V2_Opts.Controls.Add(this.v2exp);
            this.Adv_V2_Opts.Controls.Add(this.v2adv);
            this.Adv_V2_Opts.Controls.Add(this.V2_Auto_Adj);
            this.Adv_V2_Opts.Controls.Add(this.V2_Custom);
            this.Adv_V2_Opts.Controls.Add(this.V2_hlen);
            this.Adv_V2_Opts.Controls.Add(this.V2_Add_Sync);
            this.Adv_V2_Opts.Location = new System.Drawing.Point(4, 37);
            this.Adv_V2_Opts.Name = "Adv_V2_Opts";
            this.Adv_V2_Opts.Padding = new System.Windows.Forms.Padding(3);
            this.Adv_V2_Opts.Size = new System.Drawing.Size(908, 169);
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
            this.Adv_V3_Opts.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Adv_V3_Opts.Controls.Add(this.v3exp);
            this.Adv_V3_Opts.Controls.Add(this.v3adv);
            this.Adv_V3_Opts.Controls.Add(this.V3_hlen);
            this.Adv_V3_Opts.Controls.Add(this.V3_Auto_Adj);
            this.Adv_V3_Opts.Controls.Add(this.V3_Custom);
            this.Adv_V3_Opts.Location = new System.Drawing.Point(4, 37);
            this.Adv_V3_Opts.Name = "Adv_V3_Opts";
            this.Adv_V3_Opts.Padding = new System.Windows.Forms.Padding(3);
            this.Adv_V3_Opts.Size = new System.Drawing.Size(908, 169);
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
            this.V3_hlen.Location = new System.Drawing.Point(307, 79);
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
            this.V3_Custom.Location = new System.Drawing.Point(6, 81);
            this.V3_Custom.Name = "V3_Custom";
            this.V3_Custom.Size = new System.Drawing.Size(295, 29);
            this.V3_Custom.TabIndex = 34;
            this.V3_Custom.Text = "Use custom header length";
            this.V3_Custom.UseVisualStyleBackColor = true;
            this.V3_Custom.CheckedChanged += new System.EventHandler(this.V3_Custom_CheckedChanged);
            // 
            // Adv_ctrl
            // 
            this.Adv_ctrl.Appearance = System.Windows.Forms.TabAppearance.Buttons;
            this.Adv_ctrl.Controls.Add(this.tabPage2);
            this.Adv_ctrl.Controls.Add(this.tabPage1);
            this.Adv_ctrl.Location = new System.Drawing.Point(930, 10);
            this.Adv_ctrl.Name = "Adv_ctrl";
            this.Adv_ctrl.SelectedIndex = 0;
            this.Adv_ctrl.Size = new System.Drawing.Size(1153, 1348);
            this.Adv_ctrl.TabIndex = 36;
            // 
            // tabPage2
            // 
            this.tabPage2.BackColor = System.Drawing.Color.DimGray;
            this.tabPage2.Controls.Add(this.M_render);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.Flat_Render);
            this.tabPage2.Controls.Add(this.Circle_Render);
            this.tabPage2.Controls.Add(this.Img_opts);
            this.tabPage2.Controls.Add(this.panPic);
            this.tabPage2.Controls.Add(this.Img_style);
            this.tabPage2.Controls.Add(this.Save_Circle_btn);
            this.tabPage2.Controls.Add(this.Img_View);
            this.tabPage2.Location = new System.Drawing.Point(4, 37);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1145, 1307);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Visualizer";
            // 
            // M_render
            // 
            this.M_render.Location = new System.Drawing.Point(811, 1163);
            this.M_render.Name = "M_render";
            this.M_render.Size = new System.Drawing.Size(118, 38);
            this.M_render.TabIndex = 54;
            this.M_render.Text = "Render";
            this.M_render.UseVisualStyleBackColor = true;
            this.M_render.Click += new System.EventHandler(this.Manual_render_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.Yellow;
            this.label3.Location = new System.Drawing.Point(881, 1204);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(186, 25);
            this.label3.TabIndex = 53;
            this.label3.Text = "Rendering Images";
            // 
            // Flat_Render
            // 
            this.Flat_Render.Location = new System.Drawing.Point(812, 1259);
            this.Flat_Render.Name = "Flat_Render";
            this.Flat_Render.Size = new System.Drawing.Size(317, 16);
            this.Flat_Render.TabIndex = 52;
            // 
            // Circle_Render
            // 
            this.Circle_Render.Location = new System.Drawing.Point(812, 1232);
            this.Circle_Render.Name = "Circle_Render";
            this.Circle_Render.Size = new System.Drawing.Size(317, 16);
            this.Circle_Render.TabIndex = 51;
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
            this.Flat_Interp.Location = new System.Drawing.Point(239, 63);
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
            this.coords.Location = new System.Drawing.Point(18, 1109);
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
            this.Save_Circle_btn.Location = new System.Drawing.Point(975, 1163);
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
            this.tabPage1.Location = new System.Drawing.Point(4, 37);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1145, 1307);
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
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Gainsboro;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.Dir_screen);
            this.panel1.Controls.Add(this.Drag_pic);
            this.panel1.Controls.Add(this.Out_density);
            this.panel1.Controls.Add(this.out_rpm);
            this.panel1.Controls.Add(this.sd);
            this.panel1.Controls.Add(this.out_track);
            this.panel1.Controls.Add(this.out_dif);
            this.panel1.Controls.Add(this.Output);
            this.panel1.Controls.Add(this.out_size);
            this.panel1.Controls.Add(this.Source);
            this.panel1.Controls.Add(this.strack);
            this.panel1.Controls.Add(this.sl);
            this.panel1.Controls.Add(this.ss);
            this.panel1.Controls.Add(this.sf);
            this.panel1.Location = new System.Drawing.Point(21, 226);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(900, 1123);
            this.panel1.TabIndex = 37;
            // 
            // Dir_screen
            // 
            this.Dir_screen.CausesValidation = false;
            this.Dir_screen.Font = new System.Drawing.Font("C64 Pro Mono", 10.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Dir_screen.Location = new System.Drawing.Point(3, 3);
            this.Dir_screen.Name = "Dir_screen";
            this.Dir_screen.Size = new System.Drawing.Size(892, 1115);
            this.Dir_screen.TabIndex = 38;
            this.Dir_screen.Text = "";
            // 
            // Drag_pic
            // 
            this.Drag_pic.BackColor = System.Drawing.Color.Transparent;
            this.Drag_pic.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("Drag_pic.BackgroundImage")));
            this.Drag_pic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.Drag_pic.InitialImage = null;
            this.Drag_pic.Location = new System.Drawing.Point(18, 28);
            this.Drag_pic.Name = "Drag_pic";
            this.Drag_pic.Size = new System.Drawing.Size(863, 1009);
            this.Drag_pic.TabIndex = 58;
            this.Drag_pic.TabStop = false;
            // 
            // Out_density
            // 
            this.Out_density.BackColor = System.Drawing.Color.Lavender;
            this.Out_density.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Out_density.FormattingEnabled = true;
            this.Out_density.ItemHeight = 25;
            this.Out_density.Location = new System.Drawing.Point(752, 28);
            this.Out_density.Name = "Out_density";
            this.Out_density.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.Out_density.Size = new System.Drawing.Size(111, 1002);
            this.Out_density.TabIndex = 33;
            // 
            // out_rpm
            // 
            this.out_rpm.BackColor = System.Drawing.Color.Lavender;
            this.out_rpm.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_rpm.ForeColor = System.Drawing.Color.ForestGreen;
            this.out_rpm.FormattingEnabled = true;
            this.out_rpm.ItemHeight = 25;
            this.out_rpm.Location = new System.Drawing.Point(503, 28);
            this.out_rpm.Name = "out_rpm";
            this.out_rpm.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_rpm.Size = new System.Drawing.Size(86, 1002);
            this.out_rpm.TabIndex = 30;
            // 
            // sd
            // 
            this.sd.BackColor = System.Drawing.Color.Lavender;
            this.sd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sd.FormattingEnabled = true;
            this.sd.ItemHeight = 25;
            this.sd.Location = new System.Drawing.Point(320, 28);
            this.sd.Name = "sd";
            this.sd.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sd.Size = new System.Drawing.Size(48, 1002);
            this.sd.TabIndex = 32;
            // 
            // out_track
            // 
            this.out_track.BackColor = System.Drawing.Color.Lavender;
            this.out_track.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_track.ForeColor = System.Drawing.Color.Blue;
            this.out_track.FormattingEnabled = true;
            this.out_track.ItemHeight = 25;
            this.out_track.Location = new System.Drawing.Point(451, 28);
            this.out_track.Name = "out_track";
            this.out_track.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_track.Size = new System.Drawing.Size(53, 1002);
            this.out_track.TabIndex = 29;
            // 
            // out_dif
            // 
            this.out_dif.BackColor = System.Drawing.Color.Lavender;
            this.out_dif.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_dif.ForeColor = System.Drawing.Color.BlueViolet;
            this.out_dif.FormattingEnabled = true;
            this.out_dif.ItemHeight = 25;
            this.out_dif.Location = new System.Drawing.Point(683, 28);
            this.out_dif.Name = "out_dif";
            this.out_dif.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_dif.Size = new System.Drawing.Size(73, 1002);
            this.out_dif.TabIndex = 24;
            // 
            // Output
            // 
            this.Output.AutoSize = true;
            this.Output.Location = new System.Drawing.Point(610, 0);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(76, 25);
            this.Output.TabIndex = 54;
            this.Output.Text = "Output";
            // 
            // out_size
            // 
            this.out_size.BackColor = System.Drawing.Color.Lavender;
            this.out_size.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.out_size.FormattingEnabled = true;
            this.out_size.ItemHeight = 25;
            this.out_size.Location = new System.Drawing.Point(586, 28);
            this.out_size.Name = "out_size";
            this.out_size.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.out_size.Size = new System.Drawing.Size(100, 1002);
            this.out_size.TabIndex = 23;
            // 
            // Source
            // 
            this.Source.AutoSize = true;
            this.Source.Location = new System.Drawing.Point(124, 0);
            this.Source.Name = "Source";
            this.Source.Size = new System.Drawing.Size(121, 25);
            this.Source.TabIndex = 53;
            this.Source.Text = "Source File";
            // 
            // strack
            // 
            this.strack.BackColor = System.Drawing.Color.Lavender;
            this.strack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.strack.ForeColor = System.Drawing.Color.Blue;
            this.strack.FormattingEnabled = true;
            this.strack.ItemHeight = 25;
            this.strack.Location = new System.Drawing.Point(11, 28);
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
            this.sl.Location = new System.Drawing.Point(64, 28);
            this.sl.Name = "sl";
            this.sl.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sl.Size = new System.Drawing.Size(90, 1002);
            this.sl.TabIndex = 29;
            // 
            // ss
            // 
            this.ss.BackColor = System.Drawing.Color.Lavender;
            this.ss.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ss.FormattingEnabled = true;
            this.ss.ItemHeight = 25;
            this.ss.Location = new System.Drawing.Point(263, 28);
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
            this.sf.Location = new System.Drawing.Point(150, 28);
            this.sf.Name = "sf";
            this.sf.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.sf.Size = new System.Drawing.Size(115, 1002);
            this.sf.TabIndex = 26;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2612, 1386);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.Adv_ctrl);
            this.Controls.Add(this.Tabs);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "V-Max Sync Tool";
            ((System.ComponentModel.ISupportInitialize)(this.V2_hlen)).EndInit();
            this.Tabs.ResumeLayout(false);
            this.Main.ResumeLayout(false);
            this.Main.PerformLayout();
            this.Import_File.ResumeLayout(false);
            this.Import_File.PerformLayout();
            this.VBS_info.ResumeLayout(false);
            this.VBS_info.PerformLayout();
            this.Reg_info.ResumeLayout(false);
            this.Reg_info.PerformLayout();
            this.Adv_V2_Opts.ResumeLayout(false);
            this.Adv_V2_Opts.PerformLayout();
            this.Adv_V3_Opts.ResumeLayout(false);
            this.Adv_V3_Opts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.V3_hlen)).EndInit();
            this.Adv_ctrl.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
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
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox f_load;
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
        private System.Windows.Forms.Label v2adv;
        private System.Windows.Forms.Label v2exp;
        private System.Windows.Forms.Label v3exp;
        private System.Windows.Forms.Label v3adv;
        private System.Windows.Forms.CheckBox Adj_cbm;
        private System.Windows.Forms.CheckBox Re_Align;
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
        private System.Windows.Forms.ProgressBar Flat_Render;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button M_render;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ListBox sd;
        private System.Windows.Forms.ListBox strack;
        private System.Windows.Forms.ListBox sf;
        private System.Windows.Forms.ListBox ss;
        private System.Windows.Forms.ListBox sl;
        private System.Windows.Forms.Label Output;
        private System.Windows.Forms.Label Source;
        private System.Windows.Forms.ListBox Out_density;
        private System.Windows.Forms.ListBox out_rpm;
        private System.Windows.Forms.ListBox out_track;
        private System.Windows.Forms.ListBox out_dif;
        private System.Windows.Forms.ListBox out_size;
        private System.Windows.Forms.GroupBox Import_File;
        private System.Windows.Forms.ProgressBar Import_Progress_Bar;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.PictureBox Drag_pic;
        private System.Windows.Forms.Panel Reg_info;
        private System.Windows.Forms.Label VMax_Tracks;
        private System.Windows.Forms.Label CBM_Tracks;
        private System.Windows.Forms.Label Loader_Track;
        private System.Windows.Forms.Panel VBS_info;
        private System.Windows.Forms.Label Cust_Density;
        private System.Windows.Forms.Label VM_Ver;
        private System.Windows.Forms.Button Save_Disk;
        private System.Windows.Forms.RichTextBox Dir_screen;
        private System.Windows.Forms.CheckBox Dir_View;
    }
}

