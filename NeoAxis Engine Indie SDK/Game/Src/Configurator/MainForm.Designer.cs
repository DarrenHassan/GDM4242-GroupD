namespace Configurator
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
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPageRender = new System.Windows.Forms.TabPage();
			this.label11 = new System.Windows.Forms.Label();
			this.comboBoxRenderTechnique = new System.Windows.Forms.ComboBox();
			this.label19 = new System.Windows.Forms.Label();
			this.comboBoxFiltering = new System.Windows.Forms.ComboBox();
			this.label9 = new System.Windows.Forms.Label();
			this.checkBoxVerticalSync = new System.Windows.Forms.CheckBox();
			this.comboBoxAntialiasing = new System.Windows.Forms.ComboBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.checkBoxFullScreen = new System.Windows.Forms.CheckBox();
			this.comboBoxVideoMode = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.checkBoxAllowChangeDisplayFrequency = new System.Windows.Forms.CheckBox();
			this.comboBoxMaxVertexShaders = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.comboBoxMaxPixelShaders = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.comboBoxRenderSystems = new System.Windows.Forms.ComboBox();
			this.tabPagePhysics = new System.Windows.Forms.TabPage();
			this.labelPhysXHardwareAcceleration = new System.Windows.Forms.Label();
			this.label18 = new System.Windows.Forms.Label();
			this.comboBoxPhysicsSystems = new System.Windows.Forms.ComboBox();
			this.tabPageSound = new System.Windows.Forms.TabPage();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxSoundSystems = new System.Windows.Forms.ComboBox();
			this.tabPageLocalization = new System.Windows.Forms.TabPage();
			this.label10 = new System.Windows.Forms.Label();
			this.comboBoxLanguages = new System.Windows.Forms.ComboBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.tabControl1.SuspendLayout();
			this.tabPageRender.SuspendLayout();
			this.tabPagePhysics.SuspendLayout();
			this.tabPageSound.SuspendLayout();
			this.tabPageLocalization.SuspendLayout();
			this.SuspendLayout();
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( ( ( System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom )
							| System.Windows.Forms.AnchorStyles.Left )
							| System.Windows.Forms.AnchorStyles.Right ) ) );
			this.tabControl1.Controls.Add( this.tabPageRender );
			this.tabControl1.Controls.Add( this.tabPagePhysics );
			this.tabControl1.Controls.Add( this.tabPageSound );
			this.tabControl1.Controls.Add( this.tabPageLocalization );
			this.tabControl1.Location = new System.Drawing.Point( 12, 12 );
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size( 474, 416 );
			this.tabControl1.TabIndex = 0;
			// 
			// tabPageRender
			// 
			this.tabPageRender.Controls.Add( this.label11 );
			this.tabPageRender.Controls.Add( this.comboBoxRenderTechnique );
			this.tabPageRender.Controls.Add( this.label19 );
			this.tabPageRender.Controls.Add( this.comboBoxFiltering );
			this.tabPageRender.Controls.Add( this.label9 );
			this.tabPageRender.Controls.Add( this.checkBoxVerticalSync );
			this.tabPageRender.Controls.Add( this.comboBoxAntialiasing );
			this.tabPageRender.Controls.Add( this.label8 );
			this.tabPageRender.Controls.Add( this.label7 );
			this.tabPageRender.Controls.Add( this.checkBoxFullScreen );
			this.tabPageRender.Controls.Add( this.comboBoxVideoMode );
			this.tabPageRender.Controls.Add( this.label4 );
			this.tabPageRender.Controls.Add( this.checkBoxAllowChangeDisplayFrequency );
			this.tabPageRender.Controls.Add( this.comboBoxMaxVertexShaders );
			this.tabPageRender.Controls.Add( this.label6 );
			this.tabPageRender.Controls.Add( this.comboBoxMaxPixelShaders );
			this.tabPageRender.Controls.Add( this.label5 );
			this.tabPageRender.Controls.Add( this.label1 );
			this.tabPageRender.Controls.Add( this.comboBoxRenderSystems );
			this.tabPageRender.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageRender.Name = "tabPageRender";
			this.tabPageRender.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageRender.Size = new System.Drawing.Size( 466, 390 );
			this.tabPageRender.TabIndex = 1;
			this.tabPageRender.Text = "Render";
			this.tabPageRender.UseVisualStyleBackColor = true;
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point( 296, 72 );
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size( 164, 81 );
			this.label11.TabIndex = 22;
			this.label11.Text = "If \"Recommended setting\" option is selected shaders will be disabled on Intel vid" +
				 "eocards.";
			// 
			// comboBoxRenderTechnique
			// 
			this.comboBoxRenderTechnique.DropDownHeight = 212;
			this.comboBoxRenderTechnique.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderTechnique.FormattingEnabled = true;
			this.comboBoxRenderTechnique.IntegralHeight = false;
			this.comboBoxRenderTechnique.Location = new System.Drawing.Point( 23, 154 );
			this.comboBoxRenderTechnique.Name = "comboBoxRenderTechnique";
			this.comboBoxRenderTechnique.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxRenderTechnique.TabIndex = 3;
			// 
			// label19
			// 
			this.label19.AutoSize = true;
			this.label19.Location = new System.Drawing.Point( 20, 138 );
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size( 105, 15 );
			this.label19.TabIndex = 21;
			this.label19.Text = "Render technique";
			// 
			// comboBoxFiltering
			// 
			this.comboBoxFiltering.DropDownHeight = 212;
			this.comboBoxFiltering.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFiltering.FormattingEnabled = true;
			this.comboBoxFiltering.IntegralHeight = false;
			this.comboBoxFiltering.Location = new System.Drawing.Point( 23, 194 );
			this.comboBoxFiltering.Name = "comboBoxFiltering";
			this.comboBoxFiltering.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxFiltering.TabIndex = 4;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point( 20, 178 );
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size( 54, 15 );
			this.label9.TabIndex = 19;
			this.label9.Text = "Filtering:";
			// 
			// checkBoxVerticalSync
			// 
			this.checkBoxVerticalSync.AutoSize = true;
			this.checkBoxVerticalSync.Location = new System.Drawing.Point( 23, 326 );
			this.checkBoxVerticalSync.Name = "checkBoxVerticalSync";
			this.checkBoxVerticalSync.Size = new System.Drawing.Size( 93, 19 );
			this.checkBoxVerticalSync.TabIndex = 8;
			this.checkBoxVerticalSync.Text = "Vertical sync";
			this.checkBoxVerticalSync.UseVisualStyleBackColor = true;
			// 
			// comboBoxAntialiasing
			// 
			this.comboBoxAntialiasing.DropDownHeight = 212;
			this.comboBoxAntialiasing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxAntialiasing.FormattingEnabled = true;
			this.comboBoxAntialiasing.IntegralHeight = false;
			this.comboBoxAntialiasing.Items.AddRange( new object[] {
            "No",
            "2",
            "4",
            "6",
            "8"} );
			this.comboBoxAntialiasing.Location = new System.Drawing.Point( 23, 234 );
			this.comboBoxAntialiasing.Name = "comboBoxAntialiasing";
			this.comboBoxAntialiasing.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxAntialiasing.TabIndex = 5;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point( 20, 218 );
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size( 133, 15 );
			this.label8.TabIndex = 17;
			this.label8.Text = "Full-scene antialiasing:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point( 40, 365 );
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size( 251, 15 );
			this.label7.TabIndex = 15;
			this.label7.Text = "(Disable it if you have black screen at startup)";
			// 
			// checkBoxFullScreen
			// 
			this.checkBoxFullScreen.AutoSize = true;
			this.checkBoxFullScreen.Location = new System.Drawing.Point( 23, 305 );
			this.checkBoxFullScreen.Name = "checkBoxFullScreen";
			this.checkBoxFullScreen.Size = new System.Drawing.Size( 86, 19 );
			this.checkBoxFullScreen.TabIndex = 7;
			this.checkBoxFullScreen.Text = "Full screen";
			this.checkBoxFullScreen.UseVisualStyleBackColor = true;
			// 
			// comboBoxVideoMode
			// 
			this.comboBoxVideoMode.DropDownHeight = 212;
			this.comboBoxVideoMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxVideoMode.FormattingEnabled = true;
			this.comboBoxVideoMode.IntegralHeight = false;
			this.comboBoxVideoMode.Location = new System.Drawing.Point( 23, 274 );
			this.comboBoxVideoMode.Name = "comboBoxVideoMode";
			this.comboBoxVideoMode.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxVideoMode.TabIndex = 6;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point( 20, 258 );
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size( 76, 15 );
			this.label4.TabIndex = 14;
			this.label4.Text = "Video mode:";
			// 
			// checkBoxAllowChangeDisplayFrequency
			// 
			this.checkBoxAllowChangeDisplayFrequency.AutoSize = true;
			this.checkBoxAllowChangeDisplayFrequency.Checked = true;
			this.checkBoxAllowChangeDisplayFrequency.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxAllowChangeDisplayFrequency.Location = new System.Drawing.Point( 23, 347 );
			this.checkBoxAllowChangeDisplayFrequency.Name = "checkBoxAllowChangeDisplayFrequency";
			this.checkBoxAllowChangeDisplayFrequency.Size = new System.Drawing.Size( 386, 19 );
			this.checkBoxAllowChangeDisplayFrequency.TabIndex = 9;
			this.checkBoxAllowChangeDisplayFrequency.Text = "Enable program to change display frequency in a full screen mode";
			this.checkBoxAllowChangeDisplayFrequency.UseVisualStyleBackColor = true;
			// 
			// comboBoxMaxVertexShaders
			// 
			this.comboBoxMaxVertexShaders.DropDownHeight = 212;
			this.comboBoxMaxVertexShaders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMaxVertexShaders.FormattingEnabled = true;
			this.comboBoxMaxVertexShaders.IntegralHeight = false;
			this.comboBoxMaxVertexShaders.Location = new System.Drawing.Point( 23, 114 );
			this.comboBoxMaxVertexShaders.Name = "comboBoxMaxVertexShaders";
			this.comboBoxMaxVertexShaders.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxMaxVertexShaders.TabIndex = 2;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point( 20, 98 );
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size( 190, 15 );
			this.label6.TabIndex = 8;
			this.label6.Text = "Maximum vertex shaders version:";
			// 
			// comboBoxMaxPixelShaders
			// 
			this.comboBoxMaxPixelShaders.DropDownHeight = 212;
			this.comboBoxMaxPixelShaders.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxMaxPixelShaders.FormattingEnabled = true;
			this.comboBoxMaxPixelShaders.IntegralHeight = false;
			this.comboBoxMaxPixelShaders.Location = new System.Drawing.Point( 23, 73 );
			this.comboBoxMaxPixelShaders.Name = "comboBoxMaxPixelShaders";
			this.comboBoxMaxPixelShaders.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxMaxPixelShaders.TabIndex = 1;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point( 20, 57 );
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size( 184, 15 );
			this.label5.TabIndex = 6;
			this.label5.Text = "Maximum pixel shaders version:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point( 20, 17 );
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size( 92, 15 );
			this.label1.TabIndex = 3;
			this.label1.Text = "Render system:";
			// 
			// comboBoxRenderSystems
			// 
			this.comboBoxRenderSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxRenderSystems.FormattingEnabled = true;
			this.comboBoxRenderSystems.Location = new System.Drawing.Point( 23, 33 );
			this.comboBoxRenderSystems.Name = "comboBoxRenderSystems";
			this.comboBoxRenderSystems.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxRenderSystems.TabIndex = 0;
			this.comboBoxRenderSystems.SelectedIndexChanged += new System.EventHandler( this.comboBoxRenderSystems_SelectedIndexChanged );
			// 
			// tabPagePhysics
			// 
			this.tabPagePhysics.Controls.Add( this.labelPhysXHardwareAcceleration );
			this.tabPagePhysics.Controls.Add( this.label18 );
			this.tabPagePhysics.Controls.Add( this.comboBoxPhysicsSystems );
			this.tabPagePhysics.Location = new System.Drawing.Point( 4, 22 );
			this.tabPagePhysics.Name = "tabPagePhysics";
			this.tabPagePhysics.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPagePhysics.Size = new System.Drawing.Size( 466, 390 );
			this.tabPagePhysics.TabIndex = 4;
			this.tabPagePhysics.Text = "Physics";
			this.tabPagePhysics.UseVisualStyleBackColor = true;
			// 
			// labelPhysXHardwareAcceleration
			// 
			this.labelPhysXHardwareAcceleration.Location = new System.Drawing.Point( 20, 57 );
			this.labelPhysXHardwareAcceleration.Name = "labelPhysXHardwareAcceleration";
			this.labelPhysXHardwareAcceleration.Size = new System.Drawing.Size( 276, 60 );
			this.labelPhysXHardwareAcceleration.TabIndex = 10;
			this.labelPhysXHardwareAcceleration.Text = "PhysX: To enable/disable hardware acceleration use Windows Control Panel -> NVIDI" +
				 "A PhysX.";
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point( 20, 17 );
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size( 92, 15 );
			this.label18.TabIndex = 9;
			this.label18.Text = "Physics system:";
			// 
			// comboBoxPhysicsSystems
			// 
			this.comboBoxPhysicsSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxPhysicsSystems.FormattingEnabled = true;
			this.comboBoxPhysicsSystems.Location = new System.Drawing.Point( 23, 33 );
			this.comboBoxPhysicsSystems.Name = "comboBoxPhysicsSystems";
			this.comboBoxPhysicsSystems.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxPhysicsSystems.TabIndex = 0;
			this.comboBoxPhysicsSystems.SelectedIndexChanged += new System.EventHandler( this.comboBoxPhysicsSystems_SelectedIndexChanged );
			// 
			// tabPageSound
			// 
			this.tabPageSound.Controls.Add( this.label3 );
			this.tabPageSound.Controls.Add( this.comboBoxSoundSystems );
			this.tabPageSound.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageSound.Name = "tabPageSound";
			this.tabPageSound.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageSound.Size = new System.Drawing.Size( 466, 390 );
			this.tabPageSound.TabIndex = 2;
			this.tabPageSound.Text = "Sound";
			this.tabPageSound.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point( 20, 17 );
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size( 87, 15 );
			this.label3.TabIndex = 7;
			this.label3.Text = "Sound system:";
			// 
			// comboBoxSoundSystems
			// 
			this.comboBoxSoundSystems.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxSoundSystems.FormattingEnabled = true;
			this.comboBoxSoundSystems.Location = new System.Drawing.Point( 23, 33 );
			this.comboBoxSoundSystems.Name = "comboBoxSoundSystems";
			this.comboBoxSoundSystems.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxSoundSystems.TabIndex = 6;
			// 
			// tabPageLocalization
			// 
			this.tabPageLocalization.Controls.Add( this.label10 );
			this.tabPageLocalization.Controls.Add( this.comboBoxLanguages );
			this.tabPageLocalization.Location = new System.Drawing.Point( 4, 22 );
			this.tabPageLocalization.Name = "tabPageLocalization";
			this.tabPageLocalization.Padding = new System.Windows.Forms.Padding( 3 );
			this.tabPageLocalization.Size = new System.Drawing.Size( 466, 390 );
			this.tabPageLocalization.TabIndex = 3;
			this.tabPageLocalization.Text = "Localization";
			this.tabPageLocalization.UseVisualStyleBackColor = true;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point( 20, 17 );
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size( 66, 15 );
			this.label10.TabIndex = 8;
			this.label10.Text = "Language:";
			// 
			// comboBoxLanguages
			// 
			this.comboBoxLanguages.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxLanguages.FormattingEnabled = true;
			this.comboBoxLanguages.Location = new System.Drawing.Point( 23, 33 );
			this.comboBoxLanguages.Name = "comboBoxLanguages";
			this.comboBoxLanguages.Size = new System.Drawing.Size( 267, 21 );
			this.comboBoxLanguages.TabIndex = 7;
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point( 330, 440 );
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size( 75, 23 );
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler( this.buttonOK_Click );
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right ) ) );
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point( 411, 440 );
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size( 75, 23 );
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler( this.buttonCancel_Click );
			// 
			// label2
			// 
			this.label2.Anchor = ( (System.Windows.Forms.AnchorStyles)( ( System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left ) ) );
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point( 9, 450 );
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size( 143, 15 );
			this.label2.TabIndex = 3;
			this.label2.Text = "2010 NeoAxis Group Ltd.";
			// 
			// MainForm
			// 
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size( 498, 475 );
			this.Controls.Add( this.label2 );
			this.Controls.Add( this.buttonCancel );
			this.Controls.Add( this.buttonOK );
			this.Controls.Add( this.tabControl1 );
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ( (System.Drawing.Icon)( resources.GetObject( "$this.Icon" ) ) );
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "NeoAxis Engine Configurator";
			this.Load += new System.EventHandler( this.MainForm_Load );
			this.tabControl1.ResumeLayout( false );
			this.tabPageRender.ResumeLayout( false );
			this.tabPageRender.PerformLayout();
			this.tabPagePhysics.ResumeLayout( false );
			this.tabPagePhysics.PerformLayout();
			this.tabPageSound.ResumeLayout( false );
			this.tabPageSound.PerformLayout();
			this.tabPageLocalization.ResumeLayout( false );
			this.tabPageLocalization.PerformLayout();
			this.ResumeLayout( false );
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TabPage tabPageRender;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox comboBoxRenderSystems;
		private System.Windows.Forms.TabPage tabPageSound;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxSoundSystems;
		private System.Windows.Forms.TabPage tabPageLocalization;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ComboBox comboBoxMaxVertexShaders;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ComboBox comboBoxMaxPixelShaders;
		private System.Windows.Forms.CheckBox checkBoxFullScreen;
		private System.Windows.Forms.ComboBox comboBoxVideoMode;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.CheckBox checkBoxAllowChangeDisplayFrequency;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.ComboBox comboBoxAntialiasing;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.CheckBox checkBoxVerticalSync;
		private System.Windows.Forms.ComboBox comboBoxFiltering;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TabPage tabPagePhysics;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.ComboBox comboBoxPhysicsSystems;
		private System.Windows.Forms.ComboBox comboBoxRenderTechnique;
		private System.Windows.Forms.Label label19;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.ComboBox comboBoxLanguages;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label labelPhysXHardwareAcceleration;
	}
}

