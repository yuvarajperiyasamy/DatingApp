using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using API.Entities;
using API.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text;
using API.DTOs;
using API.Interfaces;

namespace API.Controllers
{
    public class AccountController : BaseApiController 
    {
       private readonly DataContext _context;
       private readonly ITokenService _tokenService;
       public AccountController(DataContext context,ITokenService tokenService) 
       {
           _tokenService = tokenService;
            _context = context;
       }

       [HttpPost("register")]
       public async Task<ActionResult<UserDto>> Register(RegisterDto registerdto)
       {
           if (await UserExists(registerdto.Username)) return BadRequest("UserName is taken");
           using var hmac = new System.Security.Cryptography.HMACSHA512();

           var user = new AppUser{
               UserName = registerdto.Username.ToLower(),
               PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerdto.Password)),
               PasswordSalt = hmac.Key
           };

           _context.Users.Add(user);
           await _context.SaveChangesAsync();

           return new UserDto
           {
               Username = user.UserName,
               Token = _tokenService.CreateToken(user)
           };
       }

       [HttpPost("login")]
       public async Task<ActionResult<UserDto>> Login(LoginDto logindto)
       {
           var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == logindto.Username);
           if (user==null) return Unauthorized("Invalid UserName");

            using var hmac = new System.Security.Cryptography.HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(logindto.Password));
            for(int i=0; i< computedHash.Length; i++)
            {
                if(computedHash[i]!=user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }
            
           return new UserDto
           {
               Username = user.UserName,
               Token = _tokenService.CreateToken(user)
           };
       }

       private async Task<bool> UserExists(string username)
       {
           return await _context.Users.AnyAsync(x => x.UserName == username.ToLower());
       }
    }
}