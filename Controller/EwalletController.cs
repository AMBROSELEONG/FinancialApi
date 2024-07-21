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
    }

    public class getEwalletRequest
    {
        public int WalletID { get; set; }
        public int UserID { get; set; }
        public float Balance { get; set; }
    }
}