using System.Text.RegularExpressions;

namespace ActDim.Practix.Logging
{
    // TODO: integrate into logging system

    public static class Tagged
    {
        public static string Unwrap(string value, string tag = null)
        {
            if (string.IsNullOrEmpty(tag))
            {
                tag = "[^>]+"; // ".+"
            }
            // or @$"(?<=<({tag})>)([^<]*?)(?=<\/\1>)"
            var regex = new Regex(@$"(?<=<({tag})>)(.*?)(?=<\/\1>)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var match = regex.Match(value);
            while (match.Success)
            {
                return match.Value;
            }
            return value;
        }

        public static string Wrap(string tag, string text)
        {
            return $"<{tag}>{text}</{tag}>";
        }

        public static string Stage(string text) 
        {          
            return Wrap(Tags.Stage, text);
        }

        public static string Operation(string text)
        {
            return Wrap(Tags.Operation, text);
        }

        public static string Status(string text)
        {
            return Wrap(Tags.Status, text);
        }

        public static string Progress(string text)
        {
            return Wrap(Tags.Progress, text);
        }

        public static string Error(string text)
        {
            return Wrap(Tags.Error, text);
        }

        public static string Hint(string text)
        {
            return Wrap(Tags.Hint, text);
        }
    }
}
