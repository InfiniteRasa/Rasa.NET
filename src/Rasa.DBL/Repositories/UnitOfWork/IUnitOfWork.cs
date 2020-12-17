using System;

namespace Rasa.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        void Complete();
        void Reject();
    }
}