using System.ComponentModel;
using System.Windows.Forms;

namespace WolvenKit
{
    partial class frmModSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.pgModMain = new System.Windows.Forms.PropertyGrid();
            this.settingpages = new System.Windows.Forms.TabControl();
            this.mod_details = new System.Windows.Forms.TabPage();
            this.btSave = new System.Windows.Forms.Button();
            this.btCancel = new System.Windows.Forms.Button();
            this.settingpages.SuspendLayout();
            this.mod_details.SuspendLayout();
            this.SuspendLayout();
            // 
            // pgModMain
            // 
            this.pgModMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pgModMain.Location = new System.Drawing.Point(3, 4);
            this.pgModMain.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pgModMain.Name = "pgModMain";
            this.pgModMain.Size = new System.Drawing.Size(854, 557);
            this.pgModMain.TabIndex = 8;
            // 
            // settingpages
            // 
            this.settingpages.Controls.Add(this.mod_details);
            this.settingpages.Dock = System.Windows.Forms.DockStyle.Top;
            this.settingpages.Location = new System.Drawing.Point(0, 0);
            this.settingpages.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.settingpages.Name = "settingpages";
            this.settingpages.SelectedIndex = 0;
            this.settingpages.Size = new System.Drawing.Size(868, 598);
            this.settingpages.TabIndex = 9;
            // 
            // mod_details
            // 
            this.mod_details.Controls.Add(this.pgModMain);
            this.mod_details.Location = new System.Drawing.Point(4, 29);
            this.mod_details.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.mod_details.Name = "mod_details";
            this.mod_details.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.mod_details.Size = new System.Drawing.Size(860, 565);
            this.mod_details.TabIndex = 0;
            this.mod_details.Text = "Mod details";
            this.mod_details.UseVisualStyleBackColor = true;
            // 
            // btSave
            // 
            this.btSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btSave.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btSave.Location = new System.Drawing.Point(738, 606);
            this.btSave.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btSave.Name = "btSave";
            this.btSave.Size = new System.Drawing.Size(112, 35);
            this.btSave.TabIndex = 4;
            this.btSave.Text = "Save";
            this.btSave.UseVisualStyleBackColor = true;
            this.btSave.Click += new System.EventHandler(this.btSave_Click);
            // 
            // btCancel
            // 
            this.btCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancel.Location = new System.Drawing.Point(22, 606);
            this.btCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(112, 35);
            this.btCancel.TabIndex = 5;
            this.btCancel.Text = "Cancel";
            this.btCancel.UseVisualStyleBackColor = true;
            // 
            // frmModSettings
            // 
            this.AcceptButton = this.btSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btCancel;
            this.ClientSize = new System.Drawing.Size(868, 659);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btSave);
            this.Controls.Add(this.settingpages);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(440, 177);
            this.Name = "frmModSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Mod Settings";
            this.settingpages.ResumeLayout(false);
            this.mod_details.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private PropertyGrid pgModMain;
        private TabControl settingpages;
        private TabPage mod_details;
        private Button btSave;
        private Button btCancel;
    }
}