using System;
using System.Collections.Generic;
using System.IO;

namespace Rasa.Memory
{
    public static class BufferManager
    {
        private static readonly Stack<BufferData> BufferDatas = new Stack<BufferData>();

        public static byte[] Buffer { get; private set; }
        public static int BlockSize { get; private set; }

        public static void Initialize(int blockSize, int maxClients, int concurrentOperationsByClient)
        {
            // Already initialized
            if (Buffer != null)
                return;

            if ((long)blockSize * maxClients * concurrentOperationsByClient > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(blockSize), blockSize * maxClients * concurrentOperationsByClient, "Can not alloc an array this big!");

            BlockSize = blockSize;

            // Block Size * Max Client count * concurrentOperationsByClient:
            // The block size is the size of the buffer
            // The max client count * concurrentOperationsByClient makes sure that there is enough buffers for every client for receive and concurrent send operations
            Buffer = new byte[BlockSize * maxClients * concurrentOperationsByClient];

            // It's a stack, and we should preferable use the buffers at the beginning, so we put it in in reverse order
            lock (BufferDatas)
                for (var i = Buffer.Length - BlockSize; i >= 0; i -= BlockSize)
                    BufferDatas.Push(new BufferData(i));
        }

        public static BufferData RequestBuffer()
        {
            if (BufferDatas.Count == 0)
                throw new OutOfMemoryException("BufferManager has ran out of usable buffer space!");

            BufferData data;

            lock (BufferDatas)
                data = BufferDatas.Pop();

            // Clear the buffer of any previous writes
            data.Clear();

            return data;
        }

        public static void FreeBuffer(BufferData data)
        {
            data.Reset();

            lock (BufferDatas)
                BufferDatas.Push(data);
        }
    }
}
