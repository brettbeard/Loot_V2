using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Loot_V2;

public sealed partial class PlaidLinkWindow : WindowEx
{
    private readonly TaskCompletionSource<string?> _tcs = new();
    private readonly string _linkToken;

    public Task<string?> PublicTokenTask => _tcs.Task;

    public PlaidLinkWindow(string linkToken)
    {
        _linkToken = linkToken;
        InitializeComponent();
        LinkWebView.Loaded += OnWebViewLoaded;
        Closed += (_, _) => _tcs.TrySetResult(null);
    }

    private async void OnWebViewLoaded(object sender, RoutedEventArgs e)
    {
        await LinkWebView.EnsureCoreWebView2Async();

        // NavigateToString gives the page a null origin which Plaid Link's iframe rejects.
        // Map a virtual HTTPS hostname to a temp folder so the page has a valid origin.
        var tempDir = Path.GetTempPath();
        LinkWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "plaid-link.local", tempDir, CoreWebView2HostResourceAccessKind.Allow);

        var htmlFile = $"plaid-link-{Guid.NewGuid():N}.html";
        await File.WriteAllTextAsync(Path.Combine(tempDir, htmlFile), BuildLinkHtml(_linkToken));

        LinkWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        LinkWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
        LinkWebView.CoreWebView2.Navigate($"https://plaid-link.local/{htmlFile}");
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var json = args.TryGetWebMessageAsString();
        var message = json is not null ? JsonSerializer.Deserialize<PlaidLinkMessage>(json) : null;
        _tcs.TrySetResult(message?.Type == "success" ? message.PublicToken : null);
        DispatcherQueue.TryEnqueue(Close);
    }

    private static string BuildLinkHtml(string linkToken) => $$"""
        <!DOCTYPE html>
        <html>
        <head>
          <meta charset="utf-8">
          <script src="https://cdn.plaid.com/link/v2/stable/link-initialize.js"></script>
        </head>
        <body>
        <script>
          var handler = Plaid.create({
            token: '{{linkToken}}',
            onSuccess: function(public_token, metadata) {
              window.chrome.webview.postMessage(JSON.stringify({type:'success',public_token:public_token}));
            },
            onExit: function(err, metadata) {
              window.chrome.webview.postMessage(JSON.stringify({type:'exit'}));
            }
          });
          handler.open();
        </script>
        </body>
        </html>
        """;

    private record PlaidLinkMessage(
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("public_token")] string? PublicToken);
}
