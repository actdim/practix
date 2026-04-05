namespace CanarySystems.Caching
{
    public class InvocationContext
    {
        /// <summary>
        /// 
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

        public object[] Arguments { get; set; }

        public InvocationContext()
        {

        }
    }
}