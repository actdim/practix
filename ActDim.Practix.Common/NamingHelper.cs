using System;
using System.Text.RegularExpressions;

namespace ActDim.Practix
{
    /// <summary>
    /// Naming helper
    /// </summary>
    public static class NamingHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefix">tag</param>
        /// <returns></returns>
        public static string CreateUniqueName(string prefix)
        {
            var uid = Guid.NewGuid().ToString();
            uid = uid.Replace('-', '_');
            // var uid = Guid.NewGuid().ToString("N");
            var name = string.Format("{0}{1}", prefix, uid);
            return name;
        }

        //create (make, get) a valid identifier
        public static string CreateIdentifier(string name)//CleanName
        {
            // Compliant with item 2.4.2 of the C# specification
            var regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
            // @?[_\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}][\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]* 
            // cleanName
            // string result = regex.Replace(name, "");
            string result = regex.Replace(name, "_");
            // The identifier must start with a character or a "_"
            // if (!char.IsLetter(result, 0) && !Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#").IsValidIdentifier(ret))
            if (!char.IsLetter(result, 0))
                result = string.Concat("_", result);//result = "_" + result;
            return result;
        }
    }
}