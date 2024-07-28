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
    public class BankSpendController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public BankSpendController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("AddSpend")]
        public async Task<IActionResult> AddSpend([FromBody] GetBankSpend request)
        {
            if (request.BankID <= 0 || request.UserID <= 0 || request.Amount <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { success = false });
            }

            var bankSpend = new BankSpend
            {
                BankID = request.BankID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };

            _context.BankSpends.Add(bankSpend);
            await _context.SaveChangesAsync();

            var bank = await _context.Banks.FindAsync(request.BankID);
            if (bank == null)
            {
                return NotFound(new { success = false, message = "Bank not found" });
            }

            bank.Amount -= request.Amount;
            _context.Banks.Update(bank);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Income added successfully" });
        }

        [HttpGet("GetSpend")]
        public async Task<IActionResult> GetSpend(int bankId)
        {
            if (bankId <= 0)
            {
                return BadRequest(new { success = false });
            }

            var bankSpend = await _context.BankSpends
            .Where(x => x.BankID == bankId)
            .OrderByDescending(x => x.Date)
            .ToListAsync();

            if (bankSpend == null || bankSpend.Count == 0)
            {
                return NotFound(new { success = false, message = "No spend found for the given UserID" });
            }

            return Ok(new { success = true, data = bankSpend });
        }

        [HttpGet("GetUserWeeklySpendTypeRatio")]
        public async Task<IActionResult> GetUserWeeklySpendTypeRatio(int bankId)
        {
            if (bankId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            TimeZoneInfo myTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kuala_Lumpur");
            DateTime todayMalaysia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, myTimeZone).Date;

            DateTime oneWeekAgo = todayMalaysia.AddDays(-7);

            var userSpends = await _context.BankSpends
                                           .Where(w => w.BankID == bankId)
                                           .ToListAsync();

            userSpends = userSpends
                          .Where(w => DateTime.Parse(w.Date) >= oneWeekAgo)
                          .ToList();

            if (!userSpends.Any())
            {
                return NotFound("No income data found for the specified user in the past week.");
            }

            var typeAmountMap = userSpends
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

        [HttpGet("WeeklySpend")]
        public async Task<IActionResult> GetWeeklySpend(int bankId)
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

                decimal dailySpend = await _context.BankSpends
                    .Where(w => w.BankID == bankId && w.Date == formattedDate)
                    .SumAsync(w => (decimal)w.Amount);

                results.Add(new { Date = formattedDate, TotalSpend = dailySpend });
            }

            return Ok(new { success = true, data = results });
        }

        [HttpPost("BankTransfer")]
        public async Task<IActionResult> Transfer([FromBody] GetBankTransfer request)
        {
            if (request.BankID <= 0 || request.ToBankID <= 0 || request.UserID <= 0 || request.Amount <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest("Invalid input parameters");
            }

            var fromBank = await _context.Banks.FindAsync(request.BankID);
            if (fromBank == null)
            {
                return NotFound("From Bank not found");
            }

            var toBank = await _context.Banks.FindAsync(request.ToBankID);
            if (toBank == null)
            {
                return NotFound("To Bank not found");
            }

            if (fromBank.Amount < request.Amount)
            {
                return BadRequest("Insufficient balance in the from bank");
            }

            fromBank.Amount -= request.Amount;
            _context.Banks.Update(fromBank);
            await _context.SaveChangesAsync();

            toBank.Amount += request.Amount;
            _context.Banks.Update(toBank);
            await _context.SaveChangesAsync();

            var bankSpend = new BankSpend
            {
                BankID = request.BankID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };
            _context.BankSpends.Add(bankSpend);
            await _context.SaveChangesAsync();

            var bankIncome = new BankIncome
            {
                BankID = request.ToBankID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = "12",
                Date = request.Date
            };
            _context.BankIncomes.Add(bankIncome);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
    public class GetBankSpend
    {
        public int BankSpendID { set; get; }
        public int BankID { set; get; }
        public int UserID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }
    public class GetBankTransfer
    {
        public int BankSpendID { set; get; }
        public int BankID { set; get; }
        public int ToBankID { set; get; }
        public int UserID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }
}