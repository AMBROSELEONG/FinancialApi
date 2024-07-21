using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinancialApi.Models
{
    public class User
    {
        public int UserID { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Token { get; set; }
        public DateTime LastLogin { get; set; }
        public bool FingerPrint { get; set; }
        public required string PublicKey { get; set; }
        public required string Signature { get; set; }

    }
}