using System;

namespace Rasa.Repositories.UnitOfWork
{
    public interface ITransactionParticipant : IDisposable
    {
        void Prepare();
        void FinalizePersistence() { }
        void Validate() { }
        // Read-only checks after every participant's final validation, with no further persistence.
        void ValidateCommitBoundary() { }
        void Committed();
    }
}
