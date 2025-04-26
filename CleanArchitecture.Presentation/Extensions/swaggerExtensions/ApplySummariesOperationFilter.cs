using Humanizer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CleanArchitecture.Presentation.Extensions.swaggerExtensions;
//public class ApplySummariesOperationFilter : IOperationFilter
//{
//    public void Apply(OpenApiOperation operation, OperationFilterContext context)
//    {
//        var routePattern = context.ApiDescription.RelativePath;


//        var singularizeName = routePattern.Singularize();
//        var pluralizeName = routePattern.Pluralize();

//        var parameterCount = operation.Parameters.Where(p => p.Name != "version" && p.Name != "api-version").Count();
//        var actionName = ExtractActionNameFromRoute(routePattern);

//        if (IsGetAllAction(actionName))
//        {
//            if (string.IsNullOrEmpty(operation.Summary))
//                operation.Summary = $"Returns all {pluralizeName}";
//        }
//        else if (IsActionName(actionName, "Post", "Create"))
//        {
//            if (string.IsNullOrEmpty(operation.Summary))
//                operation.Summary = $"Creates a {singularizeName}";

//            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
//                operation.Parameters[0].Description = $"A {singularizeName} representation";
//        }
//        else if (IsActionName(actionName, "Read", "Get"))
//        {
//            if (string.IsNullOrEmpty(operation.Summary))
//                operation.Summary = $"Retrieves a {singularizeName} by unique id";

//            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
//                operation.Parameters[0].Description = $"A unique id for the {singularizeName}";
//        }
//        else if (IsActionName(actionName, "Put", "Edit", "Update"))
//        {
//            if (string.IsNullOrEmpty(operation.Summary))
//                operation.Summary = $"Updates a {singularizeName} by unique id";

//            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
//                operation.Parameters[0].Description = $"A {singularizeName} representation";
//        }
//        else if (IsActionName(actionName, "Delete", "Remove"))
//        {
//            if (string.IsNullOrEmpty(operation.Summary))
//                operation.Summary = $"Deletes a {singularizeName} by unique id";

//            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
//                operation.Parameters[0].Description = $"A unique id for the {singularizeName}";
//        }

//        #region Local Functions

//        // استخراج نام اکشن از الگوی مسیر
//        string ExtractActionNameFromRoute(string routePattern)
//        {
//            // مثلا فرض کنید مسیرها به شکل "/api/resource/action" هستند
//            var segments = routePattern?.Split('/');
//            return segments?.LastOrDefault() ?? string.Empty;
//        }

//        bool IsGetAllAction(string actionName)
//        {
//            foreach (var name in new[] { "Get", "Read", "Select" })
//            {
//                if ((actionName.Equals(name, StringComparison.OrdinalIgnoreCase) && parameterCount == 0) ||
//                    actionName.Equals($"{name}All", StringComparison.OrdinalIgnoreCase) ||
//                    actionName.Equals($"{name}{pluralizeName}", StringComparison.OrdinalIgnoreCase) ||
//                    actionName.Equals($"{name}All{singularizeName}", StringComparison.OrdinalIgnoreCase) ||
//                    actionName.Equals($"{name}All{pluralizeName}", StringComparison.OrdinalIgnoreCase))
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        bool IsActionName(string actionName, params string[] names)
//        {
//            foreach (var name in names)
//            {
//                if (actionName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
//                    actionName.Equals($"{name}ById", StringComparison.OrdinalIgnoreCase) ||
//                    actionName.Equals($"{name}{singularizeName}", StringComparison.OrdinalIgnoreCase) ||
//                    actionName.Equals($"{name}{singularizeName}ById", StringComparison.OrdinalIgnoreCase))
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        #endregion
//    }
//}
public class ApplySummariesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var routePattern = context.ApiDescription.RelativePath;
        var httpMethod = context.ApiDescription.HttpMethod?.ToUpperInvariant();

        var segments = routePattern.Split('/');
        var lastSegment = segments.LastOrDefault();
        var secondLastSegment = segments.Length > 1 ? segments[segments.Length - 2] : null;


        var action = lastSegment.Contains("{") ? secondLastSegment : lastSegment;
        var resource = segments.Length > 1 ? segments[segments.Length - 2] : segments.FirstOrDefault();

        var singularizeName = resource.Singularize();
        var pluralizeName = resource.Pluralize();

        var parameterCount = operation.Parameters.Count(p => p.Name != "version" && p.Name != "api-version");
        if (httpMethod == "GET")
        {
            if (IsGetAllAction(action))
            {
                if (string.IsNullOrEmpty(operation.Summary))
                    operation.Summary = $"Returns all {pluralizeName}";
            }
            else
            {
                if (string.IsNullOrEmpty(operation.Summary))
                    operation.Summary = $"Retrieves a {singularizeName} by unique id";

                if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
                    operation.Parameters[0].Description = $"A unique id for the {singularizeName}";
            }
        }
        else if (httpMethod == "POST")
        {
            if (string.IsNullOrEmpty(operation.Summary))
                operation.Summary = $"Creates a {singularizeName}";

            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
                operation.Parameters[0].Description = $"A {singularizeName} representation";
        }
        else if (httpMethod == "PUT")
        {
            if (string.IsNullOrEmpty(operation.Summary))
                operation.Summary = $"Updates a {singularizeName}";

            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
                operation.Parameters[0].Description = $"A {singularizeName} representation";
        }
        else if (httpMethod == "DELETE")
        {
            if (string.IsNullOrEmpty(operation.Summary))
                operation.Summary = $"Deletes a {singularizeName}";

            if (operation.Parameters.Count > 0 && string.IsNullOrEmpty(operation.Parameters[0].Description))
                operation.Parameters[0].Description = $"A unique id for the {singularizeName}";
        }

        #region Local Functions

        bool IsGetAllAction(string actionName)
        {
            // اگر متدی مانند "GetAll" یا "Get" بدون پارامتر باشد
            return actionName.Equals("GetAll", StringComparison.OrdinalIgnoreCase) || parameterCount == 0;
        }

        #endregion
    }
}








