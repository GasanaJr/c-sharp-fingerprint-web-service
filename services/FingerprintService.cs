using System;
using libzkfpcsharp;  // Make sure this matches the namespace in the SDK

public class FingerprintService
{
    private static readonly object lockObject = new object();
    private IntPtr deviceHandle;
    private zkfp fingerprintDevice = new zkfp();

    public int InitializeFingerprintSDK()
    {
        return zkfp2.Init();
    }

    public IntPtr OpenFingerprintDevice()
    {
        lock (lockObject)
        {
            if (fingerprintDevice.GetDeviceCount() > 0)
            {
                deviceHandle = zkfp2.OpenDevice(0);
                Console.WriteLine($"Device opened successfully, Handle: {deviceHandle}");
            }
            else
            {
                Console.WriteLine("No devices found or failed to open device");
                deviceHandle = IntPtr.Zero;
            }
            return deviceHandle;
        }
    }


        public IntPtr GetCurrentDeviceHandle()
    {
        return deviceHandle;  
    }


    public bool WaitForClearScan(IntPtr deviceHandle, out byte[] imgBuffer, out byte[] template, out int templateSize)
    {
        imgBuffer = new byte[300 * 400];
        template = new byte[2048];  // Assuming 2048 is sufficient for the template; adjust if necessary.
        templateSize = 2048;

        int result;
        int attempts = 0;
        int maxAttempts =5;  // Set a reasonable limit to prevent infinite loops

        Console.WriteLine("Waiting for a clear fingerprint scan...");
        do
        {
            result = zkfp2.AcquireFingerprint(deviceHandle, imgBuffer, template, ref templateSize);
            if (result == zkfp.ZKFP_ERR_OK) {
                Console.WriteLine("Fingerprint scan successful.");
                return true;
            } else if (result == zkfp.ZKFP_ERR_CAPTURE) {
                // This error might indicate the scan was not clear or no finger was detected
                Console.WriteLine("No clear scan, retrying...");
            } else {
                // Log unexpected errors and break the loop
                Console.WriteLine($"Capture failed with error: {result}, stopping attempts.");
                break;
            }

            attempts++;
            Thread.Sleep(1000);  // Wait for half a second before retrying
        } while (attempts < maxAttempts);

        Console.WriteLine("Failed to capture a clear fingerprint after several attempts.");
        return false;
    }



    public void CloseFingerprintDevice()
    {
        lock (lockObject)
        {
            if (deviceHandle != IntPtr.Zero)
            {
                fingerprintDevice.CloseDevice();
                Console.WriteLine($"Device closed, Handle was: {deviceHandle}");
                deviceHandle = IntPtr.Zero;
            }
            zkfp2.Terminate();
        }
    }
}
