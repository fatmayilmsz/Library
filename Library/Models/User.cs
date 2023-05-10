﻿using Microsoft.AspNetCore.Identity;
using System.Runtime.InteropServices;

namespace Library.Models
{
    public class User 
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public int Role { get; set; }
        public string? Phone { get; set; }

    }
}
