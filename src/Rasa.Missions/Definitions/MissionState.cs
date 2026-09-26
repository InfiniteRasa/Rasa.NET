using System;

namespace Rasa.Data
{
    public enum MissionState
    {
        Active      = 0,
        Success     = 1,
        Failed      = 2,
        [Obsolete("Use Failed.")]
        Failded     = Failed,
        NotAssigned = 3,
        Completed   = 4
    }
}
