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
            this.listBox3 = new System.Windows.Forms.ListBox();
            this.f_load = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.Out_Type = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.T_Info = new System.Windows.Forms.CheckBox();
            this.os = new System.Windows.Forms.ListBox();
            this.ol = new System.Windows.Forms.ListBox();
            this.od = new System.Windows.Forms.ListBox();
            this.of = new System.Windows.Forms.ListBox();
            this.otrack = new System.Windows.Forms.ListBox();
            this.outbox = new System.Windows.Forms.GroupBox();
            this.Source = new System.Windows.Forms.Label();
            this.Output = new System.Windows.Forms.Label();
            this.inbox = new System.Windows.Forms.GroupBox();
            this.strack = new System.Windows.Forms.ListBox();
            this.ss = new System.Windows.Forms.ListBox();
            this.sl = new System.Windows.Forms.ListBox();
            this.Drag_pic = new System.Windows.Forms.PictureBox();
            this.CustomV2headers = new System.Windows.Forms.CheckBox();
            this.V2_Len = new System.Windows.Forms.NumericUpDown();
            this.V2H = new System.Windows.Forms.Button();
            this.Warn = new System.Windows.Forms.Label();
            this.outbox.SuspendLayout();
            this.inbox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.V2_Len)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(369, 63);
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
            this.label1.Location = new System.Drawing.Point(12, 10);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "label1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 34);
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
            this.listBox3.Location = new System.Drawing.Point(761, 72);
            this.listBox3.Name = "listBox3";
            this.listBox3.Size = new System.Drawing.Size(969, 1135);
            this.listBox3.TabIndex = 10;
            // 
            // f_load
            // 
            this.f_load.AutoSize = true;
            this.f_load.Location = new System.Drawing.Point(17, 105);
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
            this.label7.Location = new System.Drawing.Point(756, 40);
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
            this.Out_Type.Location = new System.Drawing.Point(189, 66);
            this.Out_Type.Name = "Out_Type";
            this.Out_Type.Size = new System.Drawing.Size(174, 33);
            this.Out_Type.TabIndex = 14;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 69);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(171, 25);
            this.label8.TabIndex = 15;
            this.label8.Text = "Output File Type";
            // 
            // T_Info
            // 
            this.T_Info.AutoSize = true;
            this.T_Info.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.T_Info.Location = new System.Drawing.Point(514, 128);
            this.T_Info.Name = "T_Info";
            this.T_Info.Size = new System.Drawing.Size(198, 29);
            this.T_Info.TabIndex = 16;
            this.T_Info.Text = "Show Track Info";
            this.T_Info.UseVisualStyleBackColor = true;
            this.T_Info.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // os
            // 
            this.os.BackColor = System.Drawing.Color.Gainsboro;
            this.os.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.os.FormattingEnabled = true;
            this.os.ItemHeight = 25;
            this.os.Location = new System.Drawing.Point(55, 30);
            this.os.Name = "os";
            this.os.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.os.Size = new System.Drawing.Size(100, 1002);
            this.os.TabIndex = 23;
            // 
            // ol
            // 
            this.ol.BackColor = System.Drawing.Color.Gainsboro;
            this.ol.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ol.FormattingEnabled = true;
            this.ol.ItemHeight = 25;
            this.ol.Location = new System.Drawing.Point(149, 30);
            this.ol.Name = "ol";
            this.ol.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.ol.Size = new System.Drawing.Size(73, 1002);
            this.ol.TabIndex = 24;
            // 
            // od
            // 
            this.od.BackColor = System.Drawing.Color.Gainsboro;
            this.od.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.od.FormattingEnabled = true;
            this.od.ItemHeight = 25;
            this.od.Location = new System.Drawing.Point(217, 30);
            this.od.Name = "od";
            this.od.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.od.Size = new System.Drawing.Size(62, 1002);
            this.od.TabIndex = 25;
            // 
            // of
            // 
            this.of.BackColor = System.Drawing.Color.Gainsboro;
            this.of.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.of.FormattingEnabled = true;
            this.of.ItemHeight = 25;
            this.of.Location = new System.Drawing.Point(275, 30);
            this.of.Name = "of";
            this.of.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.of.Size = new System.Drawing.Size(145, 1002);
            this.of.TabIndex = 26;
            // 
            // otrack
            // 
            this.otrack.BackColor = System.Drawing.Color.Gainsboro;
            this.otrack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.otrack.ForeColor = System.Drawing.Color.Blue;
            this.otrack.FormattingEnabled = true;
            this.otrack.ItemHeight = 25;
            this.otrack.Location = new System.Drawing.Point(6, 30);
            this.otrack.Name = "otrack";
            this.otrack.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.otrack.Size = new System.Drawing.Size(53, 1002);
            this.otrack.TabIndex = 29;
            // 
            // outbox
            // 
            this.outbox.BackColor = System.Drawing.Color.Gainsboro;
            this.outbox.Controls.Add(this.otrack);
            this.outbox.Controls.Add(this.of);
            this.outbox.Controls.Add(this.od);
            this.outbox.Controls.Add(this.ol);
            this.outbox.Controls.Add(this.os);
            this.outbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.outbox.ForeColor = System.Drawing.Color.Indigo;
            this.outbox.Location = new System.Drawing.Point(292, 160);
            this.outbox.Name = "outbox";
            this.outbox.Size = new System.Drawing.Size(437, 1047);
            this.outbox.TabIndex = 22;
            this.outbox.TabStop = false;
            this.outbox.Text = "Track / Size / Start / Diff  /   Format";
            // 
            // Source
            // 
            this.Source.AutoSize = true;
            this.Source.Location = new System.Drawing.Point(17, 132);
            this.Source.Name = "Source";
            this.Source.Size = new System.Drawing.Size(121, 25);
            this.Source.TabIndex = 23;
            this.Source.Text = "Source File";
            // 
            // Output
            // 
            this.Output.AutoSize = true;
            this.Output.Location = new System.Drawing.Point(293, 132);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(76, 25);
            this.Output.TabIndex = 24;
            this.Output.Text = "Output";
            // 
            // inbox
            // 
            this.inbox.BackColor = System.Drawing.Color.Gainsboro;
            this.inbox.Controls.Add(this.strack);
            this.inbox.Controls.Add(this.ss);
            this.inbox.Controls.Add(this.sl);
            this.inbox.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.inbox.ForeColor = System.Drawing.Color.Indigo;
            this.inbox.Location = new System.Drawing.Point(17, 160);
            this.inbox.Name = "inbox";
            this.inbox.Size = new System.Drawing.Size(235, 1047);
            this.inbox.TabIndex = 25;
            this.inbox.TabStop = false;
            this.inbox.Text = "Track / Size / Start";
            // 
            // strack
            // 
            this.strack.BackColor = System.Drawing.Color.Gainsboro;
            this.strack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.strack.ForeColor = System.Drawing.Color.Blue;
            this.strack.FormattingEnabled = true;
            this.strack.ItemHeight = 25;
            this.strack.Location = new System.Drawing.Point(11, 30);
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
            this.Drag_pic.Location = new System.Drawing.Point(50, 211);
            this.Drag_pic.Name = "Drag_pic";
            this.Drag_pic.Size = new System.Drawing.Size(648, 964);
            this.Drag_pic.TabIndex = 27;
            this.Drag_pic.TabStop = false;
            // 
            // CustomV2headers
            // 
            this.CustomV2headers.AutoSize = true;
            this.CustomV2headers.Location = new System.Drawing.Point(22, 1243);
            this.CustomV2headers.Name = "CustomV2headers";
            this.CustomV2headers.Size = new System.Drawing.Size(396, 29);
            this.CustomV2headers.TabIndex = 28;
            this.CustomV2headers.Text = "Use V-Max v2 Custom header length";
            this.CustomV2headers.UseVisualStyleBackColor = true;
            this.CustomV2headers.CheckedChanged += new System.EventHandler(this.CustomV2headers_CheckedChanged);
            // 
            // V2_Len
            // 
            this.V2_Len.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.V2_Len.Location = new System.Drawing.Point(424, 1241);
            this.V2_Len.Maximum = new decimal(new int[] {
            36,
            0,
            0,
            0});
            this.V2_Len.Minimum = new decimal(new int[] {
            8,
            0,
            0,
            0});
            this.V2_Len.Name = "V2_Len";
            this.V2_Len.Size = new System.Drawing.Size(120, 31);
            this.V2_Len.TabIndex = 29;
            this.V2_Len.Value = new decimal(new int[] {
            18,
            0,
            0,
            0});
            // 
            // V2H
            // 
            this.V2H.Location = new System.Drawing.Point(583, 1237);
            this.V2H.Name = "V2H";
            this.V2H.Size = new System.Drawing.Size(100, 44);
            this.V2H.TabIndex = 30;
            this.V2H.Text = "Apply";
            this.V2H.UseVisualStyleBackColor = true;
            this.V2H.Click += new System.EventHandler(this.V2H_Click);
            // 
            // Warn
            // 
            this.Warn.AutoSize = true;
            this.Warn.Location = new System.Drawing.Point(26, 1213);
            this.Warn.Name = "Warn";
            this.Warn.Size = new System.Drawing.Size(672, 25);
            this.Warn.TabIndex = 31;
            this.Warn.Text = "Advanced users only! Setting value too high could corrupt the output!";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(750, 1284);
            this.Controls.Add(this.Warn);
            this.Controls.Add(this.V2H);
            this.Controls.Add(this.V2_Len);
            this.Controls.Add(this.CustomV2headers);
            this.Controls.Add(this.Drag_pic);
            this.Controls.Add(this.inbox);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.Source);
            this.Controls.Add(this.outbox);
            this.Controls.Add(this.T_Info);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.Out_Type);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.f_load);
            this.Controls.Add(this.listBox3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "V-Max Sync Tool";
            this.outbox.ResumeLayout(false);
            this.inbox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.Drag_pic)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.V2_Len)).EndInit();
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
        private System.Windows.Forms.ListBox os;
        private System.Windows.Forms.ListBox ol;
        private System.Windows.Forms.ListBox od;
        private System.Windows.Forms.ListBox of;
        private System.Windows.Forms.ListBox otrack;
        private System.Windows.Forms.GroupBox outbox;
        private System.Windows.Forms.Label Source;
        private System.Windows.Forms.Label Output;
        private System.Windows.Forms.GroupBox inbox;
        private System.Windows.Forms.ListBox strack;
        private System.Windows.Forms.ListBox ss;
        private System.Windows.Forms.ListBox sl;
        private System.Windows.Forms.PictureBox Drag_pic;
        private System.Windows.Forms.CheckBox CustomV2headers;
        private System.Windows.Forms.NumericUpDown V2_Len;
        private System.Windows.Forms.Button V2H;
        private System.Windows.Forms.Label Warn;
    }
}

