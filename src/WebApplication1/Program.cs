using Patware.PingTrace;
using Patware.PingTrace.Core;
using WebApplication1.Data;
using WebApplication1.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddControllersWithViews()
  .AddJsonOptions(options =>
  {
    // Preserve the existing public property names.
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
  });

builder.Services.AddScoped<IRepository, Repository>();

builder.Services.AddPingTrace(options =>
{
  options.Id = Guid.Parse("32DE64E4-6FB4-4F8B-A2C5-C42BF1313141");
  options.Name = "WebService1";
});

builder.Services.AddPingTraceDestination<DatabaseTraceDestination>();
builder.Services.AddScoped<IDiscovery, WebServiceDiscovery>();

builder.Services.AddHttpClient<PingTraceServiceForMvc>(client =>
{
  string baseUrl = builder.Configuration["WebService:BaseUrl"]
    ?? throw new InvalidOperationException(
      "WebService:BaseUrl is not configured.");

  client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
  client.Timeout = TimeSpan.FromSeconds(30);
});

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler(errorApp =>
  {
    errorApp.Run(async context =>
    {
      context.Response.StatusCode = StatusCodes.Status500InternalServerError;
      await context.Response.WriteAsJsonAsync(new
      {
        Error = "The request could not be completed."
      });
    });
  });
}

app.UseStaticFiles();

app.MapControllers();

app.MapControllerRoute(
  name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();