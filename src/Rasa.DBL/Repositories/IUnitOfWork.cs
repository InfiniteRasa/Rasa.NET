using System;

namespace Rasa.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        bool AutoComplete { get; set; }
        void Complete();
        void Reject();
    }
}