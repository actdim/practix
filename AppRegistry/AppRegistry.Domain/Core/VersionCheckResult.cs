namespace ActDim.AppRegistry.Domain.Core
{
    public class VersionCheckResult
    {
        public int ExpectedVersion { set; get; }

        /// <summary>
        /// Version
        /// </summary>
        public int CurrentVersion { set; get; }

        public bool IsValid // IsMatched
        {
            get
            {
                return ExpectedVersion == CurrentVersion;
            }
        }
    }
}
