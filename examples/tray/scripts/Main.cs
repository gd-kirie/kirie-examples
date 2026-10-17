using System.Text.Json.Serialization;
using Eventa;
using GdKirie.EventaAdapter;
using GdKirie.Tray;
using GdKirie.Tray.EventaAdapter;
using Godot;

public partial class Main : Node
{
    private KirieClient? _kirie;
    private KirieEventaContextHandle? _eventa;
    private TrayEventaAdapter? _trayAdapter;
    private CheckMenuItem? _sharedItem;
    private IDisposable? _pageRegistration;

    public override void _Ready()
    {
        _kirie = KirieClient.FromNode(GetNode("KirieNode"));
        if (!_kirie.IsAvailable)
        {
            GD.PushError("Kirie is unavailable on this platform.");
            return;
        }

        _sharedItem = new CheckMenuItem(new CheckMenuItemOptions("Created in C#") { Id = "shared-check", Checked = true });

        var startPage = new InvokeEventDefinition<TrayResource, object?>("example:tray:start-page");
        var registry = GdKirieTrayEventaAdapter.Register(new KirieEventaJsonRegistry());
        registry.RegisterInvoke(startPage, TrayExampleJsonContext.Default.TrayResource, TrayExampleJsonContext.Default.Object);
        _eventa = _kirie.CreateEventaContext(registry);
        _pageRegistration = _eventa.Context.RegisterInvokeHandler(startPage, (object? _, CancellationToken _) =>
        {
            // A new page releases the previous page's handles. The C# item stays alive.
            _trayAdapter?.Dispose();
            _trayAdapter = GdKirieTrayEventaAdapter.Attach(_eventa.Context);
            return Task.FromResult(_trayAdapter.Expose(_sharedItem));
        });
        _kirie.IpcError += GD.PushError;

        var initialUrl = _kirie.GetLaunchOption("kirie-web-url").Trim();
        _kirie.CreateWebView(initialUrl.Length == 0 ? "res://src-web/dist/index.html" : initialUrl);
    }

    public override void _ExitTree()
    {
        _pageRegistration?.Dispose();
        _trayAdapter?.Dispose();
        _sharedItem?.Dispose();
        _eventa?.Dispose();
        _kirie?.Dispose();
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(object))]
    [JsonSerializable(typeof(TrayResource))]
    private sealed partial class TrayExampleJsonContext : JsonSerializerContext;
}
