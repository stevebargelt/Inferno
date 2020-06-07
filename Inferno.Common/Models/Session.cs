using System;

namespace Inferno.Common.Models
{
    public class Session
    {
        public Guid SessionID { get; set; }
        public string SessionName { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime Created { get; set; }
    }

}