using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApi.Data;
using FinancialApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinancialApi.Services
{
    public class UserService
    {
        private readonly ApplicationDBContext _context;

        public UserService(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetInactiveUsersAsync(DateTime threshold)
        {
            return await _context.Users
                .Where(u => u.LastLogin < threshold)
                .ToListAsync();
        }

        public async Task<List<DebtPay>> GetDebt(DateTime threshold)
        {
            return await _context.DebtPays
            .Where(d => d.NextDate < threshold)
            .ToListAsync();
        }
    }
}