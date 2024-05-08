using System;
using libzkfpcsharp;  // Make sure this matches the namespace in the SDK
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;



public class FingerprintService
{
    private static readonly object lockObject = new object();
    private IntPtr deviceHandle;
    private zkfp fingerprintDevice = new zkfp();
     string base64Image = "";

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
        int imageSize = imgBuffer.Length;

        int result;
        int attempts = 0;
        int maxAttempts =5;  // Set a reasonable limit to prevent infinite loops

        Console.WriteLine("Waiting for a clear fingerprint scan...");
        do
        {
            result = zkfp2.AcquireFingerprint(deviceHandle, imgBuffer, template, ref templateSize);
            if (result == zkfp.ZKFP_ERR_OK) {
                Console.WriteLine("Fingerprint scan successful.");
                SaveImage(imgBuffer, 300, 400, "C:\\Users\\USER\\FingerprintMiddleware\\fingerprint.bmp");
                int resultImage = zkfp.Blob2Base64String(imgBuffer, imageSize, ref base64Image);
                byte[]fingerBuffer = zkfp2.Base64ToBlob(base64Image);
                SaveImageFromBlob(fingerBuffer, 300, 400, "C:\\Users\\USER\\FingerprintMiddleware\\fingerprint1.bmp");
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

        public void SaveImage(byte[] imgBuffer, int width, int height, string filePath)
    {
        using (var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed))
        {
            // Set palette to grayscale
            ColorPalette palette = bitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                palette.Entries[i] = Color.FromArgb(i, i, i);
            }
            bitmap.Palette = palette;

            // Lock the bitmap's bits
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly, bitmap.PixelFormat);

            // Get the address of the first line
            IntPtr ptr = bitmapData.Scan0;

            // Copy the RGB values into the bitmap
            System.Runtime.InteropServices.Marshal.Copy(imgBuffer, 0, ptr, imgBuffer.Length);

            // Unlock the bits
            bitmap.UnlockBits(bitmapData);

            // Save the bitmap to file
            bitmap.Save(filePath, ImageFormat.Bmp);
            Console.WriteLine($"Image saved to {filePath}");
        }
    }

        public void SaveImageFromBlob(byte[] imageBuffer, int width, int height, string filePath)
    {
        using (var bitmap = new Bitmap(width, height, PixelFormat.Format8bppIndexed))
        {
            ColorPalette ncp = bitmap.Palette;
            for (int i = 0; i < 256; i++)
                ncp.Entries[i] = Color.FromArgb(i, i, i);
            bitmap.Palette = ncp;

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            
            Marshal.Copy(imageBuffer, 0, data.Scan0, imageBuffer.Length);
            bitmap.UnlockBits(data);

            bitmap.Save(filePath, ImageFormat.Bmp);
            Console.WriteLine("Image saved to: " + filePath);
        }
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
