using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApi.Data;
using FinancialApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApi.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class EwalletIncomeController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public EwalletIncomeController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("AddIncome")]
        public async Task<IActionResult> AddIncome([FromBody] GetEwalletIncome request)
        {
            if (request.EwalletID <= 0 || request.UserID <= 0 || request.Amount <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { success = false });
            }

            var ewalletIncome = new EwalletIncome
            {
                EwalletID = request.EwalletID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };

            _context.EwalletIncomes.Add(ewalletIncome);
            await _context.SaveChangesAsync();

            var ewallet = await _context.Ewallets.FindAsync(request.EwalletID);
            if (ewallet == null)
            {
                return NotFound(new { success = false, message = "E-Wallet not found" });
            }

            ewallet.Balance += request.Amount;
            _context.Ewallets.Update(ewallet);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Income added successfully" });
        }

        [HttpPost("AddIncomeWithBank")]
        public async Task<IActionResult> AddIncomeWithBank([FromBody] GetEwalletIncomeWithBank request)
        {
            if (request.EwalletID <= 0 || request.UserID <= 0 || request.Amount <= 0 || request.BankID <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { success = false });
            }

            var ewalletIncome = new EwalletIncome
            {
                EwalletID = request.EwalletID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };

            _context.EwalletIncomes.Add(ewalletIncome);
            await _context.SaveChangesAsync();

            var ewallet = await _context.Ewallets.FindAsync(request.EwalletID);
            if (ewallet == null)
            {
                return NotFound(new { success = false, message = "E-Wallet not found" });
            }

            ewallet.Balance += request.Amount;
            _context.Ewallets.Update(ewallet);
            await _context.SaveChangesAsync();

            var bank = await _context.Banks.FindAsync(request.BankID);
            if (bank == null)
            {
                return NotFound(new { success = false, message = "Bank not found" });
            }

            bank.Amount -= request.Amount;
            _context.Banks.Update(bank);
            await _context.SaveChangesAsync();

            var bankSpend = new BankSpend
            {
                BankID = request.BankID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = "15",
                Date = request.Date
            };

            _context.BankSpends.Add(bankSpend);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Income added successfully" });
        }

        [HttpGet("GetIncome")]
        public async Task<IActionResult> GetIncome(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            var incomes = await _context.EwalletIncomes
                .Where(w => w.UserID == userId)
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            if (incomes == null || incomes.Count == 0)
            {
                return NotFound(new { success = false, message = "No incomes found for the given UserID" });
            }

            return Ok(new { success = true, data = incomes });
        }

        [HttpGet("GetUserWeeklyIncomeTypeRatio")]
        public async Task<IActionResult> GetUserWeeklyIncomeTypeRatio(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            DateTime oneWeekAgo = todayMalaysia.AddDays(-7);

            var userIncomes = await _context.EwalletIncomes
                                           .Where(w => w.UserID == userId)
                                           .ToListAsync();

            userIncomes = userIncomes
                          .Where(w => DateTime.Parse(w.Date) >= oneWeekAgo)
                          .ToList();

            if (!userIncomes.Any())
            {
                return NotFound("No income data found for the specified user in the past week.");
            }

            var typeAmountMap = userIncomes
                                .GroupBy(w => w.Type)
                                .Select(g => new
                                {
                                    Type = g.Key,
                                    TotalAmount = g.Sum(w => (decimal)w.Amount)
                                })
                                .ToList();

            var maxType = typeAmountMap
                          .OrderByDescending(t => t.TotalAmount)
                          .FirstOrDefault();

            if (maxType == null)
            {
                return NotFound("No income type data found.");
            }

            decimal totalAmount = typeAmountMap.Sum(t => t.TotalAmount);
            decimal maxTypeAmount = maxType.TotalAmount;
            decimal ratio = maxTypeAmount / totalAmount;

            return Ok(new
            {
                success = true,
                MaxType = maxType.Type,
                MaxTypeAmount = maxTypeAmount,
                Ratio = ratio,
                AllTypes = typeAmountMap.ToDictionary(t => t.Type, t => t.TotalAmount)
            });
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

                decimal dailySpend = await _context.EwalletIncomes
                    .Where(w => w.UserID == userId && w.Date == formattedDate)
                    .SumAsync(w => (decimal)w.Amount);

                results.Add(new { Date = formattedDate, TotalSpend = dailySpend });
            }

            return Ok(new { success = true, data = results });
        }
    }

    public class GetEwalletIncome
    {
        public int EwalletIncomeID { set; get; }
        public int EwalletID { set; get; }
        public int UserID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }
    public class GetEwalletIncomeWithBank
    {
        public int EwalletIncomeID { set; get; }
        public int EwalletID { set; get; }
        public int UserID { set; get; }
        public int BankID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }
}