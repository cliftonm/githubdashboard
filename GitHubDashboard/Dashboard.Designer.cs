namespace GitHubDashboard
{
	partial class Dashboard
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
			this.pnlEvents = new GitHubDashboard.OwnerDrawPanel();
			this.pnlDescriptions = new GitHubDashboard.OwnerDrawPanel();
			this.pnlLanguages = new GitHubDashboard.OwnerDrawPanel();
			this.SuspendLayout();
			// 
			// pnlEvents
			// 
			this.pnlEvents.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.pnlEvents.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.pnlEvents.Location = new System.Drawing.Point(17, 31);
			this.pnlEvents.Name = "pnlEvents";
			this.pnlEvents.Size = new System.Drawing.Size(311, 307);
			this.pnlEvents.TabIndex = 0;
			// 
			// pnlDescriptions
			// 
			this.pnlDescriptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.pnlDescriptions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.pnlDescriptions.Location = new System.Drawing.Point(350, 31);
			this.pnlDescriptions.Name = "pnlDescriptions";
			this.pnlDescriptions.Size = new System.Drawing.Size(311, 307);
			this.pnlDescriptions.TabIndex = 1;
			// 
			// pnlLanguages
			// 
			this.pnlLanguages.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.pnlLanguages.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.pnlLanguages.Location = new System.Drawing.Point(681, 31);
			this.pnlLanguages.Name = "pnlLanguages";
			this.pnlLanguages.Size = new System.Drawing.Size(311, 307);
			this.pnlLanguages.TabIndex = 2;
			// 
			// Dashboard
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(1051, 352);
			this.Controls.Add(this.pnlLanguages);
			this.Controls.Add(this.pnlDescriptions);
			this.Controls.Add(this.pnlEvents);
			this.Name = "Dashboard";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "GitHub Dashboard";
			this.ResumeLayout(false);

		}

		#endregion

		private OwnerDrawPanel pnlEvents;
		private OwnerDrawPanel pnlDescriptions;
		private OwnerDrawPanel pnlLanguages;
	}
}

