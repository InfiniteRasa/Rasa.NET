using System;
using System.Security.Cryptography;

namespace Rasa.Services.Random
{
    public class RandomNumberService : IRandomNumberService, IDisposable
    {
        private readonly RandomNumberGenerator _randomNumberGenerator;

        public RandomNumberService()
        {
            _randomNumberGenerator = RandomNumberGenerator.Create();
        }

        public byte[] CreateRandomBytes(uint length)
        {
            var bytes = new byte[length];
            _randomNumberGenerator.GetBytes(bytes);
            return bytes;
        }

        public void Dispose()
        {
            _randomNumberGenerator?.Dispose();
        }
    }
}