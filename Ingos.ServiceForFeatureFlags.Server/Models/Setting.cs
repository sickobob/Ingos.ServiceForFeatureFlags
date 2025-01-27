namespace Ingos.ServiceForFeatureFlags.Server.Models
{
    public class Setting
    {
        public string Type { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; }
        public DateTime DateTime { get; set; }
        public string? StringValue { get; set; }
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
        public string? Description { get; set; }

    }
}