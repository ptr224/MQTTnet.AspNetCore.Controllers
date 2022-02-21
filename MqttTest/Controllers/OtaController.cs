using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MqttTest.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OtaController : ControllerBase
{
    [HttpGet("bin/{id}/{version}")]
    public IActionResult Download(string id, string version)
    {
        if (id == "esp-test" && version == "0.1-ALPHA")
            return PhysicalFile("E:\\Test.ino.esp32.bin", "application/octet-stream", false);
        else
            return NotFound();
    }

    [HttpGet("{id}")]
    public IActionResult Info(string id)
    {
        if (id == "esp-test")
            return Ok(new
            {
                bin = "/api/ota/bin/esp-test/0.1-ALPHA",
                vCode = "0.1-ALPHA",
                vNum = 1
            });
        else
            return NotFound();
    }
}
