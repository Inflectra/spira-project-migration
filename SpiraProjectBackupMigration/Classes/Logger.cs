using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ServiceModel;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	public static class Logger
	{
		private static object locker = null;

		private static string logToFile = "";

		/// <summary>Logs a trace message to the file.</summary>
		/// <param name="method">The method we're writing from.</param>
		/// <param name="message">The message to write.</param>
		public static void LogTrace(string method, string message)
		{
			if (locker == null) locker = new object();
			lock (locker)
			{
				//Save the log to memory.
				Logger.logToFile += "[" + DateTime.Now.ToString("s") + "] " + method + ": " + message + Environment.NewLine;
			}
		}

		/// <summary>Logs an exception to the log file.</summary>
		/// <param name="method">The method we're writing from.</param>
		/// <param name="message">The message to write.</param>
		/// <param name="error">The exception to log.</param>
		public static void LogError(string method, Exception error, string message = null)
		{
			//Generate our strings..
			string logString = method + Environment.NewLine + ((string.IsNullOrWhiteSpace(message)) ? "" : message + Environment.NewLine);
			if (error != null)
			{
				logString += Logger.generateErrorMessageString(error);

				//If it's a Server Fault message, get it's messages now.
				if (error.GetType() == typeof(FaultException<ExceptionDetail>))
				{
					FaultException<ExceptionDetail> exDetails = (FaultException<ExceptionDetail>)error;

					//If there's an exception detail:
					string strSvrStack = exDetails.Detail.StackTrace;
					string strSvrMsg = "Server Service Messages:" + Environment.NewLine + exDetails.Detail.Message;

					Exception innerEx = exDetails.InnerException;
					while (innerEx != null)
					{
						strSvrMsg += Environment.NewLine + innerEx.Message;
						innerEx = innerEx.InnerException;
					}

					logString += Environment.NewLine + Environment.NewLine + strSvrMsg + Environment.NewLine + strSvrStack;
				}

			}
			else
			{
				logString = method + Environment.NewLine + ((message != null) ? message : "");
			}

			//Write the line to the file..
			if (locker == null) locker = new object();
			lock (locker)
			{
				Logger.logToFile += "[" + DateTime.Now.ToString("s") + "] " + "**ERROR** " + logString + Environment.NewLine;
			}
		}

		/// <summary>Generates a message string from the excetpion messages.</summary>
		/// <param name="ex">The Exception</param>
		/// <returns>A message string, formatted.</returns>
		private static string generateErrorMessageString(Exception ex)
		{
			Exception newEx = ex;

			//Get the stack trace..
			string strStack = newEx.StackTrace;

			//The main error message.
			string strMsg = "[" + newEx.GetType().ToString() + "] " + newEx.Message;

			//Any inner messages.
			while (newEx.InnerException != null)
			{
				strMsg += Environment.NewLine + "[" + newEx.GetType().ToString() + "] " + newEx.InnerException.Message;
				newEx = newEx.InnerException;
			}

			//Combine them..
			return strMsg + Environment.NewLine + strStack;
		}

		/// <summary>Saves the log to the specified file.</summary>
		/// <param name="fileName">The file to save as, or null to clear the log.</param>
		public static void SaveLogToFile(string fileName)
		{
			if (locker == null) locker = new object();
			lock (locker)
			{
				if (!string.IsNullOrWhiteSpace(fileName))
				{
					using (StreamWriter sw = File.CreateText(fileName))
					{
						sw.Write(Logger.logToFile);
					}
				}
			}
		}

		/// <summary>Clears the saved up log.</summary>
		public static void ClearLogFile()
		{
			Logger.logToFile = "";
		}
	}
}
