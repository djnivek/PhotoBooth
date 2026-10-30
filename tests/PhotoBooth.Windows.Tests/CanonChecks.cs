using PhotoBooth.Imaging;
using PhotoBooth.Windows;

internal static class CanonChecks
{
    public static async Task RunAsync()
    {
        var root=Path.Combine(Path.GetTempPath(),"estellart-canon-"+Guid.NewGuid().ToString("N"));
        var passed=0;void Check(bool ok,string label){if(!ok)throw new Exception(label);passed++;}
        try
        {
            var store=new SessionStore(root);var connection=new FakeCanon();
            await using(var camera=new CanonWpdCamera(store,connection))
            {
                var status=await camera.ConnectAsync(default);Check(status.Connected&&status.CanLiveView&&status.CanCapture,"Connection exposes both required capabilities");
                var session=Guid.NewGuid();var keys=new List<string>();
                for(var index=0;index<4;index++)
                {
                    await using(var frames=camera.LiveViewAsync(default).GetAsyncEnumerator())
                    {
                        Check(await frames.MoveNextAsync()&&frames.Current.Jpeg.Length>0,"Live view returns actual JPEG bytes");
                        try{await camera.CaptureAsync(session,index,default);throw new Exception("Capture accepted during preview");}
                        catch(InvalidOperationException){passed++;}
                    }
                    var shot=await camera.CaptureAsync(session,index,default);keys.Add(shot.LocalAssetKey);
                    var saved=await File.ReadAllBytesAsync(store.Resolve(shot.LocalAssetKey));
                    Check(saved.SequenceEqual(PhotoProcessor.Normalize(FixtureCamera.CreateFixture(index))),"Full capture, not preview frame, persisted before success");
                    Check(connection.Completed==index+1,"Camera transfer acknowledged after file save");
                }
                Check(keys.Distinct().Count()==4&&connection.Triggers==4,"Four independent shots, exactly four shutter requests");
                Check(connection.Stops==4,"Live view stopped between each capture");
            }
            Check(connection.Disposed,"Camera released after session");
            var silent=new FakeCanon{Silent=true};
            await using(var camera=new CanonWpdCamera(store,silent,TimeSpan.FromMilliseconds(50)))
            {
                await camera.ConnectAsync(default);
                try{await camera.CaptureAsync(Guid.NewGuid(),0,default);throw new Exception("Missing capture accepted");}
                catch(TimeoutException){passed++;}
                Check(silent.Triggers==1,"Missing camera event never retries shutter");
            }
            var failing=new FakeCanon{FailTrigger=true};
            await using(var camera=new CanonWpdCamera(store,failing))
            {
                await camera.ConnectAsync(default);
                try{await camera.CaptureAsync(Guid.NewGuid(),0,default);throw new Exception("Failed shutter accepted");}
                catch(IOException){passed++;}
                Check(failing.Triggers==1,"Ambiguous shutter failure never retries");
            }
            var rawThenJpeg=new FakeCanon{RawFirst=true};
            await using(var camera=new CanonWpdCamera(store,rawThenJpeg))
            {
                await camera.ConnectAsync(default);var photo=await camera.CaptureAsync(Guid.NewGuid(),0,default);
                Check(rawThenJpeg.Cancelled==1&&File.Exists(store.Resolve(photo.LocalAssetKey)),"RAW announcement skipped and JPEG awaited from the same exposure");
            }
        }
        finally{Directory.Delete(root,true);}
        Console.WriteLine($"PASS {passed} Canon sequencing checks; injected fake transport, no USB or camera used.");
    }
    private sealed class FakeCanon:ICanonConnection
    {
        public event Action<uint>? ObjectReady;
        public int Triggers,Completed,Stops,Cancelled;public bool Disposed,Silent,FailTrigger,RawFirst;
        private bool live;
        public Task<string> OpenAsync(CancellationToken token)=>Task.FromResult("Fake Canon");
        public Task StartPreviewAsync(CancellationToken token){live=true;return Task.CompletedTask;}
        public Task StopPreviewAsync(CancellationToken token){live=false;Stops++;return Task.CompletedTask;}
        public Task<byte[]?> ReadPreviewAsync(CancellationToken token)=>Task.FromResult<byte[]?>(FixtureCamera.CreateFixture(3));
        public Task TriggerAsync(CancellationToken token)
        {
            if(live)throw new Exception("Shutter sent while live view running");Triggers++;
            if(FailTrigger)throw new IOException("Ambiguous shutter response");
            if(!Silent){if(RawFirst)ObjectReady?.Invoke(999);ObjectReady?.Invoke((uint)Triggers);}
            return Task.CompletedTask;
        }
        public Task<string?> FileNameAsync(uint handle,CancellationToken token)=>Task.FromResult<string?>(handle==999?"capture.CR2":"capture.JPG");
        public Task<byte[]> DownloadAsync(uint handle,CancellationToken token)=>Task.FromResult(FixtureCamera.CreateFixture((int)handle-1));
        public Task CompleteAsync(uint handle,CancellationToken token){Completed++;return Task.CompletedTask;}
        public Task CancelTransferAsync(uint handle,CancellationToken token){Cancelled++;return Task.CompletedTask;}
        public ValueTask DisposeAsync(){Disposed=true;return ValueTask.CompletedTask;}
    }
}
