using SensorsInterface.Devices;
using static SensorsInterface.Devices.Device.SignalState;
namespace SensorsInterface.RHIF;

public class RHIF
{
	private static Dictionary<string, string> descriptions = new()
	{
		{"EKG","EKG"},
	};

	private static Dictionary<Device.SignalState, string> statuses = new()
	{
		{Low,"Poniżej normy"},
		{Normal,"W normie"},
		{High,"Powyżej normy"},
	};
	private static Dictionary<Device.SignalState, string> statusesCodes = new()
	{
		{Low,"L"},
		{Normal,"N"},
		{High,"H"},
	};
	public static string CreateObservation(string signal, double value, DateTime date, string unit, Device.SignalState status)
	{
		return $$"""
		       {
		         "resourceType" : "Observation",
		         "id" : "{{signal}}",
		         "meta" : {
		           "profile" : ["http://hl7.org/fhir/StructureDefinition/vitalsigns"]
		         },
		         "text" : {
		           "status" : "generated",
		           "div" : "{{signal}}={{value}}"
		         },
		         "status" : "final",
		         "category" : [{R
		           "coding" : [{
		             "system" : "http://terminology.hl7.org/CodeSystem/observation-category",
		             "code" : "vital-signs",
		             "display" : "Sygnały życiowe"
		           }]
		         }],
		         "code" : {R
		           "coding" : [{
		             "system" : "http://loinc.org",
		             "code" : "85354-9",
		             "display" : "{{descriptions[signal]}}"
		           }],
		           "text" : "{{descriptions[signal]}}"
		         },
		         "subject" : {R
		           "reference" : "Patient/example"
		         },
		         "effectiveDateTime" : "{{date}}",R
		         "performer" : [{
		           "reference" : "Practitioner/example"
		         }],
		         "interpretation" : [{
		           "coding" : [{
		             "system" : "http://terminology.hl7.org/CodeSystem/v3-ObservationInterpretation",
		             "code" : "{{statusesCodes[status]}}",
		             "display" : "{{statuses[status]}}"
		           }],
		           "text" : "{{statuses[status]}}"
		         }],
		         "bodySite" : {
		           "coding" : [{
		             "system" : "http://snomed.info/sct",
		             "code" : "368209003",
		             "display" : "Right arm"
		           }]
		         },
		         "component" : [{
		           "valueQuantity" : {
		             "value" : {{value}},
		             "unit" : "{{unit}}",
		             "system" : "http://unitsofmeasure.org",
		             "code" : "{{unit}}"
		           }
		         }
		       }
		       """;
	}
}