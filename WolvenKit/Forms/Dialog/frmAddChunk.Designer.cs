using System.ComponentModel;
using System.Windows.Forms;

namespace WolvenKit
{
    partial class frmAddChunk
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
            this.txType = new System.Windows.Forms.ComboBox();
            this.btCancel = new System.Windows.Forms.Button();
            this.btOK = new System.Windows.Forms.Button();
            this.lblType = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txName = new System.Windows.Forms.TextBox();
            this.checkArray = new System.Windows.Forms.CheckBox();
            this.checkHandle = new System.Windows.Forms.CheckBox();
            this.checkSoft = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txTypeFinal = new System.Windows.Forms.TextBox();
            this.flowLayoutVariants = new System.Windows.Forms.FlowLayoutPanel();
            this.checkEnum = new System.Windows.Forms.CheckBox();
            this.flowLayoutVariants.SuspendLayout();
            this.SuspendLayout();
            // 
            // txType
            // 
            this.txType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txType.FormattingEnabled = true;
            this.txType.Location = new System.Drawing.Point(79, 47);
            this.txType.Margin = new System.Windows.Forms.Padding(4);
            this.txType.Name = "txType";
            this.txType.Size = new System.Drawing.Size(299, 24);
            this.txType.TabIndex = 12;
            this.txType.SelectedIndexChanged += new System.EventHandler(this.SelectedTypeChanged);
            // 
            // btCancel
            // 
            this.btCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btCancel.Location = new System.Drawing.Point(16, 159);
            this.btCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btCancel.Name = "btCancel";
            this.btCancel.Size = new System.Drawing.Size(100, 28);
            this.btCancel.TabIndex = 11;
            this.btCancel.Text = "Cancel";
            this.btCancel.UseVisualStyleBackColor = true;
            // 
            // btOK
            // 
            this.btOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btOK.Location = new System.Drawing.Point(279, 159);
            this.btOK.Margin = new System.Windows.Forms.Padding(4);
            this.btOK.Name = "btOK";
            this.btOK.Size = new System.Drawing.Size(100, 28);
            this.btOK.TabIndex = 10;
            this.btOK.Text = "Add";
            this.btOK.UseVisualStyleBackColor = true;
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(8, 50);
            this.lblType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(44, 17);
            this.lblType.TabIndex = 7;
            this.lblType.Text = "Type:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 113);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 17);
            this.label3.TabIndex = 15;
            this.label3.Text = "Name:";
            // 
            // txName
            // 
            this.txName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txName.Location = new System.Drawing.Point(79, 113);
            this.txName.Name = "txName";
            this.txName.Size = new System.Drawing.Size(300, 22);
            this.txName.TabIndex = 16;
            // 
            // checkArray
            // 
            this.checkArray.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkArray.AutoSize = true;
            this.checkArray.Location = new System.Drawing.Point(3, 3);
            this.checkArray.Name = "checkArray";
            this.checkArray.Size = new System.Drawing.Size(64, 21);
            this.checkArray.TabIndex = 17;
            this.checkArray.Text = "Array";
            this.checkArray.UseVisualStyleBackColor = true;
            this.checkArray.CheckedChanged += new System.EventHandler(this.checkArray_CheckedChanged);
            // 
            // checkHandle
            // 
            this.checkHandle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkHandle.AutoSize = true;
            this.checkHandle.Location = new System.Drawing.Point(73, 3);
            this.checkHandle.Name = "checkHandle";
            this.checkHandle.Size = new System.Drawing.Size(75, 21);
            this.checkHandle.TabIndex = 18;
            this.checkHandle.Text = "Handle";
            this.checkHandle.UseVisualStyleBackColor = true;
            this.checkHandle.CheckedChanged += new System.EventHandler(this.checkHandle_CheckedChanged);
            // 
            // checkSoft
            // 
            this.checkSoft.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checkSoft.AutoSize = true;
            this.checkSoft.Location = new System.Drawing.Point(154, 3);
            this.checkSoft.Name = "checkSoft";
            this.checkSoft.Size = new System.Drawing.Size(55, 21);
            this.checkSoft.TabIndex = 19;
            this.checkSoft.Text = "Soft";
            this.checkSoft.UseVisualStyleBackColor = true;
            this.checkSoft.CheckedChanged += new System.EventHandler(this.checkSoft_CheckedChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 81);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 17);
            this.label2.TabIndex = 20;
            this.label2.Text = "Final type:";
            // 
            // txTypeFinal
            // 
            this.txTypeFinal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txTypeFinal.Location = new System.Drawing.Point(79, 78);
            this.txTypeFinal.Name = "txTypeFinal";
            this.txTypeFinal.Size = new System.Drawing.Size(300, 22);
            this.txTypeFinal.TabIndex = 21;
            // 
            // flowLayoutVariants
            // 
            this.flowLayoutVariants.Controls.Add(this.checkArray);
            this.flowLayoutVariants.Controls.Add(this.checkHandle);
            this.flowLayoutVariants.Controls.Add(this.checkSoft);
            this.flowLayoutVariants.Controls.Add(this.checkEnum);
            this.flowLayoutVariants.Location = new System.Drawing.Point(79, 13);
            this.flowLayoutVariants.Name = "flowLayoutVariants";
            this.flowLayoutVariants.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.flowLayoutVariants.Size = new System.Drawing.Size(299, 27);
            this.flowLayoutVariants.TabIndex = 22;
            // 
            // checkEnum
            // 
            this.checkEnum.AutoSize = true;
            this.checkEnum.Location = new System.Drawing.Point(215, 3);
            this.checkEnum.Name = "checkEnum";
            this.checkEnum.Size = new System.Drawing.Size(66, 21);
            this.checkEnum.TabIndex = 20;
            this.checkEnum.Text = "Enum";
            this.checkEnum.UseVisualStyleBackColor = true;
            this.checkEnum.CheckedChanged += new System.EventHandler(this.checkEnum_CheckedChanged);
            // 
            // frmAddChunk
            // 
            this.AcceptButton = this.btOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btCancel;
            this.ClientSize = new System.Drawing.Size(391, 186);
            this.Controls.Add(this.flowLayoutVariants);
            this.Controls.Add(this.txTypeFinal);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txType);
            this.Controls.Add(this.btCancel);
            this.Controls.Add(this.btOK);
            this.Controls.Add(this.lblType);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximumSize = new System.Drawing.Size(1327, 500);
            this.MinimumSize = new System.Drawing.Size(259, 115);
            this.Name = "frmAddChunk";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Class Name";
            this.flowLayoutVariants.ResumeLayout(false);
            this.flowLayoutVariants.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ComboBox txType;
        private Button btCancel;
        private Button btOK;
        private Label lblType;
        private Label label3;
        private TextBox txName;
        private CheckBox checkArray;
        private CheckBox checkHandle;
        private CheckBox checkSoft;
        private Label label2;
        private TextBox txTypeFinal;
        private FlowLayoutPanel flowLayoutVariants;
        private CheckBox checkEnum;
    }
}