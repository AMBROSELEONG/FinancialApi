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
    public class EwalletController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public EwalletController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("CreateEwallet")]
        public async Task<IActionResult> CreateEwallet([FromBody] getEwalletRequest request)
        {
            if (request.UserID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var existingEwallet = await _context.Ewallets.FirstOrDefaultAsync(e => e.UserID == request.UserID);

            if (existingEwallet != null)
            {
                return Ok(new { success = false, message = "E-Wallet already exists for this UserID" });
            }

            var newEwallet = new Ewallet
            {
                UserID = request.UserID,
                Balance = 0
            };

            _context.Ewallets.Add(newEwallet);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "E-Wallet created successfully" });
        }

        [HttpGet("GetEwallet")]
        public async Task<IActionResult> GetEwallet([FromQuery] int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var ewalletData = await _context.Ewallets
            .Where(e => e.UserID == userID)
            .Select(e => new
            {
                e.EwalletID,
                e.Balance
            })
            .FirstOrDefaultAsync();

            if (ewalletData == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                ewalletData = new
                {
                    ewalletData.EwalletID,
                    ewalletData.Balance
                }
            });
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
                    ewalletPercentage
                }
            });
        }
    }

    public class getEwalletRequest
    {
        public int WalletID { get; set; }
        public int UserID { get; set; }
        public float Balance { get; set; }
    }
}