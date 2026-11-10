using RomaniaPulse.Web.Services;
using RomaniaPulse.Web.Sources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient(UpstreamHttp.ClientName, UpstreamHttp.Configure);
builder.Services.AddSingleton<IIndicatorSource, EurostatSource>();
builder.Services.AddSingleton<IIndicatorSource, BnrFxSource>();
builder.Services.AddSingleton<ISnapshotStore, FileSnapshotStore>();
builder.Services.AddSingleton<IndicatorService>();
builder.Services.AddHostedService<WarmupService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Strict by default: scripts and styles only from this site and the pinned CDN, nothing inline.
const string Cdn = "https://cdn.jsdelivr.net";
var contentSecurityPolicy = string.Join("; ",
    "default-src 'self'",
    $"script-src 'self' {Cdn}",
    $"style-src 'self' {Cdn}",
    $"font-src 'self' {Cdn}",
    $"img-src 'self' data: {Cdn}",
    "connect-src 'self'",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'self'",
    "frame-ancestors 'none'");

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.ContentSecurityPolicy = contentSecurityPolicy;
    headers.XContentTypeOptions = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

await app.RunAsync();
