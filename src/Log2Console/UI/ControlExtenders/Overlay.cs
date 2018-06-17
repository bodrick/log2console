// Copyright 2007 Herre Kuijpers - <herre@xs4all.nl>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Log2Console.UI.ControlExtenders
{
    /// <summary>
    ///     this is the overlay preview control
    /// </summary>
    internal sealed class Overlay : Form
    {
        /// <summary>
        ///     Required designer variable.
        /// </summary>
        private readonly IContainer components = null;

        // override Dockstate.
        public new DockStyle Dock;
        public Control DockHostControl;

        public Overlay()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // Overlay
            // 
            BackColor = SystemColors.ActiveCaption;
            ControlBox = false;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Overlay";
            Opacity = 0.3;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Text = "Overlay";
            ResumeLayout(false);
        }
    }
}