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
        public Session[] ImportSessions(string sImportFormat, Dictionary<string, object> dictOptions,
            EventHandler<ProgressCallbackEventArgs> evtProgressNotifications)
        {
            try
            {
                if (sImportFormat != "WARC")
                    throw new ArgumentException("Invalid import format");

                string filename = null, content = null;
                // filename = @"X:\stuff\fiddler-warc\samples\logfile.csv";

                if (dictOptions != null)
                {
                    if (dictOptions.ContainsKey("Filename"))
                        filename = dictOptions["Filename"] as string;
                    else if (dictOptions.ContainsKey("Content"))
                        content = dictOptions["Content"] as string;
                }

                if (String.IsNullOrWhiteSpace(filename) && content == null)
                {
                    var filter = "WARC file (*.warc)|*.warc|Text files (*.txt)|*.txt|Log files (*.log)|.log|All files (*.*)|*.*";
                    filter = "All files (*.*)|*.*";
                    filename = Fiddler.Utilities.ObtainOpenFilename("Import " + sImportFormat, filter);                    
                }

                WARCParser warc;
                if (!String.IsNullOrWhiteSpace(filename))
                    warc = new WARCParser(new FileStream(filename, FileMode.Open));
                else if (content != null)                    
                    warc = new WARCParser(new MemoryStream(Encoding.UTF8.GetBytes(content)));
                else
                    throw new ArgumentException("Invalid options");

                var sessions = new List<Session>();
                using (warc)
                {
                    WARCParser.Record prevRequest = null; 
                    foreach (var record in warc.parse())
                    {
                        if (prevRequest == null)
                        {
                            if (record.Type == "request")
                                prevRequest = record;
                        }
                        else
                        {
                            WARCParser.Record request = prevRequest;
                            WARCParser.Record response = null;
                            if (record.Type == "response")
                                response = record;

                            if (response == null)
                            {
                                var session = new Session(
                                    request.Body,
                                    null,
                                    SessionFlags.ImportedFromOtherTool);

                                session.Timers.ClientBeginRequest = request.Date;

                                sessions.Add(session);
                            }
                            else if (request.RecordID == response.ConcurrentTo)
                            {
                                if (!String.IsNullOrWhiteSpace(request.SentBy))
                                {
                                    var session = new Session(
                                        request.Body,
                                        response.Body,
                                        SessionFlags.ImportedFromOtherTool);

                                    session.Timers.ClientBeginRequest = request.Date;
                                    session.Timers.ServerDoneResponse = response.Date;

                                    sessions.Add(session);
                                }
                            }
                            else
                            {
                                if (!String.IsNullOrWhiteSpace(request.SentBy))
                                {
                                    var requestSession = new Session(
                                        request.Body,
                                        null,
                                        SessionFlags.ImportedFromOtherTool);

                                    requestSession.Timers.ClientBeginRequest = request.Date;

                                    sessions.Add(requestSession);
                                }
                                var responseSession = new Session(
                                    null,
                                    response.Body,
                                    SessionFlags.ImportedFromOtherTool);

                                responseSession.Timers.ServerDoneResponse = response.Date;

                                sessions.Add(responseSession);
                            }
                            
                            prevRequest = null;
                        }
                    }
                }

                return sessions.ToArray();
            }
            catch (Exception ex)
            {
                FiddlerApplication.ReportException(ex, "Failed to import NetLog");
                return null;
            }
        }
        public void Dispose()
        {   
        }
    }
}
