﻿using Microsoft.Maui.Controls;
using Application = Microsoft.Maui.Controls.Application;

namespace BigIslandBarcode;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new NavigationPage(new MainPage());
	}
}
