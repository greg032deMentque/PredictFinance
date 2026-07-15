using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackPredictFinance.Common.AnalysisV1
{
    public static class AnalysisSnapshotJsonOptions
    {
        public static readonly JsonSerializerOptions Shared = BuildOptions();

        private static JsonSerializerOptions BuildOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}
