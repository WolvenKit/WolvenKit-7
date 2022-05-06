namespace WolvenKit.Forms.Editors
{
    partial class frmSrtEditor
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSave = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExport = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonImport = new System.Windows.Forms.ToolStripButton();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(0, 34);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(1065, 518);
            this.textBox1.TabIndex = 0;
            // 
            // toolStrip
            // 
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton2,
            this.toolStripButtonSave,
            this.toolStripButtonExport,
            this.toolStripButtonImport,
            this.toolStripLabel1});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(1067, 31);
            this.toolStrip.TabIndex = 2;
            this.toolStrip.Text = "toolStrip1";
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = global::WolvenKit.Properties.Resources.OpenFile_16x;
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(94, 28);
            this.toolStripButton2.Text = "Load SRT";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
            // 
            // toolStripButtonSave
            // 
            this.toolStripButtonSave.Image = global::WolvenKit.Properties.Resources.Save_16x;
            this.toolStripButtonSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSave.Name = "toolStripButtonSave";
            this.toolStripButtonSave.Size = new System.Drawing.Size(92, 28);
            this.toolStripButtonSave.Text = "Save SRT";
            this.toolStripButtonSave.Click += new System.EventHandler(this.toolStripButtonSave_Click);
            // 
            // toolStripButtonExport
            // 
            this.toolStripButtonExport.Image = global::WolvenKit.Properties.Resources.Output_16x;
            this.toolStripButtonExport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExport.Name = "toolStripButtonExport";
            this.toolStripButtonExport.Size = new System.Drawing.Size(115, 28);
            this.toolStripButtonExport.Text = "Export JSON";
            this.toolStripButtonExport.Click += new System.EventHandler(this.toolStripButtonExport_Click);
            // 
            // toolStripButtonImport
            // 
            this.toolStripButtonImport.Image = global::WolvenKit.Properties.Resources.bug;
            this.toolStripButtonImport.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonImport.Name = "toolStripButtonImport";
            this.toolStripButtonImport.Size = new System.Drawing.Size(117, 28);
            this.toolStripButtonImport.Text = "Import JSON";
            this.toolStripButtonImport.Click += new System.EventHandler(this.toolStripButtonImport_Click);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Image = global::WolvenKit.Properties.Resources.Test_16x;
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(152, 28);
            this.toolStripLabel1.Text = "Compare .srt bytes";
            this.toolStripLabel1.Click += new System.EventHandler(this.toolStripLabel1_Click);
            // 
            // frmSrtEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1067, 554);
            this.Controls.Add(this.toolStrip);
            this.Controls.Add(this.textBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "frmSrtEditor";
            this.Text = "SRT Editor";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.ToolStripButton toolStripButtonSave;
        private System.Windows.Forms.ToolStripButton toolStripButtonExport;
        private System.Windows.Forms.ToolStripButton toolStripButtonImport;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
    }
}
