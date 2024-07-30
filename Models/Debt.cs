using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApi.Models
{
    public class Debt
    {
        public int DebtID { get; set; }
        public int UserID { get; set; }
        public string DebtName { get; set; }
        public double DebtAmount { get; set; }
        public double TotalAmount { get; set; }
        public double Cumulative { get; set; }
        public DateTime Date { get; set; }
        public DateTime NextDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Year { get; set; }
        public int MonthLeft { get; set; }
        public string Token { get; set; }
        public string Email { get; set; }
    }
}