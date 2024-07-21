using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApi.Models
{
    public class Ewallet
    {
        public int EwalletID { get; set; }
        public int UserID { get; set; }
        public float Balance { get; set; }
    }
}