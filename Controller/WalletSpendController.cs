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
    public class WalletSpendController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public WalletSpendController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("AddSpend")]
        public async Task<IActionResult> AddSpend([FromBody] GetWalletSpend request)
        {
            if (request.WalletID <= 0 || request.UserID <= 0 || request.Amount <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { success = false });
            }

            var walletSpend = new WalletSpend
            {
                WalletID = request.WalletID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };

            _context.WalletSpends.Add(walletSpend);
            await _context.SaveChangesAsync();

            var wallet = await _context.Wallets.FindAsync(request.WalletID);
            if (wallet == null)
            {
                return NotFound(new { success = false, message = "Wallet not found" });
            }

            wallet.Balance -= request.Amount;
            _context.Wallets.Update(wallet);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Spend added successfully" });
        }

        [HttpPost("AddSpendWithBank")]
        public async Task<IActionResult> AddSpendWithBank([FromBody] GetWalletSpendWithBank request)
        {
            if (request.WalletID <= 0 || request.UserID <= 0 || request.Amount <= 0 || request.BankID <= 0 || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Date))
            {
                return BadRequest(new { success = false });
            }

            var walletSpend = new WalletSpend
            {
                WalletID = request.WalletID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = request.Type,
                Date = request.Date
            };

            _context.WalletSpends.Add(walletSpend);
            await _context.SaveChangesAsync();

            var wallet = await _context.Wallets.FindAsync(request.WalletID);
            if (wallet == null)
            {
                return NotFound(new { success = false, message = "Wallet not found" });
            }

            wallet.Balance -= request.Amount;
            _context.Wallets.Update(wallet);
            await _context.SaveChangesAsync();

            var bank = await _context.Banks.FindAsync(request.BankID);
            if (bank == null)
            {
                return NotFound(new { success = false, message = "Bank not found" });
            }

            bank.Amount += request.Amount;
            _context.Banks.Update(bank);
            await _context.SaveChangesAsync();

            var bankIncome = new BankIncome
            {
                BankID = request.BankID,
                UserID = request.UserID,
                Amount = request.Amount,
                Type = "10",
                Date = request.Date
            };

            _context.BankIncomes.Add(bankIncome);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Spend added successfully" });
        }

        [HttpGet("GetSpend")]
        public async Task<IActionResult> GetSpend(int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }
            var spends = await _context.WalletSpends
            .Where(w => w.UserID == userId)
            .OrderByDescending(w => w.Date)
            .ToListAsync();

            if (spends == null || spends.Count == 0)
            {
                return NotFound(new { success = false, message = "No banks found for the given UserID" });
            }
            return Ok(new { success = true, data = spends });
        }
    }
    public class GetWalletSpend
    {
        public int WalletIncomeID { set; get; }
        public int WalletID { set; get; }
        public int UserID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }

    public class GetWalletSpendWithBank
    {
        public int WalletSpendID { set; get; }
        public int WalletID { set; get; }
        public int UserID { set; get; }
        public int BankID { set; get; }
        public float Amount { set; get; }
        public string Type { set; get; }
        public string Date { set; get; }
    }
}