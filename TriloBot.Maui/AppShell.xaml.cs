namespace TriloBot.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes for navigation
		Routing.RegisterRoute("JoystickPage", typeof(Pages.JoystickPage));
	}
}
