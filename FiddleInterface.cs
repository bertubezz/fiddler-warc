using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiddler.Importer.WARC
{
    [ProfferFormat("WARC", "Web Archive https://iipc.github.io/warc-specifications/specifications/warc-format/warc-1.0/")]
    public class FiddleInterface : ISessionImporter
    {

        public Session[] ImportSessions(string sImportFormat, Dictionary<string, object> dictOptions, EventHandler<ProgressCallbackEventArgs> evtProgressNotifications)
        {
            try
            {
                if (sImportFormat != "WARC")
                {
                    Debug.Assert(false);
                    return null;
                }

                string filename = dictOptions != null && dictOptions.ContainsKey("Filename") ?
                    dictOptions["Filename"] as string :
                    null;

                if (String.IsNullOrWhiteSpace(filename))
                {
                    var filter = "WARC file (*.warc)|*.warc|Text files (*.txt)|*.txt|Log files (*.log)|.log|All files (*.*)|*.*";
                    filter = "All files (*.*)|*.*";
                    filename = Fiddler.Utilities.ObtainOpenFilename("Import " + sImportFormat, filter);

                }

                if (String.IsNullOrWhiteSpace(filename))
                    return null;

                var sessions = new List<Session>();
                using (var oFS = new StreamReader(filename))
                {
                    var request =
                        "GET / HTTP/1.1\r\n" +
                        "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n" +
                        "Accept-Encoding: gzip,deflate\r\n" +
                        "Host: example.com\r\n" +
                        "User-Agent: Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.21 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.21\r\n" +
                        "Connection: Keep-alive\r\n\r\n";
                    var response =
                        "HTTP/1.1 200 OK\r\n" +
                        "Server: nginx/1.4.1\r\n" +
                        "Date: Tue, 17 Mar 1970 01:20:15 GMT\r\n" +
                        "Content-Type: text/plain\r\n" +
                        "Connection: keep-alive\r\n" +
                        "Content-Length: 2\r\n\r\n" +
                        "OK";

                    var session = new Session(Encoding.ASCII.GetBytes(request),
                        Encoding.ASCII.GetBytes(response),
                        SessionFlags.ImportedFromOtherTool);

                    session.Timers.ClientBeginRequest = new DateTime(2019, 8, 28, 1, 1, 1);
                    session.Timers.ServerDoneResponse = new DateTime(2019, 8, 28, 2, 2, 2);

                    sessions.Add(session);
                }

                return sessions.ToArray();
            }
            catch (Exception ex)
            {
                FiddlerApplication.ReportException(ex, "Failed to import NetLog");
                return null;
            }

            return null;

        }
        public void Dispose()
        {   
        }
    }
}
