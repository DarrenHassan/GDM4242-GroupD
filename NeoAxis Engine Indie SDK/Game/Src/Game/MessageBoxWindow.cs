// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.MathEx;

namespace Game
{
	/// <summary>
	/// Defines a "MessageBox" window.
	/// </summary>
	public class MessageBoxWindow : EControl
	{
		string messageText;
		string caption;
		EButton.ClickDelegate clickHandler;

		//

		public MessageBoxWindow( string messageText, string caption, EButton.ClickDelegate clickHandler )
		{
			this.messageText = messageText;
			this.caption = caption;
			this.clickHandler = clickHandler;
		}

		protected override void OnAttach()
		{
			base.OnAttach();

			TopMost = true;

			EControl window = ControlDeclarationManager.Instance.CreateControl(
				"Gui\\MessageBoxWindow.gui" );
			Controls.Add( window );

			window.Controls[ "MessageText" ].Text = messageText;

			window.Text = caption;

			( (EButton)window.Controls[ "OK" ] ).Click += OKButton_Click;

			BackColor = new ColorValue( 0, 0, 0, .5f );

			EngineApp.Instance.RenderScene();
		}

		void OKButton_Click( EButton sender )
		{
			if( clickHandler != null )
				clickHandler( sender );

			SetShouldDetach();
		}
	}
}
