using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApi.Models
{
    public class Bank
    {
        public int BankID { get; set; }
        public int UserID { get; set; }
        public string BankName { get; set; }
        public float Amount { get; set; }
    }
}