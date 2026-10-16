using GdKirie.Tray;
using Godot;

public partial class Main : Node
{
    private TrayIcon? _tray;
    private Menu? _menu;
    private CheckMenuItem? _check;

    public override void _Ready()
    {
        var probe = OS.GetCmdlineUserArgs().Contains("--kirie-tray-sdk-probe");
        try
        {
            _check = new CheckMenuItem(new CheckMenuItemOptions("Enabled") { Checked = true });
            _menu = new Menu(new MenuOptions { Items = [_check] });
            _tray = new TrayIcon(new TrayOptions
            {
                Menu = _menu,
                Icon = new IconSource.Resource("res://icon.svg"),
                Tooltip = "Standalone C# Tray SDK",
            });

            if (probe)
            {
                RunProbe();
                return;
            }

            _check.Clicked += OnCheckClicked;
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

    private void OnCheckClicked(object? sender, EventArgs args)
    {
        _check!.Text = _check.Checked ? "Enabled" : "Disabled";
        GD.Print($"Native check clicked: {_check.Checked}, text: {_check.Text}");
    }

    private void RunProbe()
    {
        using var queried = (CheckMenuItem)_menu!.Items.Single();
        var nativeId = queried.InstanceId;
        Assert(ConnectionCount(nativeId) == 0, "Query created an unsolicited signal connection.");
        queried.Checked = false;
        queried.Text = "Edited in C#";
        Assert(!_check!.Checked && _check.Text == queried.Text, "Wrappers did not share native state.");

        var clicks = 0;
        EventHandler onClick = (_, _) => clicks++;
        queried.Clicked += onClick;
        using (var native = GodotObject.InstanceFromId(nativeId))
        {
            native!.EmitSignal("clicked");
        }

        queried.Clicked -= onClick;
        Assert(clicks == 1 && ConnectionCount(nativeId) == 0, "Event forwarding or unsubscribe failed.");

        using var item = new MenuItem(new MenuItemOptions("Inserted"));
        _menu.Insert(0, item);
        using var removed = _menu.RemoveAt(0);
        Assert(removed?.InstanceId == item.InstanceId, "RemoveAt returned the wrong native item.");
        Reject(() => _menu.Insert(2, item));
        using var submenu = new Submenu(new SubmenuOptions("Tools"));
        Reject(() => submenu.Append(submenu));
        Reject(() => _tray!.SetIcon(new IconSource.Rgba([0], 1, 1)));
        CheckMipmappedIcons();

        _check.Dispose();
        _menu.Dispose();
        var trayId = _tray!.InstanceId;
        _tray.Dispose();
        Assert(queried.Text == "Edited in C#", "Releasing the owner invalidated a retained reference.");
        queried.Dispose();
        Assert(!GodotObject.IsInstanceIdValid(nativeId), "Final reference did not release the native item.");
        Assert(!GodotObject.IsInstanceIdValid(trayId), "Tray disposal did not release its native object.");
        GD.Print("kirie-tray-sdk-probe:passed");
        GetTree().Quit();
    }

    private static void Reject(Action operation)
    {
        try
        {
            operation();
        }
        catch (TrayException error) when (error.Code == "invalid-argument")
        {
            return;
        }

        throw new InvalidOperationException("Invalid native input was accepted.");
    }

    private static int ConnectionCount(ulong nativeId)
    {
        using var native = GodotObject.InstanceFromId(nativeId);
        using var connections = (Godot.Collections.Array)native!.GetSignalConnectionList("clicked");
        return connections.Count;
    }

    private void CheckMipmappedIcons()
    {
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
        image.Fill(Colors.White);
        Assert(image.GenerateMipmaps() == Error.Ok, "Could not generate icon mipmaps.");
        using var texture = ImageTexture.CreateFromImage(image);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"kirie-icon-{Guid.NewGuid()}.res");
        try
        {
            Assert(ResourceSaver.Save(texture, path) == Error.Ok, "Could not save the mipmapped texture.");
            var source = new IconSource.Resource(path);
            _tray!.SetIcon(source);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public override void _ExitTree()
    {
        _tray?.Dispose();
        _menu?.Dispose();
        _check?.Dispose();
    }
}
