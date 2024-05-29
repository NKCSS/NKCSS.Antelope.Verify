using VerificationExample.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR(options =>
{
    //NOTE: I enable this for debugging, you should probably disable this when rolling out to production.
    options.EnableDetailedErrors = true;
});
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();
app.MapHub<WaxHub>("/waxHub");
app.MapGet($"/{WaxHub.ServerSideScriptName}", () => WaxHub.ServerSideScript);
app.Run();