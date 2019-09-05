using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiddler.Importer.WARC
{
    class WARCReader : IDisposable
    {
        public WARCReader(Stream stream)
        {
            this.stream = new BinaryReader(stream);
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public bool EndOfStream
        {
            get
            {
                return stream.BaseStream.Position == stream.BaseStream.Length;
            }
        }

        public long Position
        {
            get
            {
                if (buffer == null)
                    return 0;

                return stream.BaseStream.Position - buffer.Length + bufferPos;
            }
        }

        public string ReadLine()
        {
            string line = string.Empty;            
            while (!EndOfStream)
            {
                if (buffer == null || bufferPos == buffer.Length)
                {
                    buffer = stream.ReadBytes(4096);
                    bufferPos = 0;
                }

                while (bufferPos < buffer.Length)
                {
                    var b = (char)buffer[bufferPos++];

                    if (b == '\n')
                        return line;

                    if (b != '\r')
                        line += b;
                }
            }
            
            return line;
        }

        public byte[] ReadBytes(int count)
        {
            count = (int)Math.Min((long)count,
                stream.BaseStream.Length - stream.BaseStream.Position);
            
            var result = new byte[count];

            var bufferSize = buffer.Length - bufferPos;
            if (count < bufferSize)
            {
                Array.Copy(buffer, bufferPos, result, 0, count);
                bufferPos += count;
            }
            else if (bufferSize > 0)
            {
                Array.Copy(buffer, bufferPos, result, 0, bufferSize);
                bufferPos += bufferSize;

                stream.Read(result, bufferSize, count - bufferSize);
            }
            else
            {
                stream.Read(result, 0, count);
            }

            return result;
        }

        BinaryReader stream;

        byte[] buffer = null;
        int bufferPos = 0;
    }

    public class WARCParser : IDisposable
    {
        public class Record
        {
            public long Offset;
            public Dictionary<string, string> Headers = new Dictionary<string, string>();
            public byte[] Body;

            public Int32 Size
            {
                get
                {
                    string size = String.Empty;
                    Headers.TryGetValue("Content-Length", out size);
                    return Convert.ToInt32(size);
                }
            }

            public string RecordID
            {
                get
                {
                    string recordID = String.Empty;
                    Headers.TryGetValue("WARC-Record-ID", out recordID);
                    return recordID;
                }
            }

            public string Type
            {
                get
                {
                    string type = String.Empty;
                    Headers.TryGetValue("WARC-Type", out type);
                    return type;
                }
            }

            public DateTime Date
            {
                get
                {
                    string date = String.Empty;
                    Headers.TryGetValue("WARC-Date", out date);
                    return Convert.ToDateTime(date);
                }
            }

            public string ConcurrentTo
            {
                get
                {
                    string concurrentTo = String.Empty;
                    Headers.TryGetValue("WARC-Concurrent-To", out concurrentTo);
                    return concurrentTo;
                }
            }

            public Int32 Duration
            {
                get
                {
                    string duration = String.Empty;
                    Headers.TryGetValue("WARC_Duration", out duration);
                    return Convert.ToInt32(duration);
                }
            }

            public string SentBy
            {
                get
                {
                    string sentBy = String.Empty;
                    Headers.TryGetValue("X-Sent-By", out sentBy);
                    return sentBy;
                }
            }
        }

        public WARCParser(Stream s)
        {
            stream = new WARCReader(s);
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        public IEnumerable<Record> parse()
        {
            while (!stream.EndOfStream)
            {
                var record = new Record();
                record.Offset = stream.Position;

                string line = null;
                line = stream.ReadLine();
                if (!line.StartsWith("WARC"))
                    continue;

                do
                {
                    var sepIndex = line.IndexOf(':');
                    string headerName, headerValue;
                    if (sepIndex < 0)
                    {
                        headerName = line.Trim();
                        headerValue = string.Empty;
                    }
                    else if (sepIndex == 0)
                    {
                        headerName = string.Empty;
                        headerValue = line.Substring(1).Trim();
                    }
                    else
                    {
                        headerName = line.Substring(0, sepIndex).Trim();
                        headerValue = line.Substring(sepIndex + 1).Trim();
                    }

                    record.Headers.Add(headerName, headerValue);

                    line = stream.ReadLine();

                } while (!String.IsNullOrWhiteSpace(line));

                record.Body = stream.ReadBytes(record.Size);

                yield return record;
            }
        }

        WARCReader stream;
    }
}
