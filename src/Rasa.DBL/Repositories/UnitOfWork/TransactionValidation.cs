using System;
using System.Collections.Generic;
using Rasa.Repositories.Char;

namespace Rasa.Repositories.UnitOfWork
{
    public sealed class TransactionValidation : ITransactionParticipant
    {
        private readonly List<Action> _checks = new();
        private readonly List<Action> _finalizers = new();
        private readonly List<Action> _commitChecks = new();
        public static void Add(ICharUnitOfWork unit, Action check) =>
            unit.Enlist(() => new TransactionValidation())._checks.Add(check);
        public static void BeforeValidation(ICharUnitOfWork unit, Action finalize) =>
            unit.Enlist(() => new TransactionValidation())._finalizers.Add(finalize);
        public static void AtCommitBoundary(ICharUnitOfWork unit, Action check) =>
            unit.Enlist(() => new TransactionValidation())._commitChecks.Add(check);
        public void Prepare() { }
        public void FinalizePersistence()
        {
            foreach (var finalize in _finalizers)
                finalize();
        }
        public void Validate()
        {
            foreach (var check in _checks)
                check();
        }
        public void ValidateCommitBoundary()
        {
            foreach (var check in _commitChecks)
                check();
        }
        public void Committed() { }
        public void Dispose() { }
    }
}
