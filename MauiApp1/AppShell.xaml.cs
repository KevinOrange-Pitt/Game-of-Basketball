#if IOS || MACCATALYST
using UIKit;
#endif

namespace MauiApp1;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

#if IOS || MACCATALYST
		SetTabBarFontSize();
#endif
	}

#if IOS || MACCATALYST
	private static void SetTabBarFontSize()
	{
		var normalTitle = new UIStringAttributes
		{
			Font = UIFont.SystemFontOfSize(16, UIFontWeight.Medium)
		};

		var selectedTitle = new UIStringAttributes
		{
			Font = UIFont.SystemFontOfSize(160, UIFontWeight.Semibold)
		};

		var appearance = new UITabBarAppearance();
		appearance.ConfigureWithDefaultBackground();
		appearance.StackedLayoutAppearance.Normal.TitleTextAttributes = normalTitle;
		appearance.StackedLayoutAppearance.Selected.TitleTextAttributes = selectedTitle;
		appearance.InlineLayoutAppearance.Normal.TitleTextAttributes = normalTitle;
		appearance.InlineLayoutAppearance.Selected.TitleTextAttributes = selectedTitle;
		appearance.CompactInlineLayoutAppearance.Normal.TitleTextAttributes = normalTitle;
		appearance.CompactInlineLayoutAppearance.Selected.TitleTextAttributes = selectedTitle;

		UITabBar.Appearance.StandardAppearance = appearance;
		UITabBar.Appearance.ScrollEdgeAppearance = appearance;
	}
#endif
}
