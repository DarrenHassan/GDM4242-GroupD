// Copyright (C) 2006-2010 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ChatExample
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		private void buttonCancel_Click( object sender, EventArgs e )
		{
			Close();
		}

		private void buttonServer_Click( object sender, EventArgs e )
		{
			if( ServerForm.instance == null )
			{
				ServerForm form = new ServerForm();
				form.Show();
			}
			else
				ServerForm.instance.Activate();
		}

		private void buttonClient_Click( object sender, EventArgs e )
		{
			if( ClientForm.instance == null )
			{
				ClientForm form = new ClientForm();
				form.Show();
			}
			else
				ClientForm.instance.Activate();
		}
	}
}
