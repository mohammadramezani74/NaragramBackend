using CleanArchitecture.Application.Abstraction;
using CleanArchitecture.Application.Abstraction.Authentication;
using CleanArchitecture.Application.Common.unitOfWork;
using CleanArchitecture.Domain.Entities.Chat;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Notification
{
    public sealed class NotificationService : INotificationService
    {
        private static bool _initialized = false;
        private readonly IApplicationUnitOfWork _applicationUnitOfWork;
        private readonly IApplicationUserManager _userManager;
        public NotificationService(IApplicationUserManager userManager, IApplicationUnitOfWork applicationUnitOfWork)
        {
            if (!_initialized)
            {
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = GoogleCredential.FromFile("adminsdk.json")
                });

                _initialized = true;
            }

            _userManager = userManager;
            _applicationUnitOfWork = applicationUnitOfWork;
        }

        public async Task Send(string token, string Message, string Name)
        {
            var message = new FirebaseAdmin.Messaging.Message()
            {
                Token = token,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = Name,
                    Body = Message,
                    ImageUrl= @"https://cdn.tarhbama.com/1401/12/25/Image/10/filelogo.jpg"
                }
            };
            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            }
            catch (Exception ex)
            {

             
            }
         
        }

        public async Task StoreFCMtoken(string token)
        {
            var myUser = _userManager.UserId!.Value;
        var existToken=  await  _applicationUnitOfWork.FireBaseTokens
                .Where(x=> x.UserId== myUser).FirstOrDefaultAsync();
            if (existToken!=null)
               {
                existToken.update(token) ;
                await _applicationUnitOfWork.SaveChangesAsync();
                return;
            }
          var FCMtoken=  Domain.Entities.Identity.FireBaseToken.CreateUserToken(token, myUser);
            _applicationUnitOfWork.FireBaseTokens.Add(FCMtoken);
           await _applicationUnitOfWork.SaveChangesAsync();
        }
    }
}
