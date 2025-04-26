using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CleanArchitecture.Presentation.Extensions.swaggerExtensions;

public  class SwaggerConfigurationExtensions:IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public SwaggerConfigurationExtensions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }


    public  void AddSwaggerAndUi( IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(swaggerUI =>
        {
            swaggerUI.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 Docs");
            swaggerUI.SwaggerEndpoint("/swagger/v2/swagger.json", "V2 Docs");
        });
  
        //app.UseReDoc(options =>
        //{
        //    options.SpecUrl("/swagger/v1/swagger.json");
        //    //options.SpecUrl("/swagger/v2/swagger.json");

        //    #region Customizing
        //    //By default, the ReDoc UI will be exposed at "/api-docs"
        //    //options.RoutePrefix = "docs";
        //    //options.DocumentTitle = "My API Docs";

        //    options.EnableUntrustedSpec();
        //    options.ScrollYOffset(10);
        //    options.HideHostname();
        //    options.HideDownloadButton();
        //    options.ExpandResponses("200,201");
        //    options.RequiredPropsFirst();
        //    options.NoAutoAuth();
        //    options.PathInMiddlePanel();
        //    options.HideLoading();
        //    options.NativeScrollbars();
        //    options.DisableSearch();
        //    options.OnlyRequiredInSamples();
        //    options.SortPropsAlphabetically();
        //    #endregion
        //});
    }

    public void Configure(string? name, SwaggerGenOptions options)
    {
      Configure(options);
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            var openApi = new OpenApiInfo
            {
                Title=$"CleanArchiteture v{description.ApiVersion}",
                Version=description.ApiVersion.ToString()
            };
            options.SwaggerDoc(description.GroupName,openApi);

        }
        options.EnableAnnotations();

        options.DescribeAllParametersInCamelCase();
        #region Filters


        //options.OperationFilter<ApplySummariesOperationFilter>();

        #region Add UnAuthorized to Response
        //Add 401 response and security requirements (Lock icon) to actions that need authorization
          options.OperationFilter<AuthorizeCheckOperationFilter>();
        #endregion

        #region Add Jwt Authentication
        #region Old way
        //var securityScheme = new OpenApiSecurityScheme()
        //{
        //    Name = "JWT Authentication",
        //    BearerFormat = "JWT",
        //    Description = "Enter JWT Bearer token Without Bearer",
        //    In = ParameterLocation.Header,
        //    Type = SecuritySchemeType.Http,
        //    Scheme = "bearer",
        //    Reference = new OpenApiReference()
        //    {
        //        Id = JwtBearerDefaults.AuthenticationScheme,
        //        Type = ReferenceType.SecurityScheme
        //    }
        //};
        //options.AddSecurityDefinition(securityScheme.Reference.Id, new OpenApiSecurityScheme()
        //{
        //    Scheme = "Bearer",
        //    Type = SecuritySchemeType.Http,
        //    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        //    Name = "Authorization",
        //    In = ParameterLocation.Header
        //});

        #endregion

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "OAuth2" }
                },
                Array.Empty<string>() //new[] { "readAccess", "writeAccess" }
            }
        });

        //OAuth2Scheme
        options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
        {
            Scheme = "Bearer",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = new Uri("/api/v1/account/LoginSwagger", UriKind.Relative),
                    AuthorizationUrl = new Uri("/api/v1/account/LoginSwagger", UriKind.Relative)
                    //Scopes = new Dictionary<string, string>
                    //{
                    //    { "readAccess", "Access read operations" },
                    //    { "writeAccess", "Access write operations" }
                    //}
                }
            }
        });
        #endregion




        #endregion
    }
}

