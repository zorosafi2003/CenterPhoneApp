
using Android.Gms.Tasks;
using Java.Interop;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.CodeScanner;
using Task = Android.Gms.Tasks.Task;

namespace CentersBarCode.Platforms.Android;

public class MlKitBarcodeScanner : IDisposable
{
    private readonly IGmsBarcodeScanner barcodeScanner = GmsBarcodeScanning.GetClient(
        Platform.AppContext,
        new GmsBarcodeScannerOptions.Builder()
            .AllowManualInput()
            .EnableAutoZoom()
            .SetBarcodeFormats(Barcode.FormatAllFormats)
            .Build());

    public async Task<Barcode?> ScanAsync()
    {
        var taskCompletionSource = new TaskCompletionSource<Barcode?>();
        var barcodeResultListener = new OnBarcodeResultListener(taskCompletionSource);
        using var task = barcodeScanner.StartScan()
                    .AddOnCompleteListener(barcodeResultListener);
        return await taskCompletionSource.Task;
    }

    public void Dispose()
    {
        barcodeScanner.Dispose();
    }
}

public class OnBarcodeResultListener(TaskCompletionSource<Barcode?> taskCompletionSource) : Object, IOnCompleteListener
{
    public nint Handle => throw new NotImplementedException();

    public int JniIdentityHashCode => throw new NotImplementedException();

    public JniObjectReference PeerReference => throw new NotImplementedException();

    public JniPeerMembers JniPeerMembers => throw new NotImplementedException();

    public JniManagedPeerStates JniManagedPeerState => throw new NotImplementedException();

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Disposed()
    {
        throw new NotImplementedException();
    }

    public void DisposeUnlessReferenced()
    {
        throw new NotImplementedException();
    }

    public void Finalized()
    {
        throw new NotImplementedException();
    }

    public void OnComplete(Task task)
    {
        if (task.IsSuccessful)
        {
            taskCompletionSource.SetResult(task.Result.JavaCast<Barcode>());
        }
        else if (task.IsCanceled)
        {
            taskCompletionSource.SetResult(null);
        }
        else
        {
            taskCompletionSource.SetException(task.Exception);
        }
    }

    public void SetJniIdentityHashCode(int value)
    {
        throw new NotImplementedException();
    }

    public void SetJniManagedPeerState(JniManagedPeerStates value)
    {
        throw new NotImplementedException();
    }

    public void SetPeerReference(JniObjectReference reference)
    {
        throw new NotImplementedException();
    }

    public void UnregisterFromRuntime()
    {
        throw new NotImplementedException();
    }
}
