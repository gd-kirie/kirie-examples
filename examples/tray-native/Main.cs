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
        var probe = OS.GetCmdlineUserArgs().Contains("--kirie-tray-sdk-probe");
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

            if (probe)
            {
                RunProbe();
                return;
            }

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

    private void RunProbe()
    {
        using var queried = (CheckMenuItem)_menu!.Items.Single();
        var nativeId = queried.InstanceId;
        queried.Checked = false;
        queried.Text = "Edited in C#";
        Assert(!_check!.Checked && _check.Text == queried.Text, "Wrappers did not share native state.");

        using var item = new MenuItem(new MenuItemOptions("Inserted"));
        _menu.Insert(0, item);
        using var removed = _menu.RemoveAt(0);
        Assert(removed?.InstanceId == item.InstanceId, "RemoveAt returned the wrong native item.");
        Reject(() => _menu.Insert(2, item));
        item.Dispose();
        Assert(removed!.Text == "Inserted", "RemoveAt did not retain an independent reference.");
        using (var secondQuery = (CheckMenuItem)_menu.Items.Single())
        {
            Assert(!ReferenceEquals(queried, secondQuery), "Items reused a managed wrapper.");
            secondQuery.Dispose();
            Assert(queried.Text == "Edited in C#", "Disposing one query invalidated another reference.");
        }
        using var submenu = new Submenu(new SubmenuOptions("Tools"));
        Reject(() => submenu.Append(submenu));
        Reject(() => _tray!.SetIcon(new IconSource.Rgba([0], 1, 1)));
        CheckMipmappedIcons();
        CheckPredefinedItems();

        _check.Dispose();
        queried.Dispose();
        using var retained = (CheckMenuItem)_menu.Items.Single();
        Assert(retained.Text == "Edited in C#", "The menu did not retain its child after wrapper disposal.");
        _menu.Dispose();
        var trayId = _tray!.InstanceId;
        _tray.Dispose();
        Assert(retained.Text == "Edited in C#", "Releasing the owner invalidated a retained reference.");
        retained.Dispose();
        Assert(!GodotObject.IsInstanceIdValid(nativeId), "Final reference did not release the native item.");
        Assert(!GodotObject.IsInstanceIdValid(trayId), "Tray disposal did not release its native object.");
        CheckGlobalEvents();
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

    private static void CheckGlobalEvents()
    {
        // Synthetic singleton signals check forwarding, not operating-system clicks.
        var native = Engine.GetSingleton("KirieTrayEvents");
        using var events = new TrayEvents();
        var menus = 0;
        var trays = 0;
        Action<string> onMenu = id =>
        {
            Assert(id == "enabled", "The global menu event lost its upstream ID.");
            menus++;
        };
        events.Menu += onMenu;
        events.Tray += value =>
        {
            Assert(value.Id == "unexposed-tray" && value.Type == TrayIconEventKind.Click
                && value.Button == TrayMouseButton.Left && value.ButtonState == TrayMouseButtonState.Up
                && value.Position == new TrayPosition(12, 34)
                && value.Rect == new TrayRect(new TrayPosition(10, 30), new TraySize(16, 16)),
                "The global tray event changed its upstream values.");
            trays++;
        };
        using var position = new Godot.Collections.Dictionary { ["x"] = 12.0, ["y"] = 34.0 };
        using var origin = new Godot.Collections.Dictionary { ["x"] = 10.0, ["y"] = 30.0 };
        using var size = new Godot.Collections.Dictionary { ["width"] = 16, ["height"] = 16 };
        using var rect = new Godot.Collections.Dictionary { ["position"] = origin, ["size"] = size };
        using var trayEvent = new Godot.Collections.Dictionary
        {
            ["id"] = "unexposed-tray",
            ["type"] = "Click",
            ["position"] = position,
            ["rect"] = rect,
            ["button"] = "Left",
            ["buttonState"] = "Up",
        };
        native.EmitSignal("menu_event", "enabled");
        native.EmitSignal("tray_event", trayEvent);
        Assert(menus == 1 && trays == 1, "Global events stopped after object disposal.");
        events.Menu -= onMenu;
        native.EmitSignal("menu_event", "enabled");
        Assert(menus == 1, "Removing the menu handler did not stop delivery.");
        events.Dispose();
        native.EmitSignal("tray_event", trayEvent);
        Assert(trays == 1, "Disposing the subscription did not stop delivery.");
    }

    private static void CheckPredefinedItems()
    {
        using var copy = PredefinedMenuItem.Copy();
        using var defaultCopy = PredefinedMenuItem.Copy(null);
        using var emptyCopy = PredefinedMenuItem.Copy("");
        Assert(copy.Text == defaultCopy.Text && emptyCopy.Text == "", "Predefined text defaults changed.");
        using var separator = PredefinedMenuItem.Separator();
        using var header = PredefinedMenuItem.SectionHeader("Actions");
        using var about = PredefinedMenuItem.About(metadata: new AboutMetadata { Name = "Kirie Tray" });
        if (OS.GetName() == "macOS")
        {
            using var icon = new IconMenuItem(new IconMenuItemOptions("Native icon") { Icon = new IconSource.Native("Folder") });
            icon.SetIcon(new IconSource.Native("StatusAvailable"));
        }
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
        _events?.Dispose();
        _tray?.Dispose();
        _menu?.Dispose();
        _check?.Dispose();
    }
}
