using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Structures
{
    using Repositories;

    internal sealed class GameplayRejectionException : InvalidOperationException
    {
        internal GameplayRejectionException(string message) : base(message)
        {
        }

        internal GameplayRejectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal static bool IsExpected(Exception error) =>
            error is GameplayRejectionException or global::Rasa.Missions.Runtime.MissionRuleException or EntityNotFoundException or
                DbException or DbUpdateException;
    }
}
