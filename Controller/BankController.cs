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
    public class BankController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public BankController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("AddBank")]
        public async Task<IActionResult> AddBank([FromBody] AddBankRequest request)
        {
            if (request.UserID <= 0 || string.IsNullOrEmpty(request.BankName) || !float.IsNormal(request.Amount))
            {
                return BadRequest(new { success = false });
            }

            var bank = new Bank
            {
                UserID = request.UserID,
                BankName = request.BankName,
                Amount = request.Amount
            };

            _context.Banks.Add(bank);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpGet("GetBanks")]
        public async Task<IActionResult> GetBanks([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid UserID" });
            }

            var totalBankBalance = await _context.Banks
                    .Where(b => b.UserID == userId)
                    .SumAsync(b => b.Amount);

            var banks = await _context.Banks
                .Where(b => b.UserID == userId)
                .Select(b => new
                {
                    b.BankID,
                    b.BankName,
                    b.Amount,
                    BalancePercentage = totalBankBalance > 0 ? (b.Amount / totalBankBalance) * 100 : 0
                })
                .ToListAsync();

            if (banks == null || banks.Count == 0)
            {
                return NotFound(new { success = false, message = "No banks found for the given UserID" });
            }

            return Ok(new { success = true, data = banks });
        }

        [HttpGet("GetBanksByBankID")]
        public async Task<IActionResult> GetBanksByBankID([FromQuery] int bankId)
        {
            if (bankId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid BankID" });
            }

            var bank = await _context.Banks
            .Where(b => b.BankID == bankId)
            .Select(b => new
            {
                b.BankName,
                b.Amount
            })
            .FirstOrDefaultAsync();

            if (bank == null)
            {
                return NotFound(new { success = false, message = "No banks found for the given BankID" });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    bank.BankName,
                    bank.Amount
                }
            });
        }

        [HttpDelete("DeleteBank")]
        public async Task<IActionResult> DeleteBank(int bankId)
        {
            if (bankId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid BankID" });
            }

            var bank = await _context.Banks.FindAsync(bankId);
            if (bank == null)
            {
                return NotFound(new { success = false, message = "Bank not found" });
            }

            _context.Banks.Remove(bank);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Bank deleted successfully" });
        }

        [HttpGet("GetTotalBalance")]
        public async Task<IActionResult> GetTotalBalance([FromQuery] int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            // 获取银行余额
            var bankData = await _context.Banks
                .Where(b => b.UserID == userID)
                .GroupBy(b => b.UserID)
                .Select(g => new
                {
                    UserID = g.Key,
                    TotalBalance = g.Sum(b => b.Amount)
                }).FirstOrDefaultAsync();

            var bankBalance = bankData?.TotalBalance ?? 0;

            // 获取电子钱包余额
            var ewalletData = await _context.Ewallets
                .Where(e => e.UserID == userID)
                .Select(e => new
                {
                    e.Balance
                })
                .FirstOrDefaultAsync();

            var ewalletBalance = ewalletData?.Balance ?? 0;

            // 获取钱包余额
            var walletData = await _context.Wallets
                .Where(w => w.UserID == userID)
                .Select(w => new
                {
                    w.Balance
                })
                .FirstOrDefaultAsync();

            var walletBalance = walletData?.Balance ?? 0;

            // 计算总余额
            var totalBalance = bankBalance + ewalletBalance + walletBalance;

            // 计算银行余额的比例百分比
            var bankPercentage = totalBalance > 0 ? (bankBalance / totalBalance) * 100 : 0;

            return Ok(new
            {
                success = true,
                total = new
                {
                    totalBalance,
                    bankBalance,
                    ewalletBalance,
                    walletBalance,
                    bankPercentage
                }
            });
        }

        [HttpGet("GetPercent")]
        public async Task<IActionResult> GetPercent([FromQuery] int userID, [FromQuery] int bankID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            // 获取银行余额
            var bankData = await _context.Banks
                .Where(b => b.UserID == userID)
                .GroupBy(b => b.UserID)
                .Select(g => new
                {
                    UserID = g.Key,
                    TotalBalance = g.Sum(b => b.Amount)
                }).FirstOrDefaultAsync();

            var bankBalance = bankData?.TotalBalance ?? 0;

            // 获取电子钱包余额
            var ewalletData = await _context.Ewallets
                .Where(e => e.UserID == userID)
                .Select(e => new
                {
                    e.Balance
                })
                .FirstOrDefaultAsync();

            var ewalletBalance = ewalletData?.Balance ?? 0;

            // 获取钱包余额
            var walletData = await _context.Wallets
                .Where(w => w.UserID == userID)
                .Select(w => new
                {
                    w.Balance
                })
                .FirstOrDefaultAsync();

            var walletBalance = walletData?.Balance ?? 0;

            // 计算总余额
            var totalBalance = bankBalance + ewalletBalance + walletBalance;

            if (totalBalance == 0)
            {
                return Ok(new
                {
                    success = true,
                    total = new
                    {
                        totalBalance,
                        bankBalance,
                        ewalletBalance,
                        walletBalance,
                        specifiedBankBalance = 0,
                        specifiedBankPercentage = 0
                    }
                });
            }

            if (bankID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var specifiedBankData = await _context.Banks
                   .Where(b => b.UserID == userID && b.BankID == bankID)
                   .Select(b => new
                   {
                       b.Amount
                   })
                   .FirstOrDefaultAsync();

            var specifiedBankBalance = specifiedBankData?.Amount ?? 0;
            var specifiedBankPercentage = totalBalance > 0 ? ((specifiedBankBalance / totalBalance) * 100) : 0;

            return Ok(new
            {
                success = true,
                total = new
                {
                    totalBalance,
                    bankBalance,
                    ewalletBalance,
                    specifiedBankBalance,
                    specifiedBankPercentage
                }
            });
        }
    }

    public class AddBankRequest
    {
        public int BankID { get; set; }
        public int UserID { get; set; }
        public string BankName { get; set; }
        public float Amount { get; set; }
    }
    
}


