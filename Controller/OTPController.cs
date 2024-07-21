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
    public class OTPController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly SmtpClient _smtpClient;

        public OTPController(ApplicationDBContext context, SmtpClient smtpClient)
        {
            _context = context;
            _smtpClient = smtpClient;
        }

        [HttpPost("SendOTP")]
        public async Task<IActionResult> SendOTP([FromBody] SendOTPRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            var existingOTP = _context.VerifyOTPs.FirstOrDefault(v => v.Email == request.Email);
            var otpCode = GenerateOTP();
            if (existingOTP != null)
            {
                existingOTP.OTP = otpCode;
            }
            else
            {
                var verifyOTP = new VerifyOTP
                {
                    Email = request.Email,
                    OTP = otpCode,
                };

                _context.VerifyOTPs.Add(verifyOTP);
            }

            await _context.SaveChangesAsync();

            var mailMessage = new MailMessage("ambroseleong04@gmail.com", request.Email)
            {
                Subject = "Your OTP Code",
                Body = $"Your OTP code is {otpCode}"
            };
            await _smtpClient.SendMailAsync(mailMessage);

            return Ok("OTP sent successfully.");
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [HttpPost("VerifyOTP")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.OTP))
            {
                return BadRequest("Email and OTP are required.");
            }

            var verifyOTP = await _context.VerifyOTPs
                .Where(v => v.Email == request.Email && v.OTP == request.OTP)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verifyOTP == null)
            {
                return BadRequest(new { success = false });
            }
            Console.WriteLine($"OTP for {request.Email} verified successfully.");
            return Ok(new { success = true });
        }
    }

    public class SendOTPRequest
    {
        public string Email { get; set; }
    }

    public class VerifyOTPRequest
    {
        public string Email { get; set; }
        public string OTP { get; set; }
    }
}