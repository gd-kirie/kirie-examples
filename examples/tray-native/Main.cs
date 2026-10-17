using GdKirie.Tray;
using Godot;

public partial class Main : Node
{
    private TrayIcon? _tray;
    private Menu? _menu;
    private CheckMenuItem? _check;
    private TrayEvents? _events;

    public override void _Ready()
    {
        try
        {
            _check = new CheckMenuItem(new CheckMenuItemOptions("Enabled") { Id = "enabled", Checked = true });
            _menu = new Menu(new MenuOptions { Items = [_check] });
            _tray = new TrayIcon(new TrayOptions
            {
                Menu = _menu,
                Icon = new IconSource.Resource("res://icon.svg"),
                Tooltip = "Standalone C# Tray SDK",
            });

            _events = new TrayEvents();
            _events.Menu += OnMenu;
            _events.Tray += value => GD.Print($"Tray {value.Id}: {value.Type}");
            var label = new Label
            {
                Text = "This tray uses the C# SDK without Eventa or WebView.\nClick the tray item to toggle its native checked state.",
                Position = new Vector2(24, 24),
            };
            AddChild(label);

            var openMenu = new Button
            {
                Text = "Open tray menu",
                Position = new Vector2(24, 90),
                Disabled = OS.GetName() == "Linux",
            };
            openMenu.Pressed += _tray.ShowMenu;
            AddChild(openMenu);
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }

    private void OnMenu(string id)
    {
        if (id != _check!.Id)
        {
            return;
        }

        _check!.Text = _check.Checked ? "Enabled" : "Disabled";
        GD.Print($"Native check clicked: {_check.Checked}, text: {_check.Text}");
    }

    public override void _ExitTree()
    {
        _events?.Dispose();
        _tray?.Dispose();
        _menu?.Dispose();
        _check?.Dispose();
    }
}
