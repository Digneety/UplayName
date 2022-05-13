using System;

namespace NameCheck
{
    public class UbisoftToken
    {
        public string Ticket { get; set; }
        public DateTime Expiration { get; set; }

        public string profileId { get; set; }

        public bool IsExpired => Expiration <= DateTime.Now;
    }
}