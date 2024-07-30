using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace FinancialApi.Data
{
    public class ApplicationDBContext : DbContext
    {
        public ApplicationDBContext(DbContextOptions dbContextOptions)
        : base(dbContextOptions)
        {

        }

        public DbSet<VerifyOTP> VerifyOTPs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletIncome> WalletIncomes { get; set; }
        public DbSet<WalletSpend> WalletSpends { get; set; }
        public DbSet<Ewallet> Ewallets { get; set; }
        public DbSet<EwalletIncome> EwalletIncomes { get; set; }
        public DbSet<EwalletSpend> EwalletSpends { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<BankIncome> BankIncomes { get; set; }
        public DbSet<BankSpend> BankSpends { get; set; }
        public DbSet<Debt> Debts { get; set; }
    }
}