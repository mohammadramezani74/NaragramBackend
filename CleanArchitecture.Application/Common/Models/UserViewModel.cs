using CleanArchitecture.Domain.Entities.Identity;
using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Models
{
    public sealed class UserViewModel
    {
        public Guid Id { get; set; }
        public string? Avatar { get; set; } 
        public string? Name { get; set; }
        public bool IsOnline { get; set; } = false;
        public bool IsSelected { get; set; } = false;


    }
}
