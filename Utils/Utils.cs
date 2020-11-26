using System;
using System.Text;

namespace PartyAIOverhaulCommands
{
	public static class Utils
	{
		public static string FlattenException(this Exception exception)
		{
			StringBuilder stringBuilder = new StringBuilder();
			while (exception != null)
			{
				stringBuilder.AppendLine(exception.Message);
				stringBuilder.AppendLine(exception.StackTrace);
				exception = exception.InnerException;
			}
			return stringBuilder.ToString();
		}
	}
}
