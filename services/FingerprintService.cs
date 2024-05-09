using System;
using libzkfpcsharp;  
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using MongoDB.Driver;


public class FingerprintScanResult
{
    public bool Success { get; set; }
    public byte[] ImageBuffer { get; set; }
    public byte[] Template { get; set; }
    public int TemplateSize { get; set; }
}


public class FingerprintService
{
    private static readonly object lockObject = new object();

    private readonly IMongoCollection<Fingerprint> _fingerprints;
    private IntPtr deviceHandle;
    public IntPtr  dbHandle = zkfp2.DBInit();
    private zkfp fingerprintDevice = new zkfp();
    public string base64Image = ""; 

    private Dictionary<int, byte[]> userTemplates = new Dictionary<int, byte[]>();
    private int userIdCounter = 0;
    private const int MATCH_THRESHOLD = 75;

    // Constructor method

    public FingerprintService(IConfiguration config)
    {
        var client = new MongoClient(config.GetConnectionString("MongoDb"));
        var database = client.GetDatabase("FingerprintDb");
        _fingerprints = database.GetCollection<Fingerprint>("Fingerprints");
    }

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

        public int AddUserTemplate(byte[] template)
    {
        lock (userTemplates)
        {
            if (!Exists(template))
            {
                int newUserId = ++userIdCounter;  // Increment and get the new user ID
                userTemplates.Add(newUserId, template);
                zkfp2.DBAdd(dbHandle, newUserId, template);
                Console.WriteLine("Template added to memory successfully");
                return 0;  // Return the new user ID
            }
            return -1; // Template exists already, return an indicator
        }
    }

        private bool Exists(byte[] candidateTemplate)
    {
        foreach (var entry in userTemplates)
        {
            int score = zkfp2.DBMatch(dbHandle, candidateTemplate, entry.Value);
            if (score > MATCH_THRESHOLD)
            {
                Console.WriteLine($"Duplicate detected with score: {score}");
                return true;
            }
        }
        return false;
    }

    public async Task<int> MatchTemplate(byte[] candidateTemplate, string userId)
    {
        byte[] storedTemplate = await GetFingerprintByUserIdAsync(userId);
        if (storedTemplate == null)
        {
            Console.WriteLine("No template found for user ID.");
            return -1; // Indicate no match found because the user has no stored template
        }
        
        int matchScore = zkfp2.DBMatch(dbHandle, candidateTemplate, storedTemplate);
        return matchScore;
    }



public bool WaitForClearScan(IntPtr deviceHandle, out byte[] imgBuffer, out byte[] template, out int templateSize)
    {
        imgBuffer = new byte[300 * 400];
        template = new byte[2048];  // Assuming 2048 is sufficient for the template; adjust if necessary.
        templateSize = 2048;
        int imageSize = imgBuffer.Length;

        int result;
        int attempts = 0;
        int maxAttempts = 5;  // Set a reasonable limit to prevent infinite loops

        Console.WriteLine("Waiting for a clear fingerprint scan...");
        do
        {
            result = zkfp2.AcquireFingerprint(deviceHandle, imgBuffer, template, ref templateSize);
            if (result == zkfp.ZKFP_ERR_OK) {
                Console.WriteLine("Fingerprint scan successful.");
                string filePath = "C:\\Users\\USER\\FingerprintMiddleware\\fingerprint.bmp";
                SaveImage(imgBuffer, 300, 400, filePath);
                base64Image = ConvertImageToBase64(filePath);
                AddUserTemplate(template);
                return true;
            } else if (result == zkfp.ZKFP_ERR_CAPTURE) {
                Console.WriteLine("No clear scan, retrying...");
            } else {
                Console.WriteLine($"Capture failed with error: {result}, stopping attempts.");
                break;
            }

            attempts++;
            Thread.Sleep(1000);  // Wait for half a second before retrying
        } while (attempts < maxAttempts);

        Console.WriteLine("Failed to capture a clear fingerprint after several attempts.");
        return false;
    }

