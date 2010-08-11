namespace WindowsAppExample
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if( disposing && ( components != null ) )
			{
				components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( MainForm ) );
			this.buttonExit = new System.Windows.Forms.Button();
			this.labelEngineVersion = new System.Windows.Forms.Label();
			this.renderTargetUserControl1 = new WindowsAppFramework.RenderTargetUserControl();
			this.buttonAdditionalForm = new System.Windows.Forms.Button();
			this.buttonCreateBox = new System.Windows.Forms.Button();
			this.trackBarVolume = new System.Windows.Forms.TrackBar();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonShowUI = new System.Windows.Forms.Button();
			( (System.ComponentModel.ISupportInitialize)( this.trackBarVolume ) ).BeginInit();
			this.SuspendLayout();
			// 
			// buttonExit
			// 
			this.buttonExit.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonExit.Location = new System.Drawing.Point( 691, 12 );
			this.buttonExit.Name = "buttonExit";
			this.buttonExit.Size = new System.Drawing.Size( 75, 23 );
			this.buttonExit.TabIndex = 0;
			this.buttonExit.Text = "E&xit";
			this.buttonExit.UseVisualStyleBackColor = true;
			this.buttonExit.Click += new System.EventHandler( this.buttonExit_Click );
			// 
			// labelEngineVersion
			// 
			this.labelEngineVersion.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.labelEngineVersion.AutoSize = true;
			this.labelEngineVersion.Location = new System.Drawing.Point( 9, 527 );
			this.labelEngineVersion.Name = "labelEngineVersion";
			this.labelEngineVersion.Size = new System.Drawing.Size( 112, 15 );
			this.labelEngineVersion.TabIndex = 4;
			this.labelEngineVersion.Text = "NeoAxis Group Ltd.";
			// 
			// renderTargetUserControl1
			// 
			this.renderTargetUserControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.renderTargetUserControl1.AutomaticUpdateFPS = 30F;
			this.renderTargetUserControl1.BackColor = System.Drawing.Color.Black;
			this.renderTargetUserControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.renderTargetUserControl1.Location = new System.Drawing.Point( 12, 12 );
			this.renderTargetUserControl1.MouseRelativeMode = false;
			this.renderTargetUserControl1.Name = "renderTargetUserControl1";
			this.renderTargetUserControl1.Size = new System.Drawing.Size( 667, 502 );
			this.renderTargetUserControl1.TabIndex = 5;
			// 
			// buttonAdditionalForm
			// 
			this.buttonAdditionalForm.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonAdditionalForm.Location = new System.Drawing.Point( 691, 95 );
			this.buttonAdditionalForm.Name = "buttonAdditionalForm";
			this.buttonAdditionalForm.Size = new System.Drawing.Size( 75, 23 );
			this.buttonAdditionalForm.TabIndex = 1;
			this.buttonAdditionalForm.Text = "New Form";
			this.buttonAdditionalForm.UseVisualStyleBackColor = true;
			this.buttonAdditionalForm.Click += new System.EventHandler( this.buttonAdditionalForm_Click );
			// 
			// buttonCreateBox
			// 
			this.buttonCreateBox.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCreateBox.Location = new System.Drawing.Point( 691, 149 );
			this.buttonCreateBox.Name = "buttonCreateBox";
			this.buttonCreateBox.Size = new System.Drawing.Size( 75, 23 );
			this.buttonCreateBox.TabIndex = 2;
			this.buttonCreateBox.Text = "Create box";
			this.buttonCreateBox.UseVisualStyleBackColor = true;
			this.buttonCreateBox.Click += new System.EventHandler( this.buttonCreateBox_Click );
			// 
			// trackBarVolume
			// 
			this.trackBarVolume.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.trackBarVolume.LargeChange = 50;
			this.trackBarVolume.Location = new System.Drawing.Point( 685, 251 );
			this.trackBarVolume.Maximum = 100;
			this.trackBarVolume.Name = "trackBarVolume";
			this.trackBarVolume.Size = new System.Drawing.Size( 81, 50 );
			this.trackBarVolume.TabIndex = 3;
			this.trackBarVolume.TickFrequency = 10;
			this.trackBarVolume.Value = 25;
			this.trackBarVolume.Scroll += new System.EventHandler( this.trackBarVolume_Scroll );
			// 
			// label1
			// 
			this.label1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 688, 233 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 52, 15 );
			this.label1.TabIndex = 9;
			this.label1.Text = "Volume:";
			// 
			// buttonShowUI
			// 
			this.buttonShowUI.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonShowUI.Location = new System.Drawing.Point( 691, 178 );
			this.buttonShowUI.Name = "buttonShowUI";
			this.buttonShowUI.Size = new System.Drawing.Size( 75, 23 );
			this.buttonShowUI.TabIndex = 10;
			this.buttonShowUI.Text = "Show UI";
			this.buttonShowUI.UseVisualStyleBackColor = true;
			this.buttonShowUI.Click += new System.EventHandler( this.buttonShowUI_Click );
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size( 778, 549 );
			this.Controls.Add( this.buttonShowUI );
			this.Controls.Add( this.label1 );
			this.Controls.Add( this.trackBarVolume );
			this.Controls.Add( this.buttonCreateBox );
			this.Controls.Add( this.buttonAdditionalForm );
			this.Controls.Add( this.renderTargetUserControl1 );
			this.Controls.Add( this.labelEngineVersion );
			this.Controls.Add( this.buttonExit );
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Windows Application Example";
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler( this.MainForm_FormClosed );
			( (System.ComponentModel.ISupportInitialize)( this.trackBarVolume ) ).EndInit();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonExit;
		private System.Windows.Forms.Label labelEngineVersion;
		private WindowsAppFramework.RenderTargetUserControl renderTargetUserControl1;
		private System.Windows.Forms.Button buttonAdditionalForm;
		private System.Windows.Forms.Button buttonCreateBox;
		private System.Windows.Forms.TrackBar trackBarVolume;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonShowUI;

	}
}

