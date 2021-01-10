using System;
using System.Text;

namespace PartyAIOverhaulCommands
{
    public static class Utils
    {
        public static string FlattenException(this Exception exception)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (; exception != null; exception = exception.InnerException)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);
            }
            return stringBuilder.ToString();
        }
    }
}
