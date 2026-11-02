using Microsoft.Extensions.DependencyInjection;
using MoXia.Control;
using MoXia.Control.Extensions;
using MoXia.Gtk;
using MoXia.Gtk.Extensions;
using MoXia.Rcl.Extensions;

var services = new ServiceCollection();
services.AddGtkBlazorWebView();
#if DEBUG
services.AddBlazorWebViewDeveloperTools();
#endif

services.AddMasaBlazorConfig();

services.AddSerilogConfig(SQLiteConstants.LogDatabasePath);

services.AddSqlSugarConfig(SQLiteConstants.ConnectionString, SQLiteConstants.PrivacyConnectionString, SQLiteConstants.LogConnectionString);

services.AddDependencyInjection();
services.AddMcpControl();

var sp = services.BuildServiceProvider();

var mcpControl = sp.GetRequiredService<McpControlServer>();
await mcpControl.StartAsync();
try
{
    var app = new App(sp);
    return app.Run(args);
}
finally
{
    await mcpControl.StopAsync();
}
