using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCoreFileProcessing
{
    public class SqlBlobReader : Stream
    {
        private readonly SqlCommand _command;
        private SqlDataReader _dataReader;
        private bool _disposed = false;
        private long _currentPosition = 0;
        private Stream _stream;

        public SqlBlobReader(SqlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException("command");
            if (command.Connection == null)
                throw new ArgumentException("The internal Connection cannot be null", "command");
            if (command.Connection.State != ConnectionState.Open)
                throw new ArgumentException("The internal Connection must be opened", "command");
            _command = command;
        }

        public void GetData()
        {
            _dataReader = _command.ExecuteReader(CommandBehavior.SequentialAccess);
            if (_dataReader.Read() && !_dataReader.IsDBNull(0))
            {
                _stream = _dataReader.GetStream(0);
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            return _stream.Read(buffer, index, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        public override bool CanSeek
        {
            get {
                return _stream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get {
                return _stream.Length;
            }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }

        public new async Task CopyToAsync(Stream destination, int bufferSize)
        {
            await _stream.CopyToAsync(destination, bufferSize);
        }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_dataReader != null)
                        _dataReader.Dispose();
                    SqlConnection conn = null;
                    if (_command != null)
                    {
                        conn = _command.Connection;
                        _command.Dispose();
                    }
                    if (conn != null)
                        conn.Dispose();
                    _disposed = true;
                    _stream.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }
    }
}
