using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;
using System;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	public static class Common
	{
		//App information
		public static string APP_NAME = "SpiraProjectExport";

		//Version information. (v5.0.0.7+)
		public static Version SPIRA_VERSION = new Version("5.0.0.0");
		public static int SPIRA_PATCH = 7;

		//Generic file names..
		public static string PROJECT_FILE = "project.proj";
		public static string LOG_OUTPUT = "export.log";
		public static string LOG_ERROR_EXPORT = "ProjectExport";
		public static string LOG_ERROR_IMPORT = "ProjectExport";

		//File dialog information..
		public static string FILEDIALOG_FILTER = "SpiraTeam Project Files (*.prj)|*.prj|All Files|*.*";
		public static string FILEDIALOG_EXT = ".prj";

		//Logging on..
		public static string USER_NAME = "administrator";
		public static int USER_ID_ADMINISTRATOR = 1;

		//Filter page size..
		public static int PAGE_NUM = 100;

		//URIs for Services..
        //public static string SERVICE_IMPORTEXPORT_3 = "/Services/v3_0/ImportExport.svc";
        //public static string SERVICE_IMPORTEXPORT_4 = "/Services/v4_0/ImportExport.svc";
        //public static string SERVICE_DATASYNC_3 = "/Services/v3_0/DataSync.svc";
        public static string SERVICE_SPIRA_5 = "/Services/v5_0/SoapService.svc";

		//Hardcoded Security Stuff..
		public static string SECURITY_QUESTION = "What is 5 x 5?";
		public static string SECURITY_ANSWER = "25";

		/// <summary>Checks the given RemoteVersion wether it passes our minumum version required.</summary>
		/// <param name="version">The RemoteVersion from the Spira Instance</param>
		/// <returns>True is the SpiraServer meets minimum requirements.</returns>
		public static bool CheckVersionNum(RemoteVersion version)
		{
			//Return value.
			bool verOk = false;

			/* Try to convert over. First we try converting to a float (Pre-v4.1, ver.Version is two numbers, "0.0". This will
			 * pass converting to a float. On v4.1 and later, ver.Version is all four, "0.0.0.0", which can be converted into
			 * a Version object.
			 */
			float spiraVerNum = 0;
			Version spiraVer = null;
			if (float.TryParse(version.Version, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out spiraVerNum))
			{
				int verPatch = ((version.Patch.HasValue) ? version.Patch.Value : 0);
				//Translate the minimunm version to a float as well.
				string strMinVer = Common.SPIRA_VERSION.Major + "." + Common.SPIRA_VERSION.Minor;
				float verMin = float.Parse(strMinVer, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture);
				if ((spiraVerNum >= verMin && verPatch >= Common.SPIRA_PATCH) || (spiraVerNum > verMin))
				{
					verOk = true;
				}
			}
			else if (Version.TryParse(version.Version, out spiraVer))
			{
				if (spiraVer >= Common.SPIRA_VERSION)
					verOk = true;
			}

			return verOk;
		}
	}
}
