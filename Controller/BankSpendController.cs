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
}