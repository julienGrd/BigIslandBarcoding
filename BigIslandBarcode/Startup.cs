﻿using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.LifecycleEvents;
using ZXing.Net.Maui;
using Microsoft.Maui.ApplicationModel;

namespace BigIslandBarcode;

public class MauiProgram
{
	public static MauiApp Create()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseBarcodeReader()
			.UseMauiApp<App>()
				
			.ConfigureLifecycleEvents(life =>
            {
#if __ANDROID__
                Platform.Init(MauiApplication.Current);

                life.AddAndroid(android => android
                    .OnRequestPermissionsResult((activity, requestCode, permissions, grantResults) =>
                    {
                        Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                    })
                    .OnNewIntent((activity, intent) =>
                    {
                        Platform.OnNewIntent(intent);
                    })
                    .OnResume((activity) =>
                    {
                        Platform.OnResume();
                    }));
#elif __IOS__
			//life.AddiOS(ios => ios
			//	.ContinueUserActivity((application, userActivity, completionHandler) =>
			//	{
			//		return Platform.ContinueUserActivity(application, userActivity, completionHandler);
			//	})
			//	.OpenUrl((application, url, options) =>
			//	{
			//		return Platform.OpenUrl(application, url, options);
			//	})
			//	.PerformActionForShortcutItem((application, shortcutItem, completionHandler) =>
			//	{
			//		Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler);
			//	}));
#elif WINDOWS
			life.AddWindows(windows => windows
				.OnLaunched((application, args) =>
				{
					Platform.OnLaunched(args);
				}));
#endif
            });

        return builder.Build();
	}
}