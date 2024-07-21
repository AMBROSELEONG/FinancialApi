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