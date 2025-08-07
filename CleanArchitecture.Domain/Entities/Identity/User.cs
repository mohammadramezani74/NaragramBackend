using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities.Chat;
using CleanArchitecture.Domain.Enums;
using CleanArchitecture.Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Entities.Identity
{
    public class User : IdentityUser<Guid>
    {

        public string? FirsName { get; private set; }
        public string? LastName { get; private set; }
        public string? Avatar { get; private set; }
        public string? bio { get; set; }
        public string? NationalCode { get; set; }
        public int Age { get; }
        public Address? Address { get; private set; }
        public Gender Gender { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDoNotDisturb { get; private set; }
        public DateTimeOffset? LastLoginDate { get; private set; }
        public virtual ICollection<RefreshToken> RefreshTokens { get; internal set; } = new List<RefreshToken>();
        public virtual ICollection<UserAvatar> UserAvatars { get; internal set; } = new List<UserAvatar>();
        public virtual ICollection<FireBaseToken> firebaseTokens { get; internal set; } = new List<FireBaseToken>();



        public User(string userName, int age, string? email, string lastName, string firstName, Gender gender, string? phoneNumber, Address? address,Guid? Id=null,string?bio=null)
        {
            if(Id!=null) { Id = Id.Value; }
            UserName = userName;
            Age = age;
            Email = email;
            FirsName = firstName;
            LastName = lastName;
            Gender = gender;
            PhoneNumber = phoneNumber;
            EmailConfirmed = true;
            PhoneNumberConfirmed = true;
            TwoFactorEnabled = true;
            LockoutEnabled = false;
            AccessFailedCount = 7;
            IsActive = true;
            IsDoNotDisturb = false;
            this.bio = bio;
            if (address is not null)
            {
                Address = new Address(address.City, address.Street, address.PostalCode);
            }
        }
        public void updateNationalCode(int code)
        {
            NationalCode = code.ToString();
        }

        private User()
        {

        }

        public static User Create(string userName, int age, string? email, string lastName, string firstName, Gender gender, string? phoneNumber, Address? address)
           => new User
          (
              userName,
               age,
                email,
              firstName,
              lastName,
               gender,
                phoneNumber,
              address

           );
        public static User CreateWithPhoneNumber(Guid Id,string phonenumber, string lastName, string firstName, Address? address,string?bio)
    => new User
   (
      
       phonenumber,
        0,
         null,
       firstName,
       lastName,
        Gender.Unknown,
         phonenumber,
       address,
       Id,
       bio

    );
        public void SetDoNotDisturb(bool isDoNotDisturb)
        {
            IsDoNotDisturb = isDoNotDisturb;
        }
        public User AddRefreshToken(RefreshToken refreshToken)
        {
            RefreshTokens.Add( refreshToken );
            return this;
        }
        public void SetAvatar(string avatar)
        {
            Avatar= avatar;
        }
        public void SetAddress(string city)
        {
            Address = new Address(city,null,null);
        }
        public void UpdatelastLoginDate()
        {
            LastLoginDate = DateTime.Now;
        
        }

    }
}
