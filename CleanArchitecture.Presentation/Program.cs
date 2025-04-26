using CleanArchitecture.Application;
using CleanArchitecture.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Persistence.Extensions;
using CleanArchitecture.Infrastructure.Persistence.Middlewares;
using CleanArchitecture.Presentation;
using CleanArchitecture.Presentation.Extensions;
using CleanArchitecture.Presentation.Extensions.swaggerExtensions;
using Prometheus;
using Serilog;
using System.Reflection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.Extensions.FileProviders;
using CleanArchitecture.Application.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Host.UseSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen();
builder.Services.AddEndpointsApiExplorer();
builder.Services.ConfigureOptions<SwaggerConfigurationExtensions>();
builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
builder.Services.AddMemoryCache();
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval=TimeSpan.FromSeconds(120);
});



builder.Services.RegisterApplicationServices()
                .RegisterInfraStructureServices(builder.Configuration)
                .RegisterPersistenceServices(builder.Configuration)
                .RegisterPresentationServices(builder.Configuration);

builder.Services.AddCors(options => options.AddPolicy("CorsPolicy",
    builder =>
    {
        builder
        .SetIsOriginAllowed(origin => true)
        .AllowAnyHeader()
        .AllowAnyMethod()
       .AllowCredentials();
    }));


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //dbContext.Database.Migrate();
    await SeedDataExtensions.InitializeRoles(scope.ServiceProvider);
}
app.UseMiddleware<CustomExceptionHandlerMiddleware>();
app.MapEndpoints();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            var url = $"/swagger/{description.GroupName}/swagger.json";
            var name = description.GroupName.ToUpperInvariant();
            options.SwaggerEndpoint(url, name);
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        }

    });
    app.UseSwaggerUI();


app.UseHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.UseHealthChecksUI(x =>
          {
              x.UIPath = "/healthui";
              x.ApiPath = "/healthuiApi";
          });
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<NaraHub>("/hubs/naraHub");
app.MapControllers();
app.UseMetricServer();
app.UseHttpMetrics();

app.Run();
