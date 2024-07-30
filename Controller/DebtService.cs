using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApi.Data;
using FinancialApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialApi.Services
{
    public class DebtService
    {
        private readonly ApplicationDBContext _context;

        public DebtService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<Debt>> GetDebtsDueInDaysAsync(int days)
        {
            var malaysiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone);
            var targetDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, malaysiaTimeZone).AddDays(days);

            return await _context.Debts
                .Where(d => d.NextDate <= targetDate && d.NextDate > now)
                .ToListAsync();
        }
    }
}
