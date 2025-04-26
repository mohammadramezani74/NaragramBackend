using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CleanArchitecture.Presentation.Extensions.swaggerExtensions;


public class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
    
        var isAnonymous = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .Any(meta => meta is IAllowAnonymous);

      
        if (isAnonymous)
        {
         
            if (operation.Security == null)
                operation.Security = new List<OpenApiSecurityRequirement>();

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    new string[] { }
                }
            };

            operation.Security.Add(securityRequirement);
        }
    }
}

