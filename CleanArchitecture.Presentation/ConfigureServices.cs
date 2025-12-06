using Asp.Versioning;
using CleanArchitecture.Application.Common.Models;
using CleanArchitecture.Domain.Entities.Identity;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.SystemConsole.Themes;
using System.Collections.ObjectModel;
using System.Text;
namespace CleanArchitecture.Presentation
{
    public static class ConfigureServices
    {
        public static IServiceCollection RegisterPresentationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecksUI(p =>
            {
                p.AddHealthCheckEndpoint("CleanDbHealthChecks", "health");
                p.SetEvaluationTimeInSeconds(1440);
            }).AddInMemoryStorage();

               services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1);
                options.ApiVersionReader = new UrlSegmentApiVersionReader(); 
            })
                .AddApiExplorer(o=>
                {
                    o.GroupNameFormat = "'v'V";

                    o.SubstituteApiVersionInUrl = true;
                });

            services.Configure<ApplicationSettings>(configuration);
            services.AddIdentityServices();
            services.AddLogingInternal(configuration);
            services.AddAuthenticationInternal(configuration);
            return services;
        }
        private static IServiceCollection AddIdentityServices(this IServiceCollection services)
        {
            services.AddIdentity<User, Role>(o =>
            {

                o.Password.RequireNonAlphanumeric=false;
                o.Password.RequireUppercase = false;
                o.Password.RequireLowercase = false;
            })
                    .AddEntityFrameworkStores<ApplicationDbContext>()
                    .AddDefaultTokenProviders();

            return services;
        }
        private static IServiceCollection AddAuthenticationInternal(
                                                                    this IServiceCollection services,
                                                                    IConfiguration configuration)
        {
          var  appSettingsSection = configuration.GetSection("Jwt");
            var jwtSettings = appSettingsSection.Get<JwtInformation>();
            services.AddAuthorization();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })

              .AddJwtBearer(o =>
                {

                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings!.Secret)),
                        ValidIssuer = jwtSettings!.Issuer,
                        ValidAudience = jwtSettings!.Audience,
                        ClockSkew = TimeSpan.Zero,
                        RequireSignedTokens = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        ValidateAudience = true,
                        ValidateIssuer = true,

                    };

                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Path.StartsWithSegments("/hubs/naraHub"))
                            {
                                var jwt = context.Request.Query["access_token"];
                                if (!string.IsNullOrEmpty(jwt))
                                {
                                    context.Token = jwt;
                                }
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;

    }
        
        private static IServiceCollection AddLogingInternal(
                                                                    this IServiceCollection services,
                                                                    IConfiguration configuration)
        {
            var sinkoptions = new MSSqlServerSinkOptions
            {
                TableName = "AppLogs",
                AutoCreateSqlTable = true,

            };
            var colOptions = new ColumnOptions();
            colOptions.Store.Add(StandardColumn.Id);
            //colOptions.Id.DataType = System.Data.SqlDbType.UniqueIdentifier;
            colOptions.AdditionalColumns = new Collection<SqlColumn>
{
    new SqlColumn
    {
        ColumnName="Ip",
        AllowNull=true,
        DataLength=100,
        DataType=System.Data.SqlDbType.NVarChar,
        PropertyName="IpPalce",//restore
    }
};
            var logger = new LoggerConfiguration()
     
      .WriteTo.Console(
          theme: AnsiConsoleTheme.Sixteen,
          restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information) 
                                                                              
      .WriteTo.File("Serilog/Log.txt",
          rollingInterval: RollingInterval.Day,
          rollOnFileSizeLimit: true,
          retainedFileCountLimit: null,
          restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)

      //.WriteTo.MSSqlServer(
      //    connectionString: configuration.GetConnectionString("ApplicationDbContext"),
      //    sinkOptions: sinkoptions,
      //    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)

      .MinimumLevel.Information() 
      .CreateLogger();

           
            Log.Logger = logger;

            return services;
        }
    }
}
