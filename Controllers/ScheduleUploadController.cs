using Microsoft.AspNetCore.Mvc;
using SingleSlotTester.Services;
using SingleSlotTester.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Net.Http;
using System.Text;

namespace SingleSlotTester.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleUploadController : ControllerBase
    {
        private readonly FhirMappingService _mappingService;
        private static readonly HttpClient _httpClient = new HttpClient(); // 用於傳送到 FHIR Server

        public ScheduleUploadController(FhirMappingService mappingService)
        {
            _mappingService = mappingService;
        }
[HttpPost("convert-and-upload")]
public async Task<IActionResult> ConvertAndUpload([FromForm] IFormFile file)
{
    // 1. 檢查檔案
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "伺服器未收到檔案" });

    string jsonPayload = ""; // 先宣告在外面，確保後面讀得到

    try 
    {
        // 2. 讀取 CSV
        using var reader = new StreamReader(file.OpenReadStream());
        var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim(), // 只修剪空白
            HeaderValidated = null,
            MissingFieldFound = null
        };

        using var csv = new CsvHelper.CsvReader(reader, config);
        var records = csv.GetRecords<TeleERCsvRecord>().ToList();
        var record = records.FirstOrDefault();

        if (record == null) return BadRequest(new { message = "CSV 內容為空" });

        // 3. 轉換為 FHIR JSON
        var slot = _mappingService.MapToFhirSlot(record, record.Sched_ID);
        var serializer = new Hl7.Fhir.Serialization.FhirJsonSerializer();
        jsonPayload = serializer.SerializeToString(slot);

        // 4. 嘗試上傳 (用 try-catch 包起來，不讓上傳失敗影響轉換結果)
        string uploadMessage = "未嘗試上傳";
        string serverResponse = "";
        
        try 
        {
            const string fhirServerUrl = "https://hapi.fhir.org/baseR4/Slot";
            var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/fhir+json");
            _httpClient.Timeout = TimeSpan.FromSeconds(3); // 設定 3 秒超時，避免卡太久
            
            var response = await _httpClient.PostAsync(fhirServerUrl, content);
            serverResponse = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
                uploadMessage = "✅ 上傳成功";
            else
                uploadMessage = $"⚠️ 上傳失敗 (HTTP {response.StatusCode})";
        }
        catch (Exception uploadEx)
        {
            // 捕捉連線錯誤 (例如伺服器沒開)
            uploadMessage = $"❌ 無法連線至伺服器: {uploadEx.Message}";
        }

        // 5. 回傳結果 (重點：不管上傳成不成功，都把 JSON 回傳給前端)
        return Ok(new { 
            message = $"轉換成功！但上傳結果為: {uploadMessage}", 
            fhirResponse = serverResponse,
            generatedJson = jsonPayload 
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = $"解析或轉換過程失敗: {ex.Message}" });
    }
}
    }
}