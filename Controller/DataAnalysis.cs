using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApi.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataAnalysis : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public DataAnalysis(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet("TotalSpend")]
        public async Task<IActionResult> GetSpend(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            string formattedDate = todayMalaysia.ToString("yyyy-MM-dd");
            decimal totalSpend = await _context.WalletSpends
               .Where(w => w.UserID == userId && w.Date == formattedDate)
               .SumAsync(w => (decimal)w.Amount);

            totalSpend += await _context.EwalletSpends
               .Where(e => e.UserID == userId && e.Date == formattedDate)
               .SumAsync(e => (decimal)e.Amount);

            totalSpend += await _context.BankSpends
                .Where(b => b.UserID == userId && b.Date == formattedDate)
                .SumAsync(b => (decimal)b.Amount);

            return Ok(new { success = true, data = totalSpend });
        }

        [HttpGet("TotalIncome")]
        public async Task<IActionResult> GetIncome(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            string formattedDate = todayMalaysia.ToString("yyyy-MM-dd");
            decimal totalSpend = await _context.WalletIncomes
               .Where(w => w.UserID == userId && w.Date == formattedDate)
               .SumAsync(w => (decimal)w.Amount);

            totalSpend += await _context.EwalletIncomes
               .Where(e => e.UserID == userId && e.Date == formattedDate)
               .SumAsync(e => (decimal)e.Amount);

            totalSpend += await _context.BankIncomes
                .Where(b => b.UserID == userId && b.Date == formattedDate)
                .SumAsync(b => (decimal)b.Amount);

            return Ok(new { success = true, data = totalSpend });
        }

        [HttpGet("WeeklySpend")]
        public async Task<IActionResult> GetWeeklySpend(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            var results = new List<object>();

            for (int i = 0; i < 7; i++)
            {
                DateTime targetDate = todayMalaysia.AddDays(-i);
                string formattedDate = targetDate.ToString("yyyy-MM-dd");

                decimal dailySpend = await _context.WalletSpends
                    .Where(w => w.UserID == userId && w.Date == formattedDate)
                    .SumAsync(w => (decimal)w.Amount);

                dailySpend += await _context.EwalletSpends
                    .Where(e => e.UserID == userId && e.Date == formattedDate)
                    .SumAsync(e => (decimal)e.Amount);

                dailySpend += await _context.BankSpends
                    .Where(b => b.UserID == userId && b.Date == formattedDate)
                    .SumAsync(b => (decimal)b.Amount);

                results.Add(new { Date = formattedDate, TotalSpend = dailySpend });
            }

            return Ok(new { success = true, data = results });
        }

        [HttpGet("WeeklyIncome")]
        public async Task<IActionResult> GetWeeklyIncome(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            var results = new List<object>();

            for (int i = 0; i < 7; i++)
            {
                DateTime targetDate = todayMalaysia.AddDays(-i);
                string formattedDate = targetDate.ToString("yyyy-MM-dd");

                decimal dailySpend = await _context.WalletIncomes
                    .Where(w => w.UserID == userId && w.Date == formattedDate)
                    .SumAsync(w => (decimal)w.Amount);

                dailySpend += await _context.EwalletIncomes
                    .Where(e => e.UserID == userId && e.Date == formattedDate)
                    .SumAsync(e => (decimal)e.Amount);

                dailySpend += await _context.BankIncomes
                    .Where(b => b.UserID == userId && b.Date == formattedDate)
                    .SumAsync(b => (decimal)b.Amount);

                results.Add(new { Date = formattedDate, TotalSpend = dailySpend });
            }

            return Ok(new { success = true, data = results });
        }

        [HttpGet("GetPercentage")]
        public async Task<IActionResult> GetPercentage([FromQuery] int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var bankData = await _context.Banks
                .Where(b => b.UserID == userID)
                .GroupBy(b => b.UserID)
                .Select(g => new
                {
                    UserID = g.Key,
                    TotalBalance = g.Sum(b => b.Amount)
                }).FirstOrDefaultAsync();

            var bankBalance = bankData?.TotalBalance ?? 0;

            var ewalletData = await _context.Ewallets
                .Where(e => e.UserID == userID)
                .Select(e => new
                {
                    e.Balance
                })
                .FirstOrDefaultAsync();

            var ewalletBalance = ewalletData?.Balance ?? 0;

            var walletData = await _context.Wallets
                .Where(w => w.UserID == userID)
                .Select(w => new
                {
                    w.Balance
                })
                .FirstOrDefaultAsync();

            var walletBalance = walletData?.Balance ?? 0;

            var totalBalance = bankBalance + ewalletBalance + walletBalance;

            var bankPercentage = totalBalance > 0 ? (bankBalance / totalBalance) * 100 : 0;
            var walletPercentage = totalBalance > 0 ? (walletBalance / totalBalance) * 100 : 0;
            var ewalletPercentage = totalBalance > 0 ? (ewalletBalance / totalBalance) * 100 : 0;

            return Ok(new
            {
                success = true,
                total = new
                {
                    totalBalance,
                    bankBalance,
                    ewalletBalance,
                    walletBalance,
                    bankPercentage,
                    walletPercentage,
                    ewalletPercentage
                }
            });
        }
    }
}