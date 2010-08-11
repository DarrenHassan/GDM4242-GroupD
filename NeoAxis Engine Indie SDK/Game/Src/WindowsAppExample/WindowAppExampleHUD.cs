// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace WindowsAppExample
{
	public class WindowsAppExampleHUD : EControl
	{
		public WindowsAppExampleHUD()
		{
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			EControl window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\WindowsAppExampleHUD.gui" );
			Controls.Add( window );

			( (EButton)window.Controls[ "Close" ] ).Click += CloseButton_Click;
		}

		void CloseButton_Click( EButton sender )
		{
			SetShouldDetach();
		}
	}
}
