using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using FinancialApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinancialApi.Models;

namespace FinancialApi.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public UserController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("getUser")]
        public async Task<IActionResult> GetUser([FromBody] GetUserRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName) && string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Password) && string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { success = false });
            }

            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                Password = request.Password,
                Token = request.Token,
                PublicKey = request.PublicKey,
                Signature = request.Signature
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true});
        }

        [HttpPost("CheckUserName")]
        public async Task<IActionResult> CheckUserName([FromBody] CheckUserNameRequest request)
        {
            if (string.IsNullOrEmpty(request.UserName))
            {
                return BadRequest("UserName is required");
            }

            var checkUserName = await _context.Users
            .Where(v => v.UserName == request.UserName)
            .FirstOrDefaultAsync();

            if (checkUserName != null)
            {
                return BadRequest(new { success = false });
            }

            return Ok(new { success = true });
        }

        [HttpPost("CheckEmail")]
        public async Task<IActionResult> CheckEmail([FromBody] CheckEmailRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required");
            }

            var checkEmail = await _context.Users
           .Where(v => v.Email == request.Email)
           .FirstOrDefaultAsync();

            if (checkEmail != null)
            {
                return BadRequest(new { success = false });
            }

            return Ok(new { success = true });
        }

        [HttpPost("SignIn")]
        public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) && string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Data is required");
            }

            var checkUserData = await _context.Users
            .Where(v => v.Email == request.Email && v.Password == request.Password)
            .FirstOrDefaultAsync();

            if (checkUserData == null)
            {
                return BadRequest(new { success = false });
            }

            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            checkUserData.LastLogin = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            _context.Users.Update(checkUserData);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, userID = checkUserData.UserID, userName = checkUserData.UserName });
        }

        [HttpPost("Reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Password is required.");
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(v => v.Email == request.Email);

            if (existingUser == null)
            {
                return NotFound("Email not found.");
            }

            existingUser.Password = request.Password;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while resetting the password.");
            }
        }

        [HttpGet("GetUserData")]
        public async Task<IActionResult> GetUserData([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var userData = await _context.Users
                .Where(u => u.UserID == userId)
                .Select(u => new
                {
                    u.UserID,
                    u.UserName,
                    u.Email,
                    u.Password
                })
                .FirstOrDefaultAsync();

            if (userData == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                userData = new
                {
                    userData.UserID,
                    userData.UserName,
                    userData.Email,
                    userData.Password
                }
            });
        }

        [HttpPost("UpdateUserData")]
        public async Task<IActionResult> UpdateUserData([FromBody] UpdateUserRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.Password = request.Password;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User data updated successfully" });
        }

        [HttpPost("FingerPrint")]
        public async Task<IActionResult> FingerPrint([FromBody] FingerPrintRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            user.FingerPrint = request.FingerPrint;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "User data updated successfully" });
        }

        [HttpGet("GetFingerPrint")]
        public async Task<IActionResult> GetFingerPrint([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var userData = await _context.Users
                .Where(u => u.UserID == userId)
                .Select(u => new
                {
                    u.FingerPrint
                })
                .FirstOrDefaultAsync();

            if (userData == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                userData = new
                {
                    userData.FingerPrint
                }
            });
        }

        [HttpPost("UpdatePublicKey")]
        public async Task<IActionResult> UpdatePublicKey([FromBody] PublicKeyRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            user.PublicKey = request.PublicKey;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "PublicKey data updated successfully" });
        }

        [HttpPost("UpdateSignature")]
        public async Task<IActionResult> UpdateSignature([FromBody] SignatureRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            user.Signature = request.Signature;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Signature data updated successfully" });
        }

        [HttpGet("GetSignInData")]
        public async Task<IActionResult> GetSignInData([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var userData = await _context.Users
                .Where(u => u.UserID == userId)
                .Select(u => new
                {
                    u.Email,
                    u.Password
                })
                .FirstOrDefaultAsync();

            if (userData == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                userData = new
                {
                    userData.Email,
                    userData.Password
                }
            });
        }

        [HttpPost("UpdateToken")]
        public async Task<IActionResult> UpdateToken([FromBody] UpdateTokenRequest request)
        {
            if (request.UserId <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.UserId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            user.Token = request.Token;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Token data updated successfully" });
        }

    }

    public class GetUserRequest
    {
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Token { get; set; }
        public required string PublicKey { get; set; }
        public required string Signature { get; set; }
    }

    public class CheckUserNameRequest
    {
        public required string UserName { get; set; }
    }

    public class CheckEmailRequest
    {
        public required string Email { get; set; }
    }

    public class SignInRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class ResetRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class UpdateUserRequest
    {
        public int UserId { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class FingerPrintRequest
    {
        public int UserId { get; set; }
        public bool FingerPrint { get; set; }
    }

    public class PublicKeyRequest
    {
        public int UserId { get; set; }
        public required string PublicKey { get; set; }
    }

    public class SignatureRequest
    {
        public int UserId { get; set; }
        public required string Signature { get; set; }
    }

    public class UpdateTokenRequest
    {
        public int UserId { get; set; }
        public required string Token { get; set; }
        
    }
}