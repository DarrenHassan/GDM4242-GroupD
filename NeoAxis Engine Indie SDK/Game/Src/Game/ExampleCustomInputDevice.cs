// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.MathEx;

namespace Game
{
	//for enabling this example device you need uncomment "ExampleCustomInputDevice.InitDevice();"
	//in the GameEngineApp.cs. after it you will see this device in the Game Options window.

	public class ExampleCustomInputDeviceSpecialEvent : InputEvent
	{
		public ExampleCustomInputDeviceSpecialEvent( InputDevice device )
			: base( device )
		{
		}
	}

	public class ExampleCustomInputDevice : JoystickInputDevice
	{
		bool lastButton1Pressed;
		bool lastButton2Pressed;

		//

		public ExampleCustomInputDevice( string name )
			: base( name )
		{
		}

		internal bool Init()
		{
			//buttons
			Button[] buttons = new Button[ 2 ];
			buttons[ 0 ] = new Button( JoystickButtons.Button1, 0 );
			buttons[ 1 ] = new Button( JoystickButtons.Button2, 1 );

			//axes
			Axis[] axes = new Axis[ 1 ];
			axes[ 0 ] = new JoystickInputDevice.Axis( JoystickAxes.X, new Range( -1, 1 ), false );

			//povs
			POV[] povs = new POV[ 0 ];
			//povs[ 0 ] = new JoystickInputDevice.POV( JoystickPOVs.POV1 );

			//sliders
			Slider[] sliders = new Slider[ 0 ];
			//sliders[ 0 ] = new Slider( JoystickSliders.Slider1 );

			//forceFeedbackController
			ForceFeedbackController forceFeedbackController = null;

			//initialize data
			InitDeviceData( buttons, axes, povs, sliders, forceFeedbackController );

			return true;
		}

		protected override void OnShutdown()
		{
		}

		protected override void OnUpdateState()
		{
			//button1
			{
				bool pressed = EngineApp.Instance.IsKeyPressed( EKeys.H );
				if( lastButton1Pressed != pressed )
				{
					if( pressed )
					{
						InputDeviceManager.Instance.SendEvent(
							new JoystickButtonDownEvent( this, Buttons[ 0 ] ) );
					}
					else
					{
						InputDeviceManager.Instance.SendEvent(
							new JoystickButtonUpEvent( this, Buttons[ 0 ] ) );
					}
					lastButton1Pressed = pressed;
				}
			}

			//button2
			{
				bool pressed = EngineApp.Instance.IsKeyPressed( EKeys.J );
				if( lastButton2Pressed != pressed )
				{
					if( pressed )
					{
						InputDeviceManager.Instance.SendEvent(
							new JoystickButtonDownEvent( this, Buttons[ 1 ] ) );
					}
					else
					{
						InputDeviceManager.Instance.SendEvent(
							new JoystickButtonUpEvent( this, Buttons[ 1 ] ) );
					}
					lastButton2Pressed = pressed;
				}
			}

			//axis X
			{
				float value = MathFunctions.Sin( EngineApp.Instance.Time * 2.0f );

				Axes[ 0 ].Value = value;

				InputDeviceManager.Instance.SendEvent(
					new JoystickAxisChangedEvent( this, Axes[ 0 ] ) );
			}

			//custom event example
			{
				//this event will be caused in the EngineApp.OnCustomInputDeviceEvent()
				//and in the all gui controls EControl.OnCustomInputDeviceEvent().
				ExampleCustomInputDeviceSpecialEvent customEvent =
					new ExampleCustomInputDeviceSpecialEvent( this );
				InputDeviceManager.Instance.SendEvent( customEvent );
			}

		}

		public static void InitDevice()
		{
			if( InputDeviceManager.Instance == null )
				return;

			ExampleCustomInputDevice device = new ExampleCustomInputDevice( "ExampleCustomDevice" );

			if( !device.Init() )
				return;

			InputDeviceManager.Instance.RegisterDevice( device );
		}
	}
}
