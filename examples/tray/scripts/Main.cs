using System.Text.Json.Serialization;
using Eventa;
using GdKirie.EventaAdapter;
using GdKirie.Tray;
using GdKirie.Tray.EventaAdapter;
using Godot;

public partial class Main : Node
{
    private readonly InvokeEventDefinition<TrayResource, BootstrapRequest> _bootstrap = new("example:tray:bootstrap");
    private KirieClient? _kirie;
    private KirieEventaContextHandle? _eventa;
    private TrayEventaAdapter? _tray;
    private CheckMenuItem? _sharedItem;
    private IDisposable? _bootstrapRegistration;

    public override void _Ready()
    {
        var probe = OS.GetCmdlineUserArgs().Contains("--kirie-tray-probe");
        _kirie = KirieClient.FromNode(GetNode("KirieNode"));
        if (!_kirie.IsAvailable)
        {
            GD.PushError("Kirie is unavailable on this platform.");
            if (probe)
            {
                GetTree().Quit(1);
            }

            return;
        }

        _sharedItem = new CheckMenuItem(new CheckMenuItemOptions("Created in C#") { Id = "shared-check", Checked = true });

        var registry = GdKirieTrayEventaAdapter.Register(new KirieEventaJsonRegistry());
        registry.RegisterInvoke(_bootstrap, TrayExampleJsonContext.Default.TrayResource, TrayExampleJsonContext.Default.BootstrapRequest);
        _eventa = _kirie.CreateEventaContext(registry);
        _bootstrapRegistration = _eventa.Context.RegisterInvokeHandler(_bootstrap, (BootstrapRequest _, CancellationToken _) =>
        {
            _tray?.Dispose();
            _tray = GdKirieTrayEventaAdapter.Attach(_eventa.Context);
            return Task.FromResult(_tray.Expose(_sharedItem));
        });
        _kirie.IpcError += GD.PushError;

        var initialUrl = _kirie.GetLaunchOption("kirie-web-url").Trim();
        if (probe)
        {
            initialUrl = "res://src-web/dist/index.html?tray-probe";
            _kirie.TextReceived += OnProbeMessage;
            GetTree().CreateTimer(30).Timeout += () =>
            {
                GD.PushError("Tray forwarding probe timed out.");
                GetTree().Quit(1);
            };
        }

        _kirie.CreateWebView(initialUrl.Length == 0 ? "res://src-web/dist/index.html" : initialUrl);
    }

    private void OnProbeMessage(string message)
    {
        if (message == "kirie-tray-probe:passed")
        {
            if (_sharedItem is null || _sharedItem.IsDisposed || _sharedItem.Checked || _sharedItem.Text != "Changed in browser")
            {
                GD.PushError("Browser edits or reference release did not preserve the C# object.");
                GetTree().Quit(1);
                return;
            }

            GD.Print(message);
            GetTree().Quit();
            return;
        }

        if (message.StartsWith("kirie-tray-probe:failed:", StringComparison.Ordinal))
        {
            GD.PushError(message);
            GetTree().Quit(1);
        }
    }

    public override void _ExitTree()
    {
        _kirie?.TextReceived -= OnProbeMessage;

        _bootstrapRegistration?.Dispose();
        _tray?.Dispose();
        _sharedItem?.Dispose();
        _eventa?.Dispose();
        _kirie?.Dispose();
    }

    private sealed record BootstrapRequest;

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(BootstrapRequest))]
    [JsonSerializable(typeof(TrayResource))]
    private sealed partial class TrayExampleJsonContext : JsonSerializerContext;
}
