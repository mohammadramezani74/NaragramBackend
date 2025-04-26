using CleanArchitecture.Application.Abstraction.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Behaviours
{
    public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly Stopwatch _timer;
        private readonly ILogger<TRequest> _logger;
        private readonly IApplicationUserManager _currentUserService;
      

        public PerformanceBehaviour(
            ILogger<TRequest> logger,
            IApplicationUserManager currentUserService
           )
        {
            _timer = new Stopwatch();

            _logger = logger;
            _currentUserService = currentUserService;
         
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _timer.Start();

            var response = await next();

            _timer.Stop();

            var elapsedMilliseconds = _timer.ElapsedMilliseconds;

            if (elapsedMilliseconds > 2000)
            {
                var requestName = typeof(TRequest).Name;
                try
                {

              
                var userId = _currentUserService.UserId;
                var userName = string.Empty;

                if (userId != null)
                {
                   var user= await _currentUserService.GetUserBy(userId.Value);
                    userName= user?.UserName;
                }

                _logger.LogWarning($"CleanApplication Long Running Request: {requestName} ({elapsedMilliseconds} milliseconds) {userId} {userName} {request}"
                      );
                }
                catch (Exception)
                {

                 
                }
            }

            return response;
        }

   
    }
}
