using System;
using System.Collections.ObjectModel;

namespace Armat.Utils.Extensions
{
	// Mode of exception lookup in aggregate exceptions
	// Is used in Exception.As, Exception.Is extension methods
	public enum ExceptionLookupMode
	{
		// returns the only exception in the Aggregate exception
		// the result is null if there are multiple exceptions
		TheOnlyOne,

		// finds any exception matching the given exception type
		// the result is null if there are no matches
		AnyMatch,

		// considers only the first exception
		First,

		// considers only the last exception
		Last,

		// considers only the first exception, if all have the same type
		// if there are multiple ones with different types, it won't find anything
		FirstIfAllSameType,

		// considers only the last exception, if all have the same type
		// if there are multiple ones with different types, it won't find anything
		LastIfAllSameType,

		Default = TheOnlyOne
	}

	public static class ExceptionHelpers
	{
		public static T? As<T>(this Exception exc)
			where T : Exception
		{
			return As<T>(exc, ExceptionLookupMode.Default);
		}
		public static T? As<T>(this AggregateException exc)
			where T : Exception
		{
			return As<T>(exc, ExceptionLookupMode.Default);
		}
		public static T? As<T>(this Exception exc, ExceptionLookupMode elm)
			where T : Exception
		{
			if (exc == null)
				throw new NullReferenceException("Exception is null");

			// check if the exception has the desired type
			if (exc is T result)
				return result;

			// check if it's an aggregate exception and we should look deeper
			if (exc is AggregateException aggrExc)
				return As<T>(aggrExc, elm);

			// check if it has an inner exception and we should look deeper
			if (exc.InnerException != null)
				return As<T>(exc.InnerException, elm);

			// the exception type is not convertible
			return null;
		}
		public static T? As<T>(this AggregateException exc, ExceptionLookupMode elm)
			where T : Exception
		{
			if (exc == null)
				throw new NullReferenceException("AggregateException is null");

			// check if the AggregateException itself has the desired type
			if (exc is T thisResult)
				return thisResult;

			// check if there's any exception
			ReadOnlyCollection<Exception> innerExceptions = exc.InnerExceptions;
			if (exc.InnerExceptions == null || innerExceptions.Count == 0)
				return null;

			// will return the inner exception based on exception lookup options
			T? result = LookupException<T>(innerExceptions, elm);
			if (result != null)
				return result;

			// lookup for deeper level exceptions in all inner exceptions
			foreach (Exception innerExc in innerExceptions)
			{
				result = As<T>(innerExc, elm);
				if (result != null)
					break;
			}

			return result;
		}

		public static Boolean Is<T>(this Exception exc)
			where T : Exception
		{
			return As<T>(exc) != null;
		}
		public static Boolean Is<T>(this Exception exc, ExceptionLookupMode elm)
			where T : Exception
		{
			return As<T>(exc, elm) != null;
		}
		public static Boolean Is<T>(this AggregateException exc)
			where T : Exception
		{
			return As<T>(exc) != null;
		}
		public static Boolean Is<T>(this AggregateException exc, ExceptionLookupMode elm)
			where T : Exception
		{
			return As<T>(exc, elm) != null;
		}

		private static T? LookupException<T>(ReadOnlyCollection<Exception> listExceptions, ExceptionLookupMode elm)
			where T : Exception
		{
			if (listExceptions.Count == 0)
				return null;

			T? result = null;

			switch (elm)
			{
				case ExceptionLookupMode.TheOnlyOne:
					// returns the only exception in the Aggregate exception matching teh given type
					if (listExceptions.Count == 1)
					{
						result = listExceptions[0] as T;
					}
					break;
				case ExceptionLookupMode.AnyMatch:
					// finds any exception matching the given exception type
					for (Int32 index = 0; index < listExceptions.Count; index++)
					{
						if (listExceptions[index] is T testExc)
						{
							result = testExc;
							break;
						}
					}
					break;
				case ExceptionLookupMode.First:
					// considers only the first exception
					{
						if (listExceptions[0] is T testExc)
							result = testExc;
					}
					break;
				case ExceptionLookupMode.Last:
					// considers only the last exception
					{
						if (listExceptions[^1] is T testExc)
							result = testExc;
					}
					break;
				case ExceptionLookupMode.FirstIfAllSameType:
					// considers only the first exception, if all have the same type
					result = listExceptions[0] as T;
					if (result != null)
					{
						for (Int32 index = 1; index < listExceptions.Count; index++)
						{
							if (listExceptions[index] is not T)
							{
								result = null;
								break;
							}
						}
					}
					break;
				case ExceptionLookupMode.LastIfAllSameType:
					// considers only the last exception, if all have the same type
					result = listExceptions[^1] as T;
					if (result != null)
					{
						for (Int32 index = listExceptions.Count - 2; index >= 0; index--)
						{
							if (listExceptions[index] is not T)
							{
								result = null;
								break;
							}
						}
					}
					break;
			}

			return result;
		}

		public static String FormatMessage(this Exception exc)
		{
			if (exc == null)
				throw new ArgumentNullException(nameof(exc));

			return FormatExceptionMessage(exc, 0);
		}
		public static String FormatMessage(this Exception exc, String errorMessage)
		{
			return FormatExceptionMessage(errorMessage, exc, 0);
		}

		private const Int32 INDENT = 4;

		private static String FormatExceptionMessage(String errorMessage, Exception exc, Int32 indent)
		{
			String message = String.Empty;

			if (!String.IsNullOrEmpty(errorMessage))
			{
				// get the exception message
				message = errorMessage;

				// apply the indent
				if (indent > 0)
					message = new String(' ', indent) + message;
			}

			// get exception message
			String detailedMessage = FormatExceptionMessage(exc, indent + INDENT);

			// concatenate message details
			if (detailedMessage.Length == 0)
			{
				if (!message.EndsWith(".", StringComparison.InvariantCulture))
					message += ".";
			}
			else
			{
				message += message.EndsWith(".", StringComparison.InvariantCulture) ? " Error Message:\n" : ". Error Message:\n";
				message += detailedMessage;
			}

			return message;
		}
		private static String FormatExceptionMessage(Exception error, Int32 indent)
		{
			// get the exception message
			String message = error.Message;

			// apply the indent
			if (indent > 0)
				message = new String(' ', indent) + message;

			// consider inner exception
			if (error.InnerException == null)
				return message;

			// build message details
			String detailedMessage = String.Empty;

			if (error.InnerException is AggregateException aggrExc)
			{
				// add all aggregated exceptions
				foreach (Exception excInner in aggrExc.InnerExceptions)
				{
					String innerMessage = FormatExceptionMessage(excInner, indent + INDENT);
					if (!String.IsNullOrEmpty(innerMessage))
						detailedMessage += "\n" + innerMessage;
				}
			}
			else
			{
				// add inner exception
				String innerMessage = FormatExceptionMessage(error.InnerException, indent + INDENT);
				if (!String.IsNullOrEmpty(innerMessage))
					detailedMessage += "\n" + innerMessage;
			}

			// concatenate message details
			if (detailedMessage.Length == 0)
			{
				if (!message.EndsWith(".", StringComparison.InvariantCulture))
					message += ".";
			}
			else
			{
				message += message.EndsWith(".", StringComparison.InvariantCulture) ? " Error Message:" : ". Error Message:";
				message += detailedMessage;
			}

			return message;
		}
	}
}