    public async Task<bool> WaitForClearScanToMatch(IntPtr deviceHandle, string userId)
    {
        byte[] imgBuffer = new byte[300 * 400];
        byte[] template = new byte[2048];
        int templateSize = 2048;
        int attempts = 0;
        const int maxAttempts = 5;

        Console.WriteLine("Waiting for a clear fingerprint scan...");
        while (attempts < maxAttempts)
        {
            int result = zkfp2.AcquireFingerprint(deviceHandle, imgBuffer, template, ref templateSize);
            if (result == zkfp.ZKFP_ERR_OK)
            {
                Console.WriteLine("Fingerprint scan successful.");
                int matchResult = await MatchTemplate(template, userId);
                Console.WriteLine($"Match result: {matchResult}");
                return matchResult > MATCH_THRESHOLD; // Assume MATCH_THRESHOLD is a predefined constant
            }
            else if (result == zkfp.ZKFP_ERR_CAPTURE)
            {
                Console.WriteLine("No clear scan, retrying...");
            }
            else
            {
                Console.WriteLine($"Capture failed with error: {result}, stopping attempts.");
                break;
            }
            attempts++;
            Thread.Sleep(1000);
        }
        Console.WriteLine("Failed to capture a clear fingerprint after several attempts.");
        return false;
    }


    

    public void SaveImage(byte[] imgBuffer, int width, int height, string filePath)
    {
        using (var bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed))
        {
            // Set the palette to grayscale
            ColorPalette palette = bmp.Palette;
            for (int i = 0; i < 256; i++) // Create grayscale palette
                palette.Entries[i] = Color.FromArgb(i, i, i);
            bmp.Palette = palette;

            // Lock the bitmap's bits for editing
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
            
            // Copy the raw image data into the bitmap
            Marshal.Copy(imgBuffer, 0, bmpData.Scan0, imgBuffer.Length);
            bmp.UnlockBits(bmpData);

            // Save the bitmap to a file
            bmp.Save(filePath, ImageFormat.Bmp);
        }
        Console.WriteLine("Image saved to: " + filePath);
    }

        public string ConvertImageToBase64(string filePath)
    {
        using (Image image = Image.FromFile(filePath))
        {
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, image.RawFormat);
                byte[] imageBytes = m.ToArray();
                return Convert.ToBase64String(imageBytes);
            }
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

    // Node Endpoint

        public async Task<bool> SendFingerprintDataAsync(string base64Fingerprint, string userId)
    {
        var httpClient = new HttpClient();
        var url = "http://localhost:3000/match"; 
        var payload = new
        {
            userId = userId,
            fingerprintTemplate = base64Fingerprint
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            HttpResponseMessage response = await httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("User Found.");
                return true;
            }
            else
            {
                Console.WriteLine("Failed to send data. Status code: " + response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending data: " + ex.Message);
            return false;
        }
    }


    // DB methods

    public async Task<FingerprintScanResult> WaitForClearScan(IntPtr deviceHandle, string userId)
{
    byte[] imgBuffer = new byte[300 * 400];
    byte[] template = new byte[2048];
    int templateSize = 2048;
    int attempts = 0;
    const int maxAttempts = 5;

    Console.WriteLine("Waiting for a clear fingerprint scan...");
    while (attempts < maxAttempts)
    {
        int result = zkfp2.AcquireFingerprint(deviceHandle, imgBuffer, template, ref templateSize);
        if (result == zkfp.ZKFP_ERR_OK)
        {
            Console.WriteLine("Fingerprint scan successful.");
            string filePath = "C:\\Users\\USER\\FingerprintMiddleware\\fingerprint.bmp";
            SaveImage(imgBuffer, 300, 400, filePath);
            base64Image = ConvertImageToBase64(filePath);
            await AddFingerprintAsync(userId, template);
            return new FingerprintScanResult { Success = true, ImageBuffer = imgBuffer, Template = template, TemplateSize = templateSize };
        }
        else if (result == zkfp.ZKFP_ERR_CAPTURE)
        {
            Console.WriteLine("No clear scan, retrying...");
        }
        else
        {
            Console.WriteLine($"Capture failed with error: {result}, stopping attempts.");
            break;
        }
        attempts++;
        Thread.Sleep(1000);
    }
    return new FingerprintScanResult { Success = false };
}

    

    public async Task<List<byte[]>> GetAllFingerprintsAsync()
    {
        var fingerprints = await _fingerprints.Find(f => true).ToListAsync();
        return fingerprints.Select(f => f.Template).ToList();

    }

     public async Task AddFingerprintAsync(string userId, byte[] template)
    {
        var fingerprint = new Fingerprint
        {
            UserId = userId,
            Template = template,
            CreatedAt = DateTime.UtcNow
        };
        await _fingerprints.InsertOneAsync(fingerprint);
    }

    public async Task<byte[]> GetFingerprintByUserIdAsync(string userId)
    {
        var filter = Builders<Fingerprint>.Filter.Eq(f => f.UserId, userId);
        var fingerprint = await _fingerprints.Find(filter).FirstOrDefaultAsync();

        
        return fingerprint?.Template;
    }


    
}
