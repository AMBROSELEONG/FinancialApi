using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using FinancialApi.Data;
using Microsoft.AspNetCore.Mvc;
using FinancialApi.Data;
using FinancialApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialApi.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebtController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly SmtpClient _smtpClient;
        public DebtController(ApplicationDBContext context, SmtpClient smtpClient)
        {
            _context = context;
            _smtpClient = smtpClient;

        }

        [HttpPost("CreateDebt")]
        public async Task<IActionResult> CreateDebt([FromBody] Debt request)
        {
            if (request.UserID <= 0 || string.IsNullOrEmpty(request.DebtName) || request.DebtAmount <= 0 || request.Date == default(DateTime) || request.Year <= 0 || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Invalid request");
            }

            var totalAmount = (request.DebtAmount * request.Year) * 12;

            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);

            DateTime initialNextDate = new DateTime(currentDate.Year, currentDate.Month, request.Date.Day);

            if (initialNextDate < currentDate)
            {
                initialNextDate = initialNextDate.AddMonths(1);
            }

            DateTime nextDate;
            if (initialNextDate.Day > DateTime.DaysInMonth(initialNextDate.Year, initialNextDate.Month))
            {
                nextDate = new DateTime(initialNextDate.Year, initialNextDate.Month, DateTime.DaysInMonth(initialNextDate.Year, initialNextDate.Month));
            }
            else
            {
                nextDate = initialNextDate;
            }

            DateTime endDate = request.Date.AddYears(request.Year);
            int monthLeft = ((endDate.Year - nextDate.Year) * 12) + endDate.Month - nextDate.Month;
            if (endDate.Day < nextDate.Day)
            {
                monthLeft--;
            }

            int paidMonths = ((currentDate.Year - request.Date.Year) * 12) + currentDate.Month - request.Date.Month;
            if (currentDate.Day < request.Date.Day)
            {
                paidMonths--;
            }

            double alreadyPaidAmount = paidMonths * request.DebtAmount;

            var debts = new Debt
            {
                UserID = request.UserID,
                DebtName = request.DebtName,
                DebtAmount = request.DebtAmount,
                TotalAmount = totalAmount,
                Cumulative = alreadyPaidAmount,
                Date = request.Date,
                NextDate = nextDate,
                EndDate = endDate,
                Year = request.Year,
                MonthLeft = monthLeft,
                Token = request.Token,
                Email = request.Email
            };

            _context.Debts.Add(debts);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Debt created successfully" });
        }

        [HttpGet("GetDebt")]
        public async Task<IActionResult> GetDebt([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid request" });
            }

            var debts = await _context.Debts
            .Where(d => d.UserID == userId)
            .ToListAsync();

            if (debts == null || debts.Count == 0)
            {
                return NotFound(new { success = false, message = "No debt found" });
            }

            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);

            var debtDetail = debts.Select(d => new
            {
                d.DebtID,
                d.UserID,
                d.DebtName,
                d.DebtAmount,
                d.TotalAmount,
                d.Cumulative,
                d.Date,
                d.NextDate,
                d.EndDate,
                d.Year,
                d.MonthLeft,
                DaysUntilNextDate = (d.NextDate - now).Days
            }).ToList();

            double totalDebtAmount = debts.Sum(d => d.DebtAmount);

            return Ok(new { success = true, data = debtDetail, count = debts.Count, TotalDebtAmount = totalDebtAmount });
        }

        [HttpGet("GetDebtById")]
        public async Task<IActionResult> GetDebtById([FromQuery] int debtId)
        {
            if (debtId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid request" });
            }

            var debt = await _context.Debts
            .FirstOrDefaultAsync(d => d.DebtID == debtId);

            if (debt == null)
            {
                return NotFound(new { success = false, message = "No debt found" });
            }

            bool isPaidOff = debt.Cumulative >= debt.TotalAmount;
            if (isPaidOff)
            {
                await SendDebtPaidOffEmail(debt.Email);
                return Ok(new { success = "Done", data = debt });
            }

            return Ok(new { success = true, data = debt });
        }

        [HttpPost("CompletePayment")]
        public async Task<IActionResult> CompletePayment([FromBody] CompletePaymentRequest request)
        {
            var debt = await _context.Debts.FirstOrDefaultAsync(d => d.DebtID == request.DebtID && d.UserID == request.UserID);
            if (debt == null)
            {
                return NotFound(new { success = false, message = "Debt not found" });
            }

            debt.Cumulative += debt.DebtAmount;

            DateTime nextDate = debt.NextDate.AddMonths(1);
            if (nextDate.Day > DateTime.DaysInMonth(nextDate.Year, nextDate.Month))
            {
                nextDate = new DateTime(nextDate.Year, nextDate.Month, DateTime.DaysInMonth(nextDate.Year, nextDate.Month));
            }
            debt.NextDate = nextDate;

            int monthLeft = ((debt.EndDate.Year - debt.NextDate.Year) * 12) + debt.EndDate.Month - debt.NextDate.Month;
            if (debt.EndDate.Day < debt.NextDate.Day)
            {
                monthLeft--;
            }
            debt.MonthLeft = monthLeft;

            _context.Debts.Update(debt);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Payment completed successfully" });
        }

        [HttpDelete("DeleteDebt")]
        public async Task<IActionResult> DeleteDebt(int debtId)
        {
            if (debtId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid request" });
            }

            var debt = await _context.Debts.FindAsync(debtId);
            if (debt == null)
            {
                return NotFound(new { success = false, message = "No debt found" });
            }

            _context.Debts.Remove(debt);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Debt deleted successfully" });
        }

        private async Task SendDebtPaidOffEmail(string email)
        {
            var mailMessage = new MailMessage("ambroseleong04@gmail.com", email)
            {
                Subject = "Debt Paid Off",
                Body = "Congratulations! \n Your debt has been fully paid off."
            };

            try
            {
                await _smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }

    }
    public class CompletePaymentRequest
    {
        public int DebtID { get; set; }
        public int UserID { get; set; }
    }
}