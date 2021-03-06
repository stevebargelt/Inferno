using Inferno.Common.Models;

namespace Inferno.Api.Interfaces
{
    public interface ISmoker
    {
        string SmokerId { get; set; }
        SmokerMode Mode { get; }
        SmokerStatus Status { get; }
        int SetPoint { get; set; }
        int PValue { get; set; }
        Temps Temps { get; }
        bool SetMode(SmokerMode mode);
    }
}