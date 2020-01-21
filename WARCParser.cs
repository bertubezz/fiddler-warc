using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Fiddler.Importer.WARC
{
    class WARCReader : IDisposable
    {
        public WARCReader(Stream stream)
        {
            this.stream = stream;
        }

        public WARCReader()
        {
        }

        ~WARCReader()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (stream != null)
                stream.Dispose();
        }

        Stream stream = null;
        public Stream Stream { get => stream; set => stream = value; }

        public bool EndOfStream
        {
            get
            {
                return stream.Position == stream.Length;
            }
        }

        public long Position
        {
            get
            {
                return stream.Position;
            }
        }

        public string ReadLine()
        {
            string line = string.Empty;

            do
            {
                var c = stream.ReadByte();

                if (c == '\n')
                    return line;

                if (c != '\r')
                    line += (char)c;


            } while (!EndOfStream);

            return line;
        }

        public byte[] ReadBytes(int count)
        {
            var result = new byte[count];
            stream.Read(result, 0, count);
            return result;
        }
    }

    class WARCFileReader : WARCReader, IDisposable
    {
        public WARCFileReader(FileStream fileStream)
        {
            mmf = MemoryMappedFile.CreateFromFile(fileStream, null, 0,
                MemoryMappedFileAccess.Read, HandleInheritability.None, false);
            Stream = mmf.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
        }

        ~WARCFileReader()
        {
            Dispose();
        }

        public new void Dispose()
        {
            base.Dispose();
            mmf.Dispose();
        }

        MemoryMappedFile mmf;
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

        public WARCParser(FileStream s)
        {
            stream = new WARCFileReader(s);
        }

        public WARCParser(MemoryStream s)
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
