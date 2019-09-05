using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fiddler.Importer.WARC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiddler.Importer.WARC.Tests
{
    [TestClass()]
    public class FiddleInterfaceTests
    {
        [TestMethod()]
        public void ImportSessionsTest()
        {
            string warc =
@"Some data...
WARC-Record-ID: 000002965ACDB430-0000029659EEFB10
WARC-Type: request
WARC-Date: 2019-08-26T10:02:50.273871
Content-Length: 296
X-Sent-By: preflight

GET / HTTP/1.1
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8
Accept-Encoding: gzip,deflate
Host: testhtml5.vulnweb.com
User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.21 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.21
Connection: Keep-alive


...some more data...
WARC-Record-ID: 000002965ACDB430-0000029659EE5C90
WARC-Concurrent-To: 000002965ACDB430-0000029659EEFB10
WARC-Type: response
WARC-Date: 2019-08-26T10:02:50.758653
WARC-Duration: 484
Content-Length: 228

HTTP/1.1 200 OK
Server: nginx/1.4.1
Date: Tue, 17 Mar 1970 01:20:15 GMT
Content-Type: text/html; charset=utf-8
Connection: keep-alive
Access-Control-Allow-Origin: *
Original-Content-Encoding: gzip
Content-Length: 6940


";

            var fiddler = new FiddleInterface();
            var sessions = fiddler.ImportSessions("WARC", 
                new Dictionary<string, object>
                {
                    { "Content", warc }
                }, null);
            
        }
    }
}