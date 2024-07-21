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

            return Ok(new {success = true, data = bankIncome});
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