using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Engine.MathEx;
using Engine.Renderer;

namespace WindowsAppExample
{
	public partial class AdditionalForm : Form
	{
		public AdditionalForm()
		{
			InitializeComponent();
		}

		private void buttonClose_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void AdditionalForm_Load( object sender, EventArgs e )
		{
			renderTargetUserControl1.AutomaticUpdateFPS = 60;
			renderTargetUserControl1.Render += renderTargetUserControl1_Render;
		}

		void renderTargetUserControl1_Render( Camera camera )
		{
			//renderTargetUserControl1.CameraNearFarClipDistance =
			//   Map.Instance.GetRealNearFarClipDistance();
			renderTargetUserControl1.CameraFixedUp = Vec3.ZAxis;
			//renderTargetUserControl1.CameraFov = fov;
			renderTargetUserControl1.CameraPosition = new Vec3( 0, 10, 1 );
			renderTargetUserControl1.CameraDirection = new Vec3( 0, 1, 0 );
		}

	}
}