﻿namespace GKSandbox
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }
        
        private void InitializeComponent()
        {
            this.culturePicker1 = new GKIntl.CulturePicker();
            this.optionsPicker1 = new BSLib.Controls.OptionsPicker();
            this.SuspendLayout();
            // 
            // culturePicker1
            // 
            this.culturePicker1.AnchorSize = new System.Drawing.Size(236, 23);
            this.culturePicker1.BackColor = System.Drawing.Color.White;
            this.culturePicker1.Location = new System.Drawing.Point(316, 16);
            this.culturePicker1.Name = "culturePicker1";
            this.culturePicker1.Size = new System.Drawing.Size(236, 23);
            this.culturePicker1.TabIndex = 2;
            // 
            // optionsPicker1
            // 
            this.optionsPicker1.AnchorSize = new System.Drawing.Size(236, 25);
            this.optionsPicker1.BackColor = System.Drawing.Color.White;
            this.optionsPicker1.Location = new System.Drawing.Point(315, 65);
            this.optionsPicker1.Name = "optionsPicker1";
            this.optionsPicker1.Size = new System.Drawing.Size(236, 25);
            this.optionsPicker1.TabIndex = 3;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(766, 477);
            this.Controls.Add(this.optionsPicker1);
            this.Controls.Add(this.culturePicker1);
            this.Name = "MainForm";
            this.Text = "GKSandbox";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainFormFormClosed);
            this.ResumeLayout(false);

        }
        private BSLib.Controls.OptionsPicker optionsPicker1;
        private GKIntl.CulturePicker culturePicker1;
    }
}
