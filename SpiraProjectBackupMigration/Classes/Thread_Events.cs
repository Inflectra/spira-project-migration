using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	class Thread_Events
	{
		/// <summary>Class to hold progress information.</summary>
		internal class ProgressArgs : EventArgs
		{
			public float PercentageDone
			{
				get;
				set;
			}
			public string ActivityMessage
			{
				get;
				set;
			}
		}

		/// <summary>Class to hold work is finished.</summary>
		internal class FinishedArgs : ProgressArgs
		{
			/// <summary>Creates a new instance of the class setting status to Success.</summary>
			/// <param name="results">The results.</param>
			internal FinishedArgs(ExportFile results)
			{
				this.Status = FinishedStatusEnum.OK;
				this.Results = results;
			}

			/// <summary>Creates a new instance of the class, setting status to Error.</summary>
			/// <param name="ex">The exception that was thrown.</param>
			/// <param name="message">The error message to display.</param>
			internal FinishedArgs(Exception ex, string message)
			{
				this.Status = FinishedStatusEnum.Error;
				this.Error = ex;
				this.Message = message;
			}

			/// <summary>The finished args of the thread.</summary>
			public ExportFile Results
			{ get; set; }

			/// <summary>Any exception that happened.</summary>
			public Exception Error
			{ get; set; }

			/// <summary>The error message to display.</summary>
			public string Message
			{ get; set; }

			/// <summary>The status of the job.</summary>
			public FinishedStatusEnum Status
			{ get; set; }

			/// <summary>Enumeration status of thread.</summary>
			public enum FinishedStatusEnum : int
			{
				OK = 1,
				Error = 2
			}
		}

	}
}
