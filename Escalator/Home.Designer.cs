
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
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button1 = new System.Windows.Forms.Button();
            this.inputColLot = new System.Windows.Forms.NumericUpDown();
            this.inputColSubdivision = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.inputColLot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputColSubdivision)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 81);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(173, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "Import Verify List";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // inputColLot
            // 
            this.inputColLot.Location = new System.Drawing.Point(133, 14);
            this.inputColLot.Name = "inputColLot";
            this.inputColLot.Size = new System.Drawing.Size(52, 23);
            this.inputColLot.TabIndex = 1;
            this.inputColLot.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // inputColSubdivision
            // 
            this.inputColSubdivision.Location = new System.Drawing.Point(133, 43);
            this.inputColSubdivision.Name = "inputColSubdivision";
            this.inputColSubdivision.Size = new System.Drawing.Size(52, 23);
            this.inputColSubdivision.TabIndex = 2;
            this.inputColSubdivision.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "Lot Column";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(114, 15);
            this.label2.TabIndex = 4;
            this.label2.Text = "Subdivision Column";
            // 
            // Home
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(203, 118);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.inputColSubdivision);
            this.Controls.Add(this.inputColLot);
            this.Controls.Add(this.button1);
            this.Name = "Home";
            this.Text = "Escalator";
            this.Load += new System.EventHandler(this.Home_Load);
            ((System.ComponentModel.ISupportInitialize)(this.inputColLot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.inputColSubdivision)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.NumericUpDown inputColLot;
        private System.Windows.Forms.NumericUpDown inputColSubdivision;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}

