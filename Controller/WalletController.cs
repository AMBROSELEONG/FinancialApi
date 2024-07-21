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

    public class WalletController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public WalletController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("CreateWallet")]
        public async Task<IActionResult> CreateWallet([FromBody] getWalletRequest request)
        {
            if (request.UserID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var existingWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserID == request.UserID);

            if (existingWallet != null)
            {
                return Ok(new { success = false, message = "Wallet already exists for this UserID" });
            }

            var newWallet = new Wallet
            {
                UserID = request.UserID,
                Balance = 0
            };

            _context.Wallets.Add(newWallet);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Wallet created successfully" });
        }

        [HttpGet("GetWallet")]
        public async Task<IActionResult> GetWallet([FromQuery] int userID)
        {
            if (userID <= 0)
            {
                return BadRequest("Invalid UserID");
            }

            var walletData = await _context.Wallets
            .Where(w => w.UserID == userID)
            .Select(w => new
            {
                w.WalletID,
                w.Balance
            })
            .FirstOrDefaultAsync();

            if (walletData == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new
            {
                success = true,
                walletData = new
                {
                    walletData.WalletID,
                    walletData.Balance
                }
            });
        }
    }

    public class getWalletRequest
    {
        public int WalletID { get; set; }
        public int UserID { get; set; }
        public float Balance { get; set; }
    }
}