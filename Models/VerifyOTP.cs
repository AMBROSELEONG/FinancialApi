using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApi.Models
{
    public class VerifyOTP
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public string OTP { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}