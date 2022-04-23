
namespace Escalator
{
    partial class Home
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Home));
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.btnUploadOrderList = new System.Windows.Forms.Button();
            this.radioStairs = new System.Windows.Forms.RadioButton();
            this.radioRails = new System.Windows.Forms.RadioButton();
            this.btnCreateVerifyList = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblError = new System.Windows.Forms.Label();
            this.checklistDates = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblUpload = new System.Windows.Forms.Label();
            this.btnOpenRules = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // btnUploadOrderList
            // 
            this.btnUploadOrderList.Location = new System.Drawing.Point(12, 12);
            this.btnUploadOrderList.Name = "btnUploadOrderList";
            this.btnUploadOrderList.Size = new System.Drawing.Size(113, 23);
            this.btnUploadOrderList.TabIndex = 0;
            this.btnUploadOrderList.Text = "Upload Order List";
            this.btnUploadOrderList.UseVisualStyleBackColor = true;
            this.btnUploadOrderList.Click += new System.EventHandler(this.btnUploadOrderList_Click);
            // 
            // radioStairs
            // 
            this.radioStairs.AutoSize = true;
            this.radioStairs.Location = new System.Drawing.Point(6, 22);
            this.radioStairs.Name = "radioStairs";
            this.radioStairs.Size = new System.Drawing.Size(53, 19);
            this.radioStairs.TabIndex = 1;
            this.radioStairs.TabStop = true;
            this.radioStairs.Text = "Stairs";
            this.radioStairs.UseVisualStyleBackColor = true;
            // 
            // radioRails
            // 
            this.radioRails.AutoSize = true;
            this.radioRails.Location = new System.Drawing.Point(6, 47);
            this.radioRails.Name = "radioRails";
            this.radioRails.Size = new System.Drawing.Size(49, 19);
            this.radioRails.TabIndex = 2;
            this.radioRails.TabStop = true;
            this.radioRails.Text = "Rails";
            this.radioRails.UseVisualStyleBackColor = true;
            // 
            // btnCreateVerifyList
            // 
            this.btnCreateVerifyList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreateVerifyList.Location = new System.Drawing.Point(423, 205);
            this.btnCreateVerifyList.Name = "btnCreateVerifyList";
            this.btnCreateVerifyList.Size = new System.Drawing.Size(120, 23);
            this.btnCreateVerifyList.TabIndex = 3;
            this.btnCreateVerifyList.Text = "Create Verify List";
            this.btnCreateVerifyList.UseVisualStyleBackColor = true;
            this.btnCreateVerifyList.Click += new System.EventHandler(this.btnCreateVerifyList_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioStairs);
            this.groupBox1.Controls.Add(this.radioRails);
            this.groupBox1.Location = new System.Drawing.Point(12, 53);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(113, 116);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Order Type *";
            // 
            // lblError
            // 
            this.lblError.AutoSize = true;
            this.lblError.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblError.ForeColor = System.Drawing.Color.Red;
            this.lblError.Location = new System.Drawing.Point(18, 209);
            this.lblError.Name = "lblError";
            this.lblError.Size = new System.Drawing.Size(0, 15);
            this.lblError.TabIndex = 5;
            // 
            // checklistDates
            // 
            this.checklistDates.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.checklistDates.FormattingEnabled = true;
            this.checklistDates.Location = new System.Drawing.Point(147, 75);
            this.checklistDates.Name = "checklistDates";
            this.checklistDates.Size = new System.Drawing.Size(396, 94);
            this.checklistDates.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(147, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "Requested Dates *";
            // 
            // lblUpload
            // 
            this.lblUpload.AutoSize = true;
            this.lblUpload.Location = new System.Drawing.Point(147, 16);
            this.lblUpload.Name = "lblUpload";
            this.lblUpload.Size = new System.Drawing.Size(0, 15);
            this.lblUpload.TabIndex = 8;
            // 
            // btnOpenRules
            // 
            this.btnOpenRules.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenRules.Location = new System.Drawing.Point(423, 12);
            this.btnOpenRules.Name = "btnOpenRules";
            this.btnOpenRules.Size = new System.Drawing.Size(120, 23);
            this.btnOpenRules.TabIndex = 9;
            this.btnOpenRules.Text = "View Rules...";
            this.btnOpenRules.UseVisualStyleBackColor = true;
            this.btnOpenRules.Click += new System.EventHandler(this.btnOpenRules_Click);
            // 
            // Home
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(555, 240);
            this.Controls.Add(this.btnOpenRules);
            this.Controls.Add(this.lblUpload);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.checklistDates);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnCreateVerifyList);
            this.Controls.Add(this.btnUploadOrderList);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Home";
            this.Text = "Escalator";
            this.Load += new System.EventHandler(this.Home_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button btnUploadOrderList;
        private System.Windows.Forms.RadioButton radioStairs;
        private System.Windows.Forms.RadioButton radioRails;
        private System.Windows.Forms.Button btnCreateVerifyList;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.CheckedListBox checklistDates;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblUpload;
        private System.Windows.Forms.Button btnOpenRules;
    }
}

