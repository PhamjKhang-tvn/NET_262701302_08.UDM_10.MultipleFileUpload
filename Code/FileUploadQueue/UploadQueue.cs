using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Udm10.Shared
{
    // TV5: sequential queue = ONE active file per Client. No retry storms and no byte resume.
    public sealed class UploadQueue
    {
        private int running;
        public async Task<QueueResult> RunAsync(IList<UploadJob> jobs,ClientOptions options,Action<TransferUpdate> report,CancellationToken token)
        {
            if(Interlocked.Exchange(ref running,1)==1) throw new InvalidOperationException("Queue is already running.");
            var result=new QueueResult { Waiting=jobs.Count };
            var service=new UploadClientService();
            try
            {
                foreach(var job in jobs)
                {
                    if(token.IsCancellationRequested) break;
                    result.Waiting--;
                    UploadClientService.Publish(report,job,"Uploading",0,0,0,"Opening file / connecting");
                    try
                    {
                        await service.UploadAsync(job,options,report,token).ConfigureAwait(false);
                        result.Completed++;
                    }
                    catch(OperationCanceledException)
                    {
                        result.Errors++;
                        UploadClientService.Publish(report,job,"Error",0,0,0,"CANCELLED: batch stopped; retry restarts this file from byte zero.");
                        break;
                    }
                    catch(UploadException ex)
                    {
                        result.Errors++;
                        UploadClientService.Publish(report,job,"Error",0,0,0,ex.Code+": "+ex.Message);
                        if(ex.ConnectionUnavailable) { result.ConnectionLost=true; break; }
                    }
                    catch(Exception ex)
                    {
                        result.Errors++;
                        UploadClientService.Publish(report,job,"Error",0,0,0,"UNEXPECTED: "+ex.Message);
                        // A single local failure must not stop the remaining queue.
                    }
                }
                return result;
            }
            finally { Interlocked.Exchange(ref running,0); }
        }
    }
}
