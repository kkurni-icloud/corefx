// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SSL encapsulated over TDS transport. During SSL handshake, SSL packets are
    /// transported in TDS packet type 0x12. Once SSL handshake has completed, SSL
    /// packets are sent transparently.
    /// </summary>
    internal sealed class NegotiateStreamOverTdsStream : Stream
    {
        private readonly Stream _stream;
        public Queue<byte[]> WritterBufferQueue = new Queue<byte[]>();
        public Queue<byte[]> ReadBufferQueue = new Queue<byte[]>();

        //private int _packetBytes = 0;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        public NegotiateStreamOverTdsStream(Stream stream)
        {
            _stream = stream;
        }
        

        /// <summary>
        /// Write buffer
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Byte count</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Console.WriteLine("NegotiateStream WRITE :");
            TdsParser.ConsoleWriteBytes(buffer);
            //_stream.Write(buffer, offset, count);
            WritterBufferQueue.Enqueue(buffer);
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            /*
            int readBytes;
            byte[] scratch = new byte[count < TdsEnums.HEADER_LEN ? TdsEnums.HEADER_LEN : count];

            if (_packetBytes == 0)
            {
                readBytes = _stream.Read(scratch, 0, TdsEnums.HEADER_LEN);
                _packetBytes = scratch[2] * 0x100;
                _packetBytes += scratch[3];
                _packetBytes -= TdsEnums.HEADER_LEN;
            }

            if (count > _packetBytes)
            {
                count = _packetBytes;
            }
            
            readBytes = _stream.Read(scratch, 0, count);

            _packetBytes -= readBytes;
            
            Buffer.BlockCopy(scratch, 0, buffer, offset, readBytes);
            Console.WriteLine("NegotiateStream Over TDS- Read");
            TdsParser.ConsoleWriteBytes(buffer);
            return readBytes;
            */

            /*
            Console.WriteLine("NegotiateStream - Enter Read...");
            while (ReadBufferQueue.Count <= 0)
            {
                Console.WriteLine("NegotiateStream - Read Wait...");
                Task.Delay(10);
            }

            var lst = new List<byte>();
            while (ReadBufferQueue.Count > 0)
            {
                lst.AddRange(ReadBufferQueue.Dequeue());    
            }

            buffer = lst.ToArray();
            Console.WriteLine("NegotiateStream - Exit Read...");
            TdsParser.ConsoleWriteBytes(buffer);
            return buffer.Length;
            */

            //var length = _stream.Read(buffer, offset, count);
            //TdsParser.ConsoleWriteBytes(buffer);
            Console.WriteLine("Negotiate Stream Receive Read : Do Nothing ??");
            return 0;
        }

        /// <summary>
        /// Set stream length. 
        /// </summary>
        /// <param name="value">Length</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flush stream
        /// </summary>
        public override void Flush()
        {
            _stream.Flush();
        }


        /// <summary>
        /// Get/set stream position
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Seek in stream
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="origin">Origin</param>
        /// <returns>Position</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Check if stream can be read from
        /// </summary>
        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        /// <summary>
        /// Check if stream can be written to
        /// </summary>
        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        /// <summary>
        /// Check if stream can be seeked
        /// </summary>
        public override bool CanSeek
        {
            get { return false; } // Seek not supported
        }

        /// <summary>
        /// Get stream length
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}
