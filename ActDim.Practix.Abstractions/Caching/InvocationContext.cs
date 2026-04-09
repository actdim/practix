namespace ActDim.Practix.Abstractions.Caching
{
    public class InvocationContext
    {
        /// <summary>
        /// MethodId
        /// </summary>
        public string MethodId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string[] GenericArgumentIds { get; set; }

        /// <summary>
        /// UserData/UserTag
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        ///
        /// </summary>
        public object[] Arguments { get; set; }

        public InvocationContext()
        {

        }
    }
}
