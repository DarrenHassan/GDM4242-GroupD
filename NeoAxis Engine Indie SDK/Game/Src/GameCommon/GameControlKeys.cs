// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.EntitySystem;

namespace GameCommon
{
	public enum GameControlKeys
	{
		///////////////////////////////////////////
		//Moving

		[DefaultKeyboardMouseValue( EKeys.W )]
		[DefaultJoystickValue( JoystickAxes.Y, JoystickAxisFilters.GreaterZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_LeftThumbstickY, JoystickAxisFilters.GreaterZero )]
		Forward,

		[DefaultKeyboardMouseValue( EKeys.S )]
		[DefaultJoystickValue( JoystickAxes.Y, JoystickAxisFilters.LessZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_LeftThumbstickY, JoystickAxisFilters.LessZero )]
		Backward,

		[DefaultKeyboardMouseValue( EKeys.A )]
		[DefaultJoystickValue( JoystickAxes.X, JoystickAxisFilters.LessZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_LeftThumbstickX, JoystickAxisFilters.LessZero )]
		Left,

		[DefaultKeyboardMouseValue( EKeys.D )]
		[DefaultJoystickValue( JoystickAxes.X, JoystickAxisFilters.GreaterZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_LeftThumbstickX, JoystickAxisFilters.GreaterZero )]
		Right,

		///////////////////////////////////////////
		//Looking

		[DefaultJoystickValue( JoystickAxes.Rz, JoystickAxisFilters.GreaterZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_RightThumbstickY, JoystickAxisFilters.GreaterZero )]
		//MouseMove (in the PlayerIntellect)
		LookUp,

		[DefaultJoystickValue( JoystickAxes.Rz, JoystickAxisFilters.LessZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_RightThumbstickY, JoystickAxisFilters.LessZero )]
		//MouseMove (in the PlayerIntellect)
		LookDown,

		[DefaultJoystickValue( JoystickAxes.Z, JoystickAxisFilters.LessZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_RightThumbstickX, JoystickAxisFilters.LessZero )]
		//MouseMove (in the PlayerIntellect)
		LookLeft,

		[DefaultJoystickValue( JoystickAxes.Z, JoystickAxisFilters.GreaterZero )]
		[DefaultJoystickValue( JoystickAxes.XBox360_RightThumbstickX, JoystickAxisFilters.GreaterZero )]
		//MouseMove (in the PlayerIntellect)
		LookRight,

		///////////////////////////////////////////
		//Actions

		[DefaultKeyboardMouseValue( EMouseButtons.Left )]
		[DefaultJoystickValue( JoystickButtons.Button1 )]
		[DefaultJoystickValue( JoystickAxes.XBox360_RightTrigger, JoystickAxisFilters.GreaterZero )]
		Fire1,

		[DefaultKeyboardMouseValue( EMouseButtons.Right )]
		[DefaultJoystickValue( JoystickButtons.Button2 )]
		[DefaultJoystickValue( JoystickAxes.XBox360_LeftTrigger, JoystickAxisFilters.GreaterZero )]
		Fire2,

		[DefaultKeyboardMouseValue( EKeys.Space )]
		[DefaultJoystickValue( JoystickButtons.Button3 )]
		[DefaultJoystickValue( JoystickButtons.XBox360_A )]
		Jump,

		[DefaultKeyboardMouseValue( EKeys.R )]
		[DefaultJoystickValue( JoystickButtons.Button4 )]
		[DefaultJoystickValue( JoystickButtons.XBox360_LeftShoulder )]
		Reload,

		[DefaultKeyboardMouseValue( EKeys.E )]
		[DefaultJoystickValue( JoystickButtons.Button5 )]
		[DefaultJoystickValue( JoystickButtons.XBox360_RightShoulder )]
		Use,

		[DefaultJoystickValue( JoystickPOVs.POV1, JoystickPOVDirections.West )]
		PreviousWeapon,

		[DefaultJoystickValue( JoystickPOVs.POV1, JoystickPOVDirections.East )]
		NextWeapon,

		[DefaultKeyboardMouseValue( EKeys.D1 )]
		Weapon1,
		[DefaultKeyboardMouseValue( EKeys.D2 )]
		Weapon2,
		[DefaultKeyboardMouseValue( EKeys.D3 )]
		Weapon3,
		[DefaultKeyboardMouseValue( EKeys.D4 )]
		Weapon4,
		[DefaultKeyboardMouseValue( EKeys.D5 )]
		Weapon5,
		[DefaultKeyboardMouseValue( EKeys.D6 )]
		Weapon6,
		[DefaultKeyboardMouseValue( EKeys.D7 )]
		Weapon7,
		[DefaultKeyboardMouseValue( EKeys.D8 )]
		Weapon8,
		[DefaultKeyboardMouseValue( EKeys.D9 )]
		Weapon9,

	}
}
