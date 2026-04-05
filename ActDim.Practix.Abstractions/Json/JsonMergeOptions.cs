using System.Text.Json;

namespace ActDim.Practix.Abstractions.Json
{
    public enum JsonMergeArrayHandling
    {
        /// <summary>
        /// Concatenate arrays: [1,2] + [3] = [1,2,3]
        /// </summary>
        Concat = 0,

        /// <summary>
        /// Replace target array with source: [1,2] + [3] = [3]
        /// </summary>
        Replace = 1,

        /// <summary>
        /// Merge arrays by index: [1,2] + [3] = [3,2]
        /// </summary>
        Merge = 2,

        /// <summary>
        /// Union: include only distinct values from both arrays
        /// </summary>
        Union = 3,
    }

    public enum JsonMergeNullValueHandling
    {
        /// <summary>
        /// Null values from source overwrite target values
        /// </summary>
        Merge = 0,

        /// <summary>
        /// Null values in source are ignored; target keeps its value
        /// </summary>
        Ignore = 1,
    }

    public class JsonMergeOptions
    {
        public JsonSerializerOptions BaseOptions { get; set; }

        public JsonMergeArrayHandling MergeArrayHandling { get; set; } = JsonMergeArrayHandling.Merge;

        public JsonMergeNullValueHandling MergeNullValueHandling { get; set; } = JsonMergeNullValueHandling.Merge;
    }
}
