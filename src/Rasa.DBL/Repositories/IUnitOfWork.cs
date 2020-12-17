using System;

namespace Rasa.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        void Complete();
        void Reject();
    }
}