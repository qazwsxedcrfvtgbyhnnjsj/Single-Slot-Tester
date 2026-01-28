using Microsoft.AspNetCore.Mvc;
using SingleSlotTester.Services; // 👈 確保這裡對應新專案的 Namespace
using SingleSlotTester.Models;   // 👈 確保這裡對應新專案的 Namespace
using CsvHelper;
using System.Globalization;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using CsvHelper.Configuration;

namespace SingleSlotTester.Controllers // 👈 建議改為新專案名稱
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleUploadController : ControllerBase
    {
        private readonly FhirMappingService _mappingService;

        public ScheduleUploadController(FhirMappingService mappingService)
        {
            _mappingService = mappingService;
        }

        [HttpPost("convert-single")]
        public IActionResult ConvertSingle(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("請選擇檔案");

            try 
            {
                using var reader = new StreamReader(file.OpenReadStream());
                // 加入配置以正確處理 CSV 標題
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    PrepareHeaderForMatch = args => args.Header.Trim()
                };

                using var csv = new CsvReader(reader, config);
                
                // 只拿第一筆資料
                var record = csv.GetRecords<TeleERCsvRecord>().FirstOrDefault();
                if (record == null) return BadRequest("檔案內容為空或格式不正確");

                // 轉換為單一 Slot
                var slot = _mappingService.MapToFhirSlot(record, record.Sched_ID);
                
                // FHIR 序列化輸出
                var serializer = new FhirJsonSerializer();
                return Ok(serializer.SerializeToString(slot));
            }
            catch (Exception ex)
            {
                // 幫助除錯：如果 CSV 欄位對不起來會噴到這裡
                return BadRequest($"單筆轉換失敗：{ex.Message}");
            }
        }
    }
}