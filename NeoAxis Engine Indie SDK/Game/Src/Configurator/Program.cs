// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Engine.FileSystem;

namespace Configurator
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			if( !VirtualFileSystem.Init( null, true, null, null, null ) )
				return;

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			Application.Run( new MainForm() );

			VirtualFileSystem.Shutdown();
		}
	}
}
