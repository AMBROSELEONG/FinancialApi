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
    public class BankIncomeController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public BankIncomeController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("AddIncome")]
        public async Task<IActionResult> AddIncome([FromBody] GetBankIncome request)
        {
            if (request.BankID <= 0 || request.UserID <= 0 || request.Amount <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { success = false });
            }

            var bankIncome = new BankIncome
            {
                BankID = request.BankID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };

            _context.BankIncomes.Add(bankIncome);
            await _context.SaveChangesAsync();

            var bank = await _context.Banks.FindAsync(request.BankID);
            if (bank == null)
            {
                return NotFound(new { success = false, message = "E-Wallet not found" });
            }

            bank.Amount += request.Amount;
            _context.Banks.Update(bank);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Income added successfully" });
        }

        [HttpGet("GetIncome")]
        public async Task<IActionResult> GetIncome(int bankId)
        {
            if (bankId <= 0)
            {
                return BadRequest(new { success = false });
            }

            var bankIncome = await _context.BankIncomes
            .Where(x => x.BankID == bankId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();

            if (bankIncome == null || bankIncome.Count == 0)
            {
                return NotFound(new { success = false, message = "No incomes found for the given UserID" });
            }

            return Ok(new { success = true, data = bankIncome });
        }

        [HttpGet("GetUserWeeklyIncomeTypeRatio")]
        public async Task<IActionResult> GetUserWeeklyIncomeTypeRatio(int bankId)
        {
            if (bankId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            DateTime oneWeekAgo = todayMalaysia.AddDays(-7);

            var userIncomes = await _context.BankIncomes
                                           .Where(w => w.BankID == bankId)
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
        public async Task<IActionResult> GetWeeklyIncome(int bankId)
        {
            if (bankId <= 0)
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

                decimal dailySpend = await _context.BankIncomes
                    .Where(w => w.BankID == bankId && w.Date == formattedDate)
                    .SumAsync(w => (decimal)w.Amount);

                results.Add(new { Date = formattedDate, TotalSpend = dailySpend });
            }

            return Ok(new { success = true, data = results });
        }
    }

    public class GetBankIncome
    {
        public int BankIncomeID { set; get; }
        public int BankID { set; get; }
        public int UserID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }


}