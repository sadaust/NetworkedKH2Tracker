using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Linq;
using System.IO;
using Microsoft.Win32;
using System.Drawing;
using System.Windows.Documents;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Forms;

namespace KhTracker
{
    partial class JoinMultiplayerForm
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
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.JoinButton = new System.Windows.Forms.Button();
            this.CancelJoinButton = new System.Windows.Forms.Button();
            this.NameLabel = new System.Windows.Forms.Label();
            this.NameEntry = new System.Windows.Forms.TextBox();
            this.IPEntry = new System.Windows.Forms.TextBox();
            this.IPLabel = new System.Windows.Forms.Label();
            this.PortLabel = new System.Windows.Forms.Label();
            this.PortEntry = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.PortEntry)).BeginInit();
            this.SuspendLayout();
            // 
            // JoinButton
            // 
            this.JoinButton.Location = new System.Drawing.Point(15, 91);
            this.JoinButton.Name = "JoinButton";
            this.JoinButton.Size = new System.Drawing.Size(75, 23);
            this.JoinButton.TabIndex = 0;
            this.JoinButton.Text = "Join";
            this.JoinButton.UseVisualStyleBackColor = true;
            this.JoinButton.Click += new System.EventHandler(this.Join_Click);
            // 
            // CancelJoinButton
            // 
            this.CancelJoinButton.Location = new System.Drawing.Point(190, 91);
            this.CancelJoinButton.Name = "CancelJoinButton";
            this.CancelJoinButton.Size = new System.Drawing.Size(75, 23);
            this.CancelJoinButton.TabIndex = 1;
            this.CancelJoinButton.Text = "Cancel";
            this.CancelJoinButton.UseVisualStyleBackColor = true;
            this.CancelJoinButton.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(12, 15);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(70, 13);
            this.NameLabel.TabIndex = 2;
            this.NameLabel.Text = "Player Name:";
            // 
            // NameEntry
            // 
            this.NameEntry.Enabled = false;
            this.NameEntry.Location = new System.Drawing.Point(93, 12);
            this.NameEntry.Name = "NameEntry";
            this.NameEntry.Size = new System.Drawing.Size(172, 20);
            this.NameEntry.TabIndex = 3;
            this.NameEntry.Text = "Player";
            // 
            // IPEntry
            // 
            this.IPEntry.Location = new System.Drawing.Point(93, 38);
            this.IPEntry.Name = "IPEntry";
            this.IPEntry.Size = new System.Drawing.Size(172, 20);
            this.IPEntry.TabIndex = 4;
            this.IPEntry.Text = global::KhTracker.Properties.Settings.Default.ServerIP;
            // 
            // IPLabel
            // 
            this.IPLabel.AutoSize = true;
            this.IPLabel.Location = new System.Drawing.Point(28, 38);
            this.IPLabel.Name = "IPLabel";
            this.IPLabel.Size = new System.Drawing.Size(54, 13);
            this.IPLabel.TabIndex = 5;
            this.IPLabel.Text = "Server IP:";
            // 
            // PortLabel
            // 
            this.PortLabel.AutoSize = true;
            this.PortLabel.Location = new System.Drawing.Point(19, 65);
            this.PortLabel.Name = "PortLabel";
            this.PortLabel.Size = new System.Drawing.Size(63, 13);
            this.PortLabel.TabIndex = 6;
            this.PortLabel.Text = "Server Port:";
            // 
            // PortEntry
            // 
            this.PortEntry.AllowDrop = true;
            this.PortEntry.DataBindings.Add(new System.Windows.Forms.Binding("Value", global::KhTracker.Properties.Settings.Default, "ServerPort", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.PortEntry.Location = new System.Drawing.Point(93, 63);
            this.PortEntry.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.PortEntry.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.PortEntry.Name = "PortEntry";
            this.PortEntry.Size = new System.Drawing.Size(172, 20);
            this.PortEntry.TabIndex = 7;
            this.PortEntry.Value = global::KhTracker.Properties.Settings.Default.ServerPort;
            // 
            // JoinMultiplayerForm
            // 
            this.AcceptButton = this.JoinButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.CancelButton = this.CancelJoinButton;
            this.ClientSize = new System.Drawing.Size(276, 119);
            this.Controls.Add(this.PortEntry);
            this.Controls.Add(this.PortLabel);
            this.Controls.Add(this.IPLabel);
            this.Controls.Add(this.IPEntry);
            this.Controls.Add(this.NameEntry);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.CancelJoinButton);
            this.Controls.Add(this.JoinButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "JoinMultiplayerForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Join";
            this.Load += new System.EventHandler(this.JoinMultiplayerForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.PortEntry)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button JoinButton;
        private System.Windows.Forms.Button CancelJoinButton;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.TextBox NameEntry;
        private System.Windows.Forms.TextBox IPEntry;
        private System.Windows.Forms.Label IPLabel;
        private System.Windows.Forms.Label PortLabel;
        private System.Windows.Forms.NumericUpDown PortEntry;
    }
}