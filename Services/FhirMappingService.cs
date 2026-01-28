using Hl7.Fhir.Model;
using SingleSlotTester.Models;
namespace SingleSlotTester.Services
{
    public class FhirMappingService
    {
       public Slot MapToFhirSlot(TeleERCsvRecord record, string id)
{
    var slot = new Slot
    {
        Id = id,
        Status = Slot.SlotStatus.Free,
        StartElement = new Instant(DateTimeOffset.Parse($"{record.Slot_Date} {record.Time_Start}", System.Globalization.CultureInfo.InvariantCulture)),
        EndElement = new Instant(DateTimeOffset.Parse($"{record.Slot_Date} {record.Time_End}", System.Globalization.CultureInfo.InvariantCulture)),
        Comment = $"Channel: {record.Channel}, Capacity: {record.Capacity}"
    };

    // 💡 改成更具識別性的 URL
    slot.Extension.Add(new Extension("https://teleer.org/fhir/StructureDefinition/dept-id", new FhirString(record.Dept_ID)));
    slot.Extension.Add(new Extension("https://teleer.org/fhir/StructureDefinition/practitioner-id", new FhirString(record.Pract_ID)));

    return slot;
}
    }
}