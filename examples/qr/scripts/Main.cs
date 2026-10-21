using GdKirie.EventaAdapter;
using GdKirie.Qr;
using Godot;

public partial class Main : Node
{
    private KirieClient? _kirie;
    private KirieEventaContextHandle? _eventa;
    private GdKirieQrHost? _qr;

    public override void _Ready()
    {
        var kirieNode = GetNode<Control>("KirieNode");
        _kirie = KirieClient.FromNode(kirieNode);
        if (!_kirie.IsAvailable)
        {
            GD.PushError("Kirie is unavailable on this platform.");
            return;
        }
        _eventa = _kirie.CreateEventaContext(
            GdKirieQr.Register(new KirieEventaJsonRegistry()));
        _qr = GdKirieQr.Attach(_eventa.Context, this, kirieNode);
        _kirie.IpcError += GD.PushError;
        var url = _kirie.GetLaunchOption("kirie-web-url").Trim();
        _kirie.CreateWebView(url.Length == 0 ? "res://src-web/dist/index.html" : url);
    }

    public override void _ExitTree()
    {
        _qr?.Dispose();
        _eventa?.Dispose();
        _kirie?.Dispose();
    }
}
