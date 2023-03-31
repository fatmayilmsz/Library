﻿using Library.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Library.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly LibraryContext _context;
        public UserController(LibraryContext librarycontext)
        {
            _context = librarycontext;
        }

        [HttpGet,Authorize(Roles = "SuperAdmin")]
        public IActionResult FindUsers()
        {
            var users = _context.Users.ToList();
            return Ok(users);
        }
       


    }
}
