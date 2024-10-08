using libzkfpcsharp;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class FingerprintController : ControllerBase
{
    private readonly FingerprintService _fingerprintService;
    

    public FingerprintController(FingerprintService fingerprintService)
    {
        _fingerprintService = fingerprintService;  // Injected via constructor
    }

    [HttpGet("initialize")]
    public ActionResult Initialize()
    {
        if (_fingerprintService.InitializeFingerprintSDK() == zkfp.ZKFP_ERR_OK)
            return Ok("Initialized successfully");
        else
            return BadRequest("Failed to initialize");
    }

    [HttpGet("open")]
    public ActionResult OpenDevice()
    {
        if (_fingerprintService.OpenFingerprintDevice() != IntPtr.Zero)
            return Ok("Device opened successfully");
        else
            return BadRequest("Failed to open device");
    }

    // [HttpGet("capture")]
    // public async Task<IActionResult> Capture()
    // {
    //     IntPtr deviceHandle = _fingerprintService.GetCurrentDeviceHandle();

    //     if (deviceHandle == IntPtr.Zero)
    //     {
    //         return BadRequest("Device not opened or handle is invalid.");
    //     }

    //     if (_fingerprintService.WaitForClearScan(deviceHandle, out byte[] imgBuffer, out byte[] template, out int templateSize))
    //     {
    //         string base64Image = _fingerprintService.base64Image;
    //         // await _fingerprintService.SendFingerprintDataAsync(base64Image, "123");  
    //         return Ok("Clear fingerprint captured successfully");
    //     }
    //     else
    //     {
    //         return BadRequest("Failed to capture clear fingerprint");
    //     }
    // }

    [HttpPost("verify/{userId}")]
    public async Task<IActionResult> VerifyFingerprint(string userId)
    {
        if (await _fingerprintService.WaitForClearScanToMatch(_fingerprintService.GetCurrentDeviceHandle(), userId))
        {
            return Ok("Fingerprint matches the registered template.");
        }
        else
        {
            return BadRequest("Fingerprint does not match the registered template.");
        }
    }

       [HttpPost("fingerprint")]
    public async Task<IActionResult> sendFingerPrint(byte[][] fingerprints)
    {
        int matchFingerScore = await _fingerprintService.sendFingerPrint(_fingerprintService.GetCurrentDeviceHandle(), fingerprints);
        return Ok(matchFingerScore);
    }



    [HttpGet("close")]
    public ActionResult CloseDevice()
    {
        _fingerprintService.CloseFingerprintDevice();
        return Ok("Device closed and SDK terminated");
    }

    [HttpGet("getAllTemplates")]
    public async Task<IActionResult> GetAllTemplates()
    {
        try
        {
            var templates = await _fingerprintService.GetAllFingerprintsAsync();
            return Ok(templates);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAndMergeFingerprints([FromBody] FingerprintRequest request)
    {
        IntPtr deviceHandle = _fingerprintService.GetCurrentDeviceHandle();
        if (deviceHandle == IntPtr.Zero)
        {
            return BadRequest("Fingerprint device is not initialized or connected.");
        }
        
        var (success, mergedTemplate) = await _fingerprintService.CaptureAndMergeFingerprints(deviceHandle, request.UserId);
        if (success)
        {
            return Ok(new {Message = "Fingerprint registered and Merged Successfully", template=mergedTemplate});
        }
        else
        {
            return BadRequest("Failed to register and merge fingerprints.");
        }
    }

}
