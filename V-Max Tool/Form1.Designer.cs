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
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.sl = new System.Windows.Forms.ListBox();
            this.os = new System.Windows.Forms.ListBox();
            this.ol = new System.Windows.Forms.ListBox();
            this.od = new System.Windows.Forms.ListBox();
            this.of = new System.Windows.Forms.ListBox();
            this.ss = new System.Windows.Forms.ListBox();
            this.strack = new System.Windows.Forms.ListBox();
            this.otrack = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.Source = new System.Windows.Forms.Label();
            this.Output = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(369, 3);
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
            this.label1.Location = new System.Drawing.Point(12, 1173);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 25);
            this.label1.TabIndex = 1;
            this.label1.Text = "label1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 1198);
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
            this.f_load.Location = new System.Drawing.Point(17, 49);
            this.f_load.Name = "f_load";
            this.f_load.Size = new System.Drawing.Size(206, 29);
            this.f_load.TabIndex = 12;
            this.f_load.Text = "Fix Loader Track";
            this.f_load.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(1092, 40);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(199, 29);
            this.label7.TabIndex = 13;
            this.label7.Text = "Track Information";
            // 
            // Out_Type
            // 
            this.Out_Type.AllowDrop = true;
            this.Out_Type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Out_Type.FormattingEnabled = true;
            this.Out_Type.Location = new System.Drawing.Point(189, 6);
            this.Out_Type.Name = "Out_Type";
            this.Out_Type.Size = new System.Drawing.Size(174, 33);
            this.Out_Type.TabIndex = 14;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 9);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(171, 25);
            this.label8.TabIndex = 15;
            this.label8.Text = "Output File Type";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.checkBox1.Location = new System.Drawing.Point(531, 9);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(198, 29);
            this.checkBox1.TabIndex = 16;
            this.checkBox1.Text = "Show Track Info";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.CheckBox1_CheckedChanged);
            // 
            // sl
            // 
            this.sl.BackColor = System.Drawing.Color.Silver;
            this.sl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.sl.FormattingEnabled = true;
            this.sl.ItemHeight = 25;
            this.sl.Location = new System.Drawing.Point(65, 30);
            this.sl.Name = "sl";
            this.sl.Size = new System.Drawing.Size(90, 1002);
            this.sl.TabIndex = 22;
            // 
            // os
            // 
            this.os.BackColor = System.Drawing.Color.Silver;
            this.os.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.os.FormattingEnabled = true;
            this.os.ItemHeight = 25;
            this.os.Location = new System.Drawing.Point(335, 30);
            this.os.Name = "os";
            this.os.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.os.Size = new System.Drawing.Size(100, 1002);
            this.os.TabIndex = 23;
            // 
            // ol
            // 
            this.ol.BackColor = System.Drawing.Color.Silver;
            this.ol.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ol.FormattingEnabled = true;
            this.ol.ItemHeight = 25;
            this.ol.Location = new System.Drawing.Point(429, 30);
            this.ol.Name = "ol";
            this.ol.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.ol.Size = new System.Drawing.Size(73, 1002);
            this.ol.TabIndex = 24;
            // 
            // od
            // 
            this.od.BackColor = System.Drawing.Color.Silver;
            this.od.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.od.FormattingEnabled = true;
            this.od.ItemHeight = 25;
            this.od.Location = new System.Drawing.Point(497, 30);
            this.od.Name = "od";
            this.od.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.od.Size = new System.Drawing.Size(62, 1002);
            this.od.TabIndex = 25;
            // 
            // of
            // 
            this.of.BackColor = System.Drawing.Color.Silver;
            this.of.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.of.FormattingEnabled = true;
            this.of.ItemHeight = 25;
            this.of.Location = new System.Drawing.Point(555, 30);
            this.of.Name = "of";
            this.of.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.of.Size = new System.Drawing.Size(145, 1002);
            this.of.TabIndex = 26;
            // 
            // ss
            // 
            this.ss.BackColor = System.Drawing.Color.Silver;
            this.ss.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ss.FormattingEnabled = true;
            this.ss.ItemHeight = 25;
            this.ss.Location = new System.Drawing.Point(149, 30);
            this.ss.Name = "ss";
            this.ss.Size = new System.Drawing.Size(82, 1002);
            this.ss.TabIndex = 27;
            // 
            // strack
            // 
            this.strack.BackColor = System.Drawing.Color.Silver;
            this.strack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.strack.ForeColor = System.Drawing.Color.Blue;
            this.strack.FormattingEnabled = true;
            this.strack.ItemHeight = 25;
            this.strack.Location = new System.Drawing.Point(17, 30);
            this.strack.Name = "strack";
            this.strack.Size = new System.Drawing.Size(53, 1002);
            this.strack.TabIndex = 28;
            // 
            // otrack
            // 
            this.otrack.BackColor = System.Drawing.Color.Silver;
            this.otrack.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.otrack.ForeColor = System.Drawing.Color.Blue;
            this.otrack.FormattingEnabled = true;
            this.otrack.ItemHeight = 25;
            this.otrack.Location = new System.Drawing.Point(286, 30);
            this.otrack.Name = "otrack";
            this.otrack.Size = new System.Drawing.Size(53, 1002);
            this.otrack.TabIndex = 29;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.Silver;
            this.groupBox1.Controls.Add(this.otrack);
            this.groupBox1.Controls.Add(this.strack);
            this.groupBox1.Controls.Add(this.ss);
            this.groupBox1.Controls.Add(this.of);
            this.groupBox1.Controls.Add(this.od);
            this.groupBox1.Controls.Add(this.ol);
            this.groupBox1.Controls.Add(this.os);
            this.groupBox1.Controls.Add(this.sl);
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.groupBox1.ForeColor = System.Drawing.Color.Indigo;
            this.groupBox1.Location = new System.Drawing.Point(12, 109);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(717, 1047);
            this.groupBox1.TabIndex = 22;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Track / Size  /  Start            Track /  Size /   Start /   Diff  /   Format";
            // 
            // Source
            // 
            this.Source.AutoSize = true;
            this.Source.Location = new System.Drawing.Point(24, 81);
            this.Source.Name = "Source";
            this.Source.Size = new System.Drawing.Size(121, 25);
            this.Source.TabIndex = 23;
            this.Source.Text = "Source File";
            // 
            // Output
            // 
            this.Output.AutoSize = true;
            this.Output.Location = new System.Drawing.Point(458, 81);
            this.Output.Name = "Output";
            this.Output.Size = new System.Drawing.Size(76, 25);
            this.Output.TabIndex = 24;
            this.Output.Text = "Output";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(743, 1228);
            this.Controls.Add(this.Output);
            this.Controls.Add(this.Source);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.checkBox1);
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
            this.groupBox1.ResumeLayout(false);
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
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.ListBox sl;
        private System.Windows.Forms.ListBox os;
        private System.Windows.Forms.ListBox ol;
        private System.Windows.Forms.ListBox od;
        private System.Windows.Forms.ListBox of;
        private System.Windows.Forms.ListBox ss;
        private System.Windows.Forms.ListBox strack;
        private System.Windows.Forms.ListBox otrack;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label Source;
        private System.Windows.Forms.Label Output;
    }
}

