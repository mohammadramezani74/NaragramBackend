using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Application.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Persistence.Middlewares
{
    public class CustomExceptionHandlerMiddleware(RequestDelegate next, IHostingEnvironment env, ILogger<CustomExceptionHandlerMiddleware>logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<CustomExceptionHandlerMiddleware> _logger=logger;
        private readonly IHostingEnvironment _env=env;
        public async Task Invoke(HttpContext context)
        {
            string message = string.Empty;
            HttpStatusCode httpStatusCode = HttpStatusCode.InternalServerError;
            OperationResult apiStatusCode = new OperationResult().Failed("خطای سرور");

            try
            {
                await _next(context);
            }
            catch (ValidationException validationException)  
            {
                _logger.LogError(validationException, "Validation error occurred.");

                var errors = validationException.Errors.SelectMany(v => v.Value).ToList();

               
                var response = new
                {
                    status = 400,
                    message = errors
                };
                message = JsonConvert.SerializeObject(response);
                await context.Response.WriteAsync(message);

            }
            catch (AppException exception)
            {
                _logger.LogError(exception, exception.Message);
                httpStatusCode = exception.HttpStatusCode;
                apiStatusCode = exception.ApiStatusCode;

                if (_env.IsDevelopment())
                {
                    var dic = new Dictionary<string, string>
                    {
                        ["Exception"] = exception.Message,
                        ["StackTrace"] = exception?.StackTrace??string.Empty,
                    };
                    if (exception?.InnerException != null)
                    {
                        dic.Add("InnerException.Exception", exception.InnerException.Message);
                        dic.Add("InnerException.StackTrace", exception?.InnerException?.StackTrace??"");
                    }
                    if (exception?.AdditionalData != null)
                        dic.Add("AdditionalData", JsonConvert.SerializeObject(exception.AdditionalData));

                    message = JsonConvert.SerializeObject(dic);
                }
                else
                {
                    message = exception.Message;
                }
                await WriteToResponseAsync();
            }
            catch (SecurityTokenExpiredException exception)
            {
                _logger.LogError(exception, exception.Message);
                SetUnAuthorizeResponse(exception);
                await WriteToResponseAsync();
            }
            catch (UnauthorizedAccessException exception)
            {
                httpStatusCode = HttpStatusCode.Forbidden;
                _logger.LogError(exception, exception.Message);
                SetUnAuthorizeResponse(exception);
                await WriteToResponseAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, exception.Message);

                if (_env.IsDevelopment())
                {
                    var dic = new Dictionary<string, string>
                    {
                        ["Exception"] = exception.Message,
                        ["StackTrace"] = exception?.StackTrace??string.Empty,
                    };
                    message = JsonConvert.SerializeObject(dic);
                }
                await WriteToResponseAsync();
            }

            async Task WriteToResponseAsync()
            {
                if (context.Response.HasStarted)
                    throw new InvalidOperationException("The response has already started, the http status code middleware will not be executed.");
                
                var result = apiStatusCode;
                if (_env.IsDevelopment())
                {
                    result.Message += message;
                }
                    var json = JsonConvert.SerializeObject(result);

                context.Response.StatusCode = (int)httpStatusCode;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(json);
            }

            void SetUnAuthorizeResponse(Exception exception)
            {

                apiStatusCode = new OperationResult().Forbiden("You Are Not Access To This Method. ");
                if (_env.IsDevelopment())
                {
                    var dic = new Dictionary<string, string>
                    {
                        ["Exception"] = exception.Message,
                        ["StackTrace"] = exception?.StackTrace??""
                    };
                    if (exception is SecurityTokenExpiredException tokenException)
                        dic.Add("Expires", tokenException.Expires.ToString());

                    message = JsonConvert.SerializeObject(dic);
                }
            }
        }
    }
}
