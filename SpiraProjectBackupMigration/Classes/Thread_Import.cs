using Inflectra.SpiraTest.Utilities.ProjectMigration.Classes;
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	internal class Thread_Import : Thread_Events
	{
		#region Public Properties and Events
		/// <summary>Whether we're trying to cancel the process or not..</summary>
		public static bool WantCancel = false;

		/// <summary>Fired off when we have an update to give to the screen.</summary>
		public event EventHandler<ProgressArgs> ProgressUpdate;
		/// <summary>Fired off once the import is finished (or cancelled.)</summary>
		public event EventHandler<FinishedArgs> ProgressFinished;
		#endregion

		#region Private Properties..
		private const string CLASS_NAME = "Thread_Import.";

		private string _adminPass = "";
		private string _file = "";
		private string _tempDirectory = "";
		private Uri _server;
		private string _project = "";
		private string _userPass = "";
		private bool _fromDir = false;
		private bool _canceled = false;
        List<RemoteCustomProperty> requiredCustomProperties = new List<RemoteCustomProperty>();

		//For progress information..
		private float maxValue = 27;
		private float curValue = 0;

		//File storage..
		ExportFile Import = null;

		//Our client and tracking data.
		SpiraSoapService.SoapServiceClient _spiraSoapClient;
		private int _client1Act = 0;
		private float _client1Num = 0;

		//Flag for detecting documents not uploaded.
		bool docSize = false;

		private SpiraVersionEnum _serverVer = SpiraVersionEnum.VER_32;
		#endregion

		#region Old->New Mappings
        private Dictionary<int, int> mapComponents = new Dictionary<int, int>();
        private Dictionary<int, int> mapCustLists = new Dictionary<int, int>();
		private Dictionary<int, int> mapCustListValues = new Dictionary<int, int>();
		private Dictionary<int, int> mapUsers = new Dictionary<int, int>();
		private Dictionary<int, int> mapReleases = new Dictionary<int, int>();
		private Dictionary<int, int> mapAutoEngines = new Dictionary<int, int>();
		private Dictionary<int, int> mapAutoHosts = new Dictionary<int, int>();
		private Dictionary<int, int> mapBuilds = new Dictionary<int, int>();
        private Dictionary<int, int> mapTestCaseFolders = new Dictionary<int, int>();
        private Dictionary<int, int> mapTestCases = new Dictionary<int, int>();
		private Dictionary<int, int> mapTestSteps = new Dictionary<int, int>();
        private Dictionary<int, int> mapTestSetFolders = new Dictionary<int, int>();
        private Dictionary<int, int> mapTestSets = new Dictionary<int, int>();
		private Dictionary<int, int> mapTestRuns = new Dictionary<int, int>();
		private Dictionary<int, int> mapTestRunSteps = new Dictionary<int, int>();
		private List<RemoteTestSetTestCaseMapping> newTestSetTestCaseMappings = new List<RemoteTestSetTestCaseMapping>();
		private Dictionary<int, int> mapTestSetTestCases = new Dictionary<int, int>();
		private Dictionary<int, int> mapRequirements = new Dictionary<int, int>();
        private Dictionary<int, int> mapTaskFolders = new Dictionary<int, int>();
        private Dictionary<int, int> mapTasks = new Dictionary<int, int>();
		private Dictionary<int, int> mapIncSeverities = new Dictionary<int, int>();
		private Dictionary<int, int> mapIncPriorities = new Dictionary<int, int>();
		private Dictionary<int, int> mapIncTypes = new Dictionary<int, int>();
		private Dictionary<int, int> mapIncStatuses = new Dictionary<int, int>();
		private Dictionary<int, int> mapIncidents = new Dictionary<int, int>();
		private Dictionary<int, int> mapDocFolders = new Dictionary<int, int>();
		private Dictionary<int, int> mapDocs = new Dictionary<int, int>();
		private int mapProject = -1;

		#endregion

		/// <summary>Creates a new instance of the thread class.</summary>
		/// <param name="spiraProject">The project to export.</param>
		/// <param name="exportFile">The file that we're saving the data to.</param>
		/// <param name="adminPass">The administrator's password.</param>
		/// <param name="serverUrl">The URL to connect to the SpiraTeam installation.</param>
		public Thread_Import(string spiraProject, string adminPass, string importFile, Uri serverUrl, string newUserPass)
		{
			this._project = spiraProject;
			this._adminPass = adminPass;
			this._file = importFile;
			this._server = serverUrl;
			this._userPass = newUserPass;

			try
			{
				this._fromDir = ((File.GetAttributes(importFile) & FileAttributes.Directory) == FileAttributes.Directory);
			}
			catch
			{
				this._fromDir = false;
			}
		}

		/// <summary>Step 1 - Unzip The File</summary>
		/// <returns>Success or Fail</returns>
		private bool stp01_ZipFile()
		{
			const string METHOD = CLASS_NAME + "stp01_ZipFile()";
			Logger.LogTrace(METHOD, "Opening up PRJ file...");

			if (!this._fromDir)
			{
				this._tempDirectory = System.IO.Path.GetTempPath() + Common.APP_NAME + "_" + DateTime.Now.ToString("yyyy-MM-dd-HHmmt") + "\\";
				try
				{
					using (ZipFile zip = new ZipFile(this._file))
					{
						zip.ExtractAll(this._tempDirectory, ExtractExistingFileAction.OverwriteSilently);
					}
				}
				catch (Exception ex)
				{
					Logger.LogError(METHOD, ex, "Unzipping Project file.");
					this.RaiseError(ex, "Could not extract project file.");
					return false;
				}
			}
			else
			{
				Logger.LogTrace(METHOD, "Using temporary directory.");
				this._tempDirectory = this._file;
			}

			return true;
		}

		/// <summary>Step 2 - Load the XML File</summary>
		/// <returns>Success or Fail</returns>
		private bool stp02_LoadXML()
		{
			const string METHOD = CLASS_NAME + "stp02_LoadXML()";

			try
			{
				//Update count..
				++this._client1Num;

				this.Import = null;
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.CheckCharacters = false;
				XmlSerializer xmlSer = new XmlSerializer(typeof(ExportFile));
				using (FileStream fileStream = new FileStream(this._tempDirectory + Common.PROJECT_FILE, FileMode.Open))
				{
					var reader = new CleanXMLReader(fileStream);
					this.Import = (ExportFile)xmlSer.Deserialize(reader);
				}

				//Set max count here..
				this.maxValue = 11 +
					this.Import.CustomLists.Count +
					this.Import.Users.Count +
                    this.Import.Components.Count +
					this.Import.Releases.Count +
					this.Import.ReleaseComments.Count +
					this.Import.ReleaseBuilds.Count +
					this.Import.AutomationHosts.Count +
					this.Import.AutomationEngines.Count +
                    this.Import.TestCaseFolders.Count +
					(this.Import.TestCases.Count * 2) +
					this.Import.TestCaseComments.Count +
					this.Import.ReleaseTestCases.Count +
                    this.Import.TestSetFolders.Count +
					this.Import.TestSets.Count +
					this.Import.TestSetComments.Count +
					this.Import.TestSetTestCases.Count +
					this.Import.TestRuns_Automated.Count +
					this.Import.TestRuns_Manual.Count +
					this.Import.Requirements.Count +
					this.Import.RequirementComments.Count +
					this.Import.RequirementTestCases.Count +
                    this.Import.TaskFolders.Count +
					this.Import.Tasks.Count +
					this.Import.TaskComments.Count +
					this.Import.Incidents.Count +
					this.Import.IncidentComments.Count +
                    this.Import.DocumentFolders.Count +
                    this.Import.DocumentTypes.Count +
					this.Import.Documents.Count;
			}

			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Reading project XML file.");
				this.RaiseError(ex, "Could not extract project file.");
				return false;
			}

			return true;
		}

		/// <summary>Step 3 - Connect to the Server</summary>
		/// <returns>Success or Fail</returns>
		private bool stp03_ConnectToServer()
		{
			const string METHOD = CLASS_NAME + "stp03_ConnectToServer()";
			Logger.LogTrace(METHOD, "Connecting to server.");

			try
			{
				//Update count..
				++this._client1Num;

				this._spiraSoapClient = SpiraClientFactory.CreateClient_Spira5(this._server);
				bool? connectionSucc = this._spiraSoapClient.Connection_Authenticate2(Common.USER_NAME, this._adminPass, Common.APP_NAME);
				if (!connectionSucc.HasValue || (connectionSucc.HasValue && !connectionSucc.Value))
				{
					Logger.LogError(METHOD, null, "Logging into client failed.");
					this.RaiseError(null, "Could not log into server.");
					return false;
				}

				//Check spira version..
				RemoteVersion ver = this._spiraSoapClient.System_GetProductVersion();
				bool verOk = Common.CheckVersionNum(ver);
				if (verOk)
				{
					int num = int.Parse(ver.Version.Split('.')[0]);
					//Set the enum..
					if (num == 3) this._serverVer = SpiraVersionEnum.VER_32;
                    else if (num == 4) this._serverVer = SpiraVersionEnum.VER_40;
					else if (num > 4) this._serverVer = SpiraVersionEnum.VER_50;
				}
				else
				{
                    Exception ex = new Exception("Your version of SpiraTeam is too old. You need to be running v5.0.0.7 or later. You are running: " + ver.Version + " (patch " + ver.Patch + ")");
                    Logger.LogError(METHOD, ex, "Invalid version of SpiraTeam running. Needs v5.0.0.7. Running: " + ver.Version + " (patch " + ver.Patch + ")");
                    this.RaiseError(ex, ex.Message);
					this._canceled = true;
					return false;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Creating client.");
				this.RaiseError(ex, "Could not log into server.");
				return false;
			}

			return true;
		}

		/// <summary>Step 4 - Create the Project</summary>
		/// <returns>Success or Fail</returns>
		private bool stp04_Project()
		{
			const string METHOD = CLASS_NAME + "stp04_Project()";
			Logger.LogTrace(METHOD, "Creating project.");

			try
			{
				if (this.Import.Project != null)
				{
					//Update count..
					++this._client1Num;

					RemoteProject newProj = this.Import.Project;
					string oldName = newProj.Name;
					newProj.Name = this._project;
					newProj.Active = true;
					newProj.ProjectId = null;
					if (this._fromDir)
						newProj.Description = newProj.Description + "<br /><br />Project transferred from project '" + oldName + "' on " + DateTime.Now.ToString("f");
					else
						newProj.Description = newProj.Description + "<br /><br />Project imported from file '" + this._file + "' on " + DateTime.Now.ToString("f");

					newProj = this._spiraSoapClient.Project_Create(newProj, null);

					//Now sign into the project..
					bool? connectionSucc = this._spiraSoapClient.Connection_ConnectToProject(newProj.ProjectId.Value);
					if (!connectionSucc.HasValue || (connectionSucc.HasValue && !connectionSucc.Value))
					{
						Logger.LogError(METHOD, null, "Logging into created project failed.");
						this.RaiseError(null, "Could not log into newly created project.");
						return false;
					}

					this.mapProject = newProj.ProjectId.Value;
				}
				else
				{
					Exception ex = new Exception("Project definition in file is empty!");
					Logger.LogError(METHOD, ex, "Creating new project.");
					this.RaiseError(ex, "Could not log into newly created project.");
					return false;
				}
			}
			catch (Exception ex)
			{
				//Error creating project..
				Logger.LogError(METHOD, ex, "Creating new project.");
				this.RaiseError(ex, "Could not log into newly created project.");
				return false;
			}

			return true;
		}

		/// <summary>Step 5 - Create Users</summary>
		/// <returns>Success or Fail</returns>
		private bool stp05_Users()
		{
			const string METHOD = CLASS_NAME + "stp05_Users()";
			Logger.LogTrace(METHOD, "Importing users.");

			if (this.Import.Users != null && this.Import.Users.Count > 0)
			{
				foreach (RemoteProjectUser user in this.Import.Users)
				{
					try
					{
						//Convert it to a RemoteUser
						RemoteUser convUser = new RemoteUser();
						//convUser.Active = user.Active;    - Make all users active
						convUser.Admin = user.Admin;
						convUser.EmailAddress = user.EmailAddress;
						convUser.ExtensionData = user.ExtensionData;
						convUser.FirstName = user.FirstName;
						convUser.LastName = user.LastName;
						convUser.LdapDn = user.LdapDn;
						convUser.MiddleInitial = user.MiddleInitial;
						convUser.UserId = null;
						convUser.UserName = user.UserName;

                        //We only use the standard roles since they may be different on each installation.
						int roleId = ((user.ProjectRoleId <= 6) ? user.ProjectRoleId : 5);

						//Create the user..
						int oldUserId = user.UserId.Value;
						user.UserId = null;
						RemoteUser newUser = this._spiraSoapClient.User_Create(convUser, this._userPass, Common.SECURITY_QUESTION, Common.SECURITY_ANSWER, null);
						this.mapUsers.Add(oldUserId, newUser.UserId.Value);
					}
					catch (Exception ex)
					{
						Logger.LogError(METHOD, ex, "Could not add user '" + user.UserName + "'");

						if (ex.Message.Contains("position"))
						{
							this.RaiseError(ex, "Could not create user '" + user.UserName + "'. Check to make sure user does not exist and marked 'Active: No'");
						}
						else
						{
							this.RaiseError(ex, "Could not create project users.");
						}
						return false;
					}
				}
			}
			else
			{
				Logger.LogTrace(METHOD, "No users to import, skipping.");
			}

			return true;
		}

        /// <summary>
        /// Finalizes the import
        /// </summary>
        /// <returns></returns>
        private bool stp18_FinalizeImport()
        {
            const string METHOD = CLASS_NAME + "stp18_FinalizeImport()";
			Logger.LogTrace(METHOD, "Finalizing Import.");

            try
            {
                //We need to put back any custom properties that should be required
                if (requiredCustomProperties != null && requiredCustomProperties.Count > 0)
                {
                    //Switch over the custom properties to be required
                    foreach (RemoteCustomProperty customProperty in requiredCustomProperties)
                    {
                        customProperty.Options.Where(op => op.CustomPropertyOptionId == 1).First().Value = "N";
                        this._spiraSoapClient.CustomProperty_UpdateDefinition(customProperty);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex, "Adding Custom Lists");
                this.RaiseError(ex, "Error importing Custom Property Definitions.");
                return false;
            }

            return true;
        }

		/// <summary>Step 6 - Custom Lists & Properties</summary>
		/// <returns>Success or Fail</returns>
		private bool stp06_CustomLists()
		{
			const string METHOD = CLASS_NAME + "stp06_CustomLists()";
			Logger.LogTrace(METHOD, "Importing Custom Lists.");

			try
			{
				if (this.Import.CustomLists != null && this.Import.CustomLists.Count > 0)
				{
					foreach (RemoteCustomList custList in this.Import.CustomLists)
					{
						//Get mapping values..
						int oldListId = custList.CustomPropertyListId.Value;
						custList.CustomPropertyListId = null;
						custList.ProjectId = this.mapProject;
						Dictionary<int, int> tempListValues = new Dictionary<int, int>();
						foreach (RemoteCustomListValue value in custList.Values.OrderBy(clv => clv.Name))
						{
							tempListValues.Add(value.CustomPropertyValueId.Value, -1);
							value.CustomPropertyValueId = null;
							value.CustomPropertyListId = 0;
						}

						//Now create the list.
						RemoteCustomList newList = this._spiraSoapClient.CustomProperty_AddCustomList(custList);

						//Now save the values..
						this.mapCustLists.Add(oldListId, newList.CustomPropertyListId.Value);
						List<RemoteCustomListValue> newValues = newList.Values.OrderBy(clv => clv.Name).ToList();
						for (int i = 0; i < newValues.Count; i++)
						{
							int key = tempListValues.ElementAt(i).Key;
							this.mapCustListValues.Add(key, newValues[i].CustomPropertyValueId.Value);
						}

						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
				else
				{
					Logger.LogTrace(METHOD, "No lists to import, skipping.");
				}

				//Now the custom properties..
				Logger.LogTrace(METHOD, "Importing custom properties.");
				if (this.Import.CustomProperties != null && this.Import.CustomProperties.Count > 0)
				{
					//Dictionary<int, List<RemoteCustomProperty>> newProps = new Dictionary<int, List<RemoteCustomProperty>>();
					foreach (RemoteCustomProperty custProp in this.Import.CustomProperties)
					{
						int? custPropListId = null;
						if (custProp.CustomList != null)
						{
							if (custProp.CustomList.CustomPropertyListId.HasValue) custProp.CustomList.CustomPropertyListId = this.FindMapping(this.mapCustLists, custProp.CustomList.CustomPropertyListId.Value);
							custProp.CustomList.ProjectId = this.mapProject;
							custPropListId = custProp.CustomList.CustomPropertyListId;
						}
						//We disable required properties so that the import doesn't fail, we switch them back afterwards
						if (custProp.Options.Where(op => op.CustomPropertyOptionId == 1).Count() == 1)
						{
							custProp.Options.Where(op => op.CustomPropertyOptionId == 1).Single().Value = "Y";
							Logger.LogTrace(METHOD, "Turned custom property '" + custProp.Name + "' required to 'No'.");
                            if (!requiredCustomProperties.Any(c => c.CustomPropertyId == custProp.CustomPropertyId))
                            {
                                requiredCustomProperties.Add(custProp);
                            }
						}
						//Update the project ID.
						custProp.ProjectId = this.mapProject;
						custProp.CustomPropertyId = null;

						//Now add the definition to the system. (unless name is null)
						if (!String.IsNullOrEmpty(custProp.Name))
						{
							this._spiraSoapClient.CustomProperty_AddDefinition(custProp, custPropListId);
						}
					}

					this.RaiseProgress(++this._client1Num / this.maxValue);
				}
				else
				{
					Logger.LogTrace(METHOD, "No custom properties to import, skipping.");
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Adding Custom Lists");
				this.RaiseError(ex, "Error importing Custom Property Definitions.");
				return false;
			}

			return true;
		}

		/// <summary>Step 7 - Releases</summary>
		/// <returns>Success or Fail</returns>
		private bool stp07_Releases()
		{
			const string METHOD = CLASS_NAME + "stp07_Releases()";
			Logger.LogTrace(METHOD, "Importing releases:");

			try
			{
				if (this.Import.Releases != null && this.Import.Releases.Count > 0)
				{
					Dictionary<string, int> folderMapping = new Dictionary<string, int>();
					Logger.LogTrace(METHOD, "Importing releases.");
					foreach (RemoteRelease release in this.Import.Releases.OrderBy(rl => rl.IndentLevel))
					{
						//If it has a parent, it better be created already..
						string parentIndent = release.IndentLevel.Substring(0, release.IndentLevel.Length - 3);
						int? folderId = null;
						if (parentIndent.Length > 0 && folderMapping.ContainsKey(parentIndent))
							folderId = folderMapping[parentIndent];

						//Update mappings.
						int oldNum = release.ReleaseId.Value;
						release.ReleaseId = null;
						release.ProjectId = this.mapProject;
						if (release.CreatorId.HasValue) release.CreatorId = this.FindMapping(this.mapUsers, release.CreatorId.Value);
						this.MapCustomIdPropertiesInArtifact(release);

						RemoteRelease newRelease = this._spiraSoapClient.Release_Create(release, folderId);

						//If it's a folder, add it to the mapping..
						if (release.Summary)
							folderMapping.Add(release.IndentLevel, newRelease.ReleaseId.Value);

						//Add it to the mapping & update the folder mapping..
						this.mapReleases.Add(oldNum, newRelease.ReleaseId.Value);

						//Advance counter..
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}

					//Now import builds..
					if (this.Import.ReleaseBuilds != null && this.Import.ReleaseBuilds.Count > 0)
					{
						Logger.LogTrace(METHOD, "Importing builds.");
						foreach (RemoteBuild build in this.Import.ReleaseBuilds)
						{
							//Update mappings.
							int oldNum = build.BuildId.Value;
							build.BuildId = null;
							build.ReleaseId = this.mapReleases[build.ReleaseId];

							RemoteBuild newBuild = this._spiraSoapClient.Build_Create(build);

							//Add mapping.
							this.mapBuilds.Add(oldNum, newBuild.BuildId.Value);

							//Advance counter..
							this.RaiseProgress(++this._client1Num / this.maxValue);
						}
					}
					else
					{
						Logger.LogTrace(METHOD, "No builds to import, skipping.");
					}

					//Now import comments..
					if (this.Import.ReleaseComments != null && this.Import.ReleaseComments.Count > 0)
					{
						foreach (RemoteComment comment in this.Import.ReleaseComments)
						{
							int oldId = comment.CommentId.Value;
							comment.CommentId = null;
							if (comment.UserId.HasValue) comment.UserId = this.FindMapping(this.mapUsers, comment.UserId.Value);
							int? newArtId = this.FindMapping(this.mapReleases, comment.ArtifactId);
							if (newArtId.HasValue)
							{
								comment.ArtifactId = newArtId.Value;
								this._spiraSoapClient.Release_CreateComment(comment);
							}
							else
								Logger.LogTrace(METHOD, "-- Could not import Comment ID #" + oldId.ToString() + ". No mapping found for artifact #" + comment.ArtifactId.ToString());

							//Advance counter..
							this.RaiseProgress(++this._client1Num / this.maxValue);
						}
					}
				}
				else
				{
					Logger.LogTrace(METHOD, "No releases to import, skipping.");
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Releases");
				this.RaiseError(ex, "Error importing Releases.");
				return false;
			}

			return true;
		}


        /// <summary>Step 6a - Comments</summary>
        /// <returns>Success or Fail</returns>
        private bool stp06a_Components()
        {
            const string METHOD = CLASS_NAME + "stp06a_Components()";
            Logger.LogTrace(METHOD, "Importing Components:");

            try
            {
                if (this.Import.Components != null && this.Import.Components.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Importing Components.");
                    foreach (RemoteComponent component in this.Import.Components.OrderBy(c => c.Name).ThenBy(c => c.ComponentId))
                    {
                        //Update mappings.
                        int oldNum = component.ComponentId.Value;
                        component.ComponentId = null;
                        component.ProjectId = this.mapProject;

                        //Create the component in the project
                        RemoteComponent newComponent = this._spiraSoapClient.Component_Create(component);

                        //Add it to the mapping & update the folder mapping..
                        this.mapComponents.Add(oldNum, newComponent.ComponentId.Value);

                        //Advance counter..
                        this.RaiseProgress(++this._client1Num / this.maxValue);
                    }
                }
                else
                {
                    Logger.LogTrace(METHOD, "No Components to import, skipping.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex, "Loading Components");
                this.RaiseError(ex, "Error importing Components.");
                return false;
            }

            return true;
        }

		/// <summary>Step 8 - Releases</summary>
		/// <returns>Success or Fail</returns>
		private bool stp08_AutomationEngines()
		{
			const string METHOD = CLASS_NAME + "stp08_AutomationEngines()";
			Logger.LogTrace(METHOD, "Importing Automation Engines.");
			try
			{
				if (this.Import.AutomationEngines != null && this.Import.AutomationEngines.Count > 0)
				{
					//Create the engines, if they don't already exist..
					foreach (RemoteAutomationEngine engine in this.Import.AutomationEngines)
					{
						//Try getting it first, see if it exists..
						RemoteAutomationEngine exisEngine = null;
						try
						{
							exisEngine = this._spiraSoapClient.AutomationEngine_RetrieveByToken(engine.Token);
						}
						catch { }

						if (exisEngine != null)
						{
							//Simply update mappings..
							this.mapAutoEngines.Add(engine.AutomationEngineId.Value, exisEngine.AutomationEngineId.Value);
						}
						else
						{
							//Create a new one and make mapping..
							int oldId = engine.AutomationEngineId.Value;
							engine.AutomationEngineId = null;

							RemoteAutomationEngine newEngine = this._spiraSoapClient.AutomationEngine_Create(engine);
							//Check for null. If create gives a null back, it means the item
							// is inactive or deleted. We're assuming the ID# is the same.
							if (newEngine == null)
								this.mapAutoEngines.Add(oldId, oldId);
							else
								this.mapAutoEngines.Add(oldId, newEngine.AutomationEngineId.Value);
						}
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
				else
				{
					Logger.LogTrace(METHOD, "No Automation Engines to import, skipping.");
				}

				//Now import Automation Hosts..
				Logger.LogTrace(METHOD, "Importing Automation Hosts.");
				if (this.Import.AutomationHosts != null && this.Import.AutomationHosts.Count > 0)
				{
					foreach (RemoteAutomationHost host in this.Import.AutomationHosts)
					{
						//Reset ID..
						int oldId = host.AutomationHostId.Value;
						host.AutomationHostId = null;
						this.MapCustomIdPropertiesInArtifact(host);

						//Create it..
						RemoteAutomationHost newHost = this._spiraSoapClient.AutomationHost_Create(host);

						//Add mapping.
						this.mapAutoHosts.Add(oldId, newHost.AutomationHostId.Value);

						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
				else
				{
					Logger.LogTrace(METHOD, "No Automation Hosts to import, skipping.");
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Automation Engines");
				this.RaiseError(ex, "Error importing Automation items.");
				return false;
			}

			return true;
		}

		/// <summary>Step 9 - Test Cases</summary>
		/// <returns>Success or Fail</returns>
		private bool stp09_TestCases()
		{
			const string METHOD = CLASS_NAME + "stp09_TestCases()";
			Logger.LogTrace(METHOD, "Importing Test Cases:");

			try
			{
				Logger.LogTrace(METHOD, "Record mappings.");
                if (this.Import.TestCaseFolders != null && this.Import.TestCaseFolders.Count > 0)
				{
					//First import all the folders..
					Logger.LogTrace(METHOD, "Import folders.");
                    foreach (RemoteTestCaseFolder testCaseFolder in this.Import.TestCaseFolders)
					{
						//If it has a parent, it better be created already..
						int? parentFolderId = null;
                        if (testCaseFolder.ParentTestCaseFolderId.HasValue && this.mapTestCaseFolders.ContainsKey(testCaseFolder.ParentTestCaseFolderId.Value))
                            parentFolderId = this.mapTestCaseFolders[testCaseFolder.ParentTestCaseFolderId.Value];

						//Update mappings..
                        int oldNum = testCaseFolder.TestCaseFolderId.Value;
                        testCaseFolder.TestCaseFolderId = null;
                        testCaseFolder.ParentTestCaseFolderId = parentFolderId;

						//Create the case..
                        RemoteTestCaseFolder newTestCaseFolder = this._spiraSoapClient.TestCase_CreateFolder(testCaseFolder);

						//Add it to the mapping & update the folder mapping..
                        if (!this.mapTestCaseFolders.ContainsKey(oldNum))
                        {
                            this.mapTestCaseFolders.Add(oldNum, newTestCaseFolder.TestCaseFolderId.Value);
                        }

						//Advance counter..
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
                }

				//Now import the individual test cases..
                if (this.Import.TestCases != null && this.Import.TestCases.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Import cases.");
                    foreach (RemoteTestCase testcase in this.Import.TestCases)
                    {
                        //If it has a parent, it better be created already..
                        int? folderId = null;
                        if (testcase.TestCaseFolderId.HasValue && this.mapTestCaseFolders.ContainsKey(testcase.TestCaseFolderId.Value))
                            folderId = this.mapTestCaseFolders[testcase.TestCaseFolderId.Value];

                        //Update mappings..
                        int oldNum = testcase.TestCaseId.Value;
                        testcase.TestCaseId = null;
                        if (testcase.AuthorId.HasValue) testcase.AuthorId = this.FindMapping(this.mapUsers, testcase.AuthorId.Value);
                        if (testcase.OwnerId.HasValue) testcase.OwnerId = this.FindMapping(this.mapUsers, testcase.OwnerId.Value);
                        testcase.TestCaseFolderId = folderId;
                        this.MapCustomIdPropertiesInArtifact(testcase);
                        this.MapComponents(testcase);

                        RemoteTestCase newTestCase = this._spiraSoapClient.TestCase_Create(testcase);

                        //Add it to the mapping & update the folder mapping..
                        this.mapTestCases.Add(oldNum, newTestCase.TestCaseId.Value);

                        //Advance counter..
                        this.RaiseProgress(++this._client1Num / this.maxValue);
                    }
                }

				//Now import comments..
				Logger.LogTrace(METHOD, "Import comments.");
				if (this.Import.TestCaseComments != null && this.Import.TestCaseComments.Count > 0)
				{
					foreach (RemoteComment comment in this.Import.TestCaseComments)
					{
						int oldId = comment.CommentId.Value;
						comment.CommentId = null;
						if (comment.UserId.HasValue) comment.UserId = this.FindMapping(this.mapUsers, comment.UserId.Value);

						int? newArtId = this.FindMapping(this.mapTestCases, comment.ArtifactId);
						if (newArtId.HasValue)
						{
							comment.ArtifactId = newArtId.Value;
							this._spiraSoapClient.TestCase_CreateComment(comment);
						}
						else
							Logger.LogTrace(METHOD, "-- Could not import Comment ID #" + oldId.ToString() + ". No mapping found for artifact #" + comment.ArtifactId.ToString());

						//Advance counter..
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}

				//Now link up releases and test cases..
				Logger.LogTrace(METHOD, "Import Release Mappings.");
				if (this.Import.ReleaseTestCases != null && this.Import.ReleaseTestCases.Count > 0)
				{
					foreach (RemoteReleaseTestCaseMapping testmapping in this.Import.ReleaseTestCases)
					{
						int? relId = this.FindMapping(this.mapReleases, testmapping.ReleaseId);
						int? tcId = this.FindMapping(this.mapTestCases, testmapping.TestCaseId);
						if (relId.HasValue && tcId.HasValue)
						{
							testmapping.ReleaseId = relId.Value;
							testmapping.TestCaseId = tcId.Value;
							this._spiraSoapClient.Release_AddTestMapping(testmapping);
						}

						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Test Cases");
				this.RaiseError(ex, "Error importing Test Cases.");
				return false;
			}

			return true;
		}

		/// <summary>Step 10 - Test Steps</summary>
		/// <returns>Success or Fail</returns>
		private bool stp10_TestSteps()
		{
			const string METHOD = CLASS_NAME + "stp10_TestSteps()";
			Logger.LogTrace(METHOD, "Importing Test Steps.");

			try
			{
				//Loop through each test case..
				if (this.Import.TestCases != null && this.Import.TestCases.Count > 0)
				{
					foreach (RemoteTestCase testcase in this.Import.TestCases)
					{
						int position = 0;
						//Go through each test step..
						if (testcase.TestSteps != null)
						{
							foreach (RemoteTestStep teststep in testcase.TestSteps.OrderBy(ts => ts.Position))
							{
								position++;

								//Update mappings..
								int oldId = teststep.TestStepId.Value;
								teststep.TestStepId = null;
								teststep.TestCaseId = this.mapTestCases[teststep.TestCaseId];
								if (teststep.LinkedTestCaseId.HasValue) teststep.LinkedTestCaseId = this.mapTestCases[teststep.LinkedTestCaseId.Value];
								this.MapCustomIdPropertiesInArtifact(teststep);

								//Create the test step..
								if (teststep.LinkedTestCaseId.HasValue)
								{
									int newId = this._spiraSoapClient.TestCase_AddLink(teststep.TestCaseId, position, teststep.LinkedTestCaseId.Value, null);
									this.mapTestSteps.Add(oldId, newId);
								}
								else
								{
									RemoteTestStep newTestStep = this._spiraSoapClient.TestCase_AddStep(teststep, teststep.TestCaseId);
									this.mapTestSteps.Add(oldId, newTestStep.TestStepId.Value);
								}
							}
						}
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
				else
					Logger.LogTrace(METHOD, "No test steps to import, skipping.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Test Steps");
				this.RaiseError(ex, "Error importing Test Steps.");
				return false;
			}

			return true;
		}

		/// <summary>Step 11 - Test Sets</summary>
		/// <returns>Success or Fail</returns>
		private bool stp11_TestSets()
		{
			const string METHOD = CLASS_NAME + "stp11_TestSets()";
			Logger.LogTrace(METHOD, "Importing Test Sets:");

			try
			{
                if (this.Import.TestSetFolders != null && this.Import.TestSetFolders.Count > 0)
                {
                    //First import all the folders..
                    Logger.LogTrace(METHOD, "Import folders.");
                    foreach (RemoteTestSetFolder testSetFolder in this.Import.TestSetFolders)
                    {
                        //If it has a parent, it better be created already..
                        int? parentFolderId = null;
                        if (testSetFolder.ParentTestSetFolderId.HasValue && this.mapTestSetFolders.ContainsKey(testSetFolder.ParentTestSetFolderId.Value))
                            parentFolderId = this.mapTestSetFolders[testSetFolder.ParentTestSetFolderId.Value];

                        //Update mappings..
                        int oldNum = testSetFolder.TestSetFolderId.Value;
                        testSetFolder.TestSetFolderId = null;
                        testSetFolder.ParentTestSetFolderId = parentFolderId;

                        //Create the case..
                        RemoteTestSetFolder newTestSetFolder = this._spiraSoapClient.TestSet_CreateFolder(testSetFolder);

                        //Add it to the mapping & update the folder mapping..
                        if (!this.mapTestSetFolders.ContainsKey(oldNum))
                        {
                            this.mapTestSetFolders.Add(oldNum, newTestSetFolder.TestSetFolderId.Value);
                        }

                        //Advance counter..
                        this.RaiseProgress(++this._client1Num / this.maxValue);
                    }
                }

                if (this.Import.TestSets != null && this.Import.TestSets.Count > 0)
				{
					//Now import the individual test sets..
					Logger.LogTrace(METHOD, "Importing Test Sets.");
					foreach (RemoteTestSet testset in this.Import.TestSets)
					{
                        //If it has a parent, it better be created already..
                        int? folderId = null;
                        if (testset.TestSetFolderId.HasValue && this.mapTestSetFolders.ContainsKey(testset.TestSetFolderId.Value))
                            folderId = this.mapTestSetFolders[testset.TestSetFolderId.Value];

						//Update mappings..
						int oldNum = testset.TestSetId.Value;
						testset.TestSetId = null;
						testset.ProjectId = this.mapProject;
						if (testset.CreatorId.HasValue) testset.CreatorId = this.FindMapping(this.mapUsers, testset.CreatorId.Value);
						if (testset.OwnerId.HasValue) testset.OwnerId = this.FindMapping(this.mapUsers, testset.OwnerId.Value);
						if (testset.ReleaseId.HasValue) testset.ReleaseId = this.FindMapping(this.mapReleases, testset.ReleaseId.Value);
						if (testset.AutomationHostId.HasValue) testset.AutomationHostId = this.FindMapping(this.mapAutoHosts, testset.AutomationHostId.Value);
                        testset.TestSetFolderId = folderId;
                        this.MapCustomIdPropertiesInArtifact(testset);

						RemoteTestSet newTestSet = this._spiraSoapClient.TestSet_Create(testset);

						//Add it to the mapping & update the folder mapping..
                        this.mapTestSets.Add(oldNum, newTestSet.TestSetId.Value);

						//Advance counter..
						this.RaiseProgress(++this._client1Num / this.maxValue);

						//Create the test set -> test case links.
						List<RemoteTestSetTestCaseMapping> oldLinks = this.Import.TestSetTestCases.Where(tstc => tstc.TestSetId == oldNum).OrderBy(tstc => tstc.TestSetTestCaseId).ToList();
						List<RemoteTestSetTestCaseMapping> newLinks = new List<RemoteTestSetTestCaseMapping>();
						List<int> oldIDs = new List<int>();

						for (int i = 0; i < oldLinks.Count; i++)
						{
							RemoteTestSetTestCaseMapping newMap = new RemoteTestSetTestCaseMapping();
							if (oldLinks[i].OwnerId.HasValue) newMap.OwnerId = this.FindMapping(this.mapUsers, oldLinks[i].OwnerId.Value);
							newMap.TestCaseId = this.FindMapping(this.mapTestCases, oldLinks[i].TestCaseId).Value;
							newMap.TestSetId = this.FindMapping(this.mapTestSets, oldLinks[i].TestSetId).Value;
							newMap.TestSetTestCaseId = 0;

							//Add to mapping?
							oldIDs.Add(oldLinks[i].TestSetTestCaseId);

							this._spiraSoapClient.TestSet_AddTestMapping(newMap, null, null);

							//Send update..
							this.RaiseProgress(++this._client1Num / this.maxValue);
						}

						//Now reget the list, set positions, and store them.
                        List<RemoteTestSetTestCaseMapping> newList = this._spiraSoapClient.TestSet_RetrieveTestCaseMapping(newTestSet.TestSetId.Value).OrderBy(tctsm => tctsm.TestSetTestCaseId).ToList();
						int pos = 0;
						for (int i = 0; i < newList.Count; i++)
						{
							//They should be in order still, so get the mapping now.
							this.mapTestSetTestCases.Add(oldIDs[i], newList[i].TestSetTestCaseId);
						}
					}

                    //TODO: Import test set parameters once API added that supports this
                    //if (this.Import.TestSetParameters != null && this.Import.TestSetParameters.Count > 0)
                    //{
                    //    Logger.LogTrace(METHOD, "Importing Test Set Parameters.");
                    //    foreach (RemoteTestSetParameter testSetParameter in this.Import.TestSetParameters)
                    //    {
                    //    }
                    //}

					//Now import comments..
					if (this.Import.TestSetComments != null && this.Import.TestSetComments.Count > 0)
					{
						Logger.LogTrace(METHOD, "Importing Comments.");
						foreach (RemoteComment comment in this.Import.TestSetComments)
						{
							int oldId = comment.CommentId.Value;
							comment.CommentId = null;
							if (comment.UserId.HasValue) comment.UserId = this.FindMapping(this.mapUsers, comment.UserId.Value);
							int? newArtId = this.FindMapping(this.mapTestSets, comment.ArtifactId);
							if (newArtId.HasValue)
							{
								comment.ArtifactId = newArtId.Value;
								this._spiraSoapClient.TestSet_CreateComment(comment);
							}
							else
								Logger.LogTrace(METHOD, "-- Could not import Comment ID #" + oldId.ToString() + ". No mapping found for artifact #" + comment.ArtifactId.ToString());
						}
					}
				}
				else
					Logger.LogTrace(METHOD, "No Test Sets to import, skipping.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Test Sets");
				this.RaiseError(ex, "Error importing Test Sets.");
				return false;
			}

			return true;
		}

		/// <summary>Step 12 - Test Runs</summary>
		/// <returns>Success or Fail</returns>
		private bool stp12_TestRuns()
		{
			const string METHOD = CLASS_NAME + "stp12_TestRuns()";
			Logger.LogTrace(METHOD, "Importing Test Runs:");

			try
			{
				Logger.LogTrace(METHOD, "Importing Manual.");
				if (this.Import.TestRuns_Manual != null && this.Import.TestRuns_Manual.Count > 0)
				{
					//Create Manual Test Runs..
					foreach (RemoteManualTestRun testrun in this.Import.TestRuns_Manual)
					{
						RemoteManualTestRun newRun = new RemoteManualTestRun();

						//Copy over fields.
						int oldId = testrun.TestRunId.Value;
						//testrun.TestRunId = null;
						if (testrun.BuildId.HasValue) newRun.BuildId = this.FindMapping(this.mapBuilds, testrun.BuildId.Value);
						newRun.EndDate = testrun.EndDate;
						newRun.ExecutionStatusId = testrun.ExecutionStatusId;
						newRun.Name = testrun.Name;
						if (testrun.ReleaseId.HasValue) newRun.ReleaseId = this.FindMapping(this.mapReleases, testrun.ReleaseId.Value);
						newRun.StartDate = testrun.StartDate;
						newRun.TestCaseId = this.FindMapping(this.mapTestCases, testrun.TestCaseId).Value;
						if (testrun.TesterId.HasValue) newRun.TesterId = this.FindMapping(this.mapUsers, testrun.TesterId.Value);
						newRun.TestRunTypeId = testrun.TestRunTypeId;
						if (testrun.TestSetId.HasValue) newRun.TestSetId = this.FindMapping(this.mapTestSets, testrun.TestSetId.Value);
						if (testrun.TestSetTestCaseId.HasValue) newRun.TestSetTestCaseId = this.FindMapping(this.mapTestSetTestCases, testrun.TestSetTestCaseId.Value);
						this.copyCustomProperties(testrun, newRun);

						//Now get the steps...
						Dictionary<int, int> stepOldIDs = new Dictionary<int, int>();
						List<RemoteTestRunStep> stepList = new List<RemoteTestRunStep>();
						if (testrun.TestRunSteps != null && testrun.TestRunSteps.Count > 0)
						{
							//Order the list..
							List<RemoteTestRunStep> stepListOrder = testrun.TestRunSteps.OrderBy(trs => trs.TestRunStepId).ToList();
							for (int i = 0; i < stepListOrder.Count; i++)
							{
								RemoteTestRunStep newStep = new RemoteTestRunStep();
								newStep.ActualResult = stepListOrder[i].ActualResult;
								newStep.Description = stepListOrder[i].Description;
								newStep.ExecutionStatusId = stepListOrder[i].ExecutionStatusId;
								newStep.ExpectedResult = stepListOrder[i].ExpectedResult;
								newStep.Position = stepListOrder[i].Position;
								newStep.SampleData = stepListOrder[i].SampleData;
								if (stepListOrder[i].TestCaseId.HasValue) newStep.TestCaseId = this.FindMapping(this.mapTestCases, stepListOrder[i].TestCaseId.Value);
								newStep.TestRunStepId = 0;
								newStep.TestRunId = 0;

								//Add it to our temporary record.
								stepList.Add(newStep);

								//Add to the temporary list.
								stepOldIDs.Add(i, stepListOrder[i].TestRunStepId.Value);
							}
						}

						//Copy over the steps.
						newRun.TestRunSteps = stepList;

						//Record it..
						List<RemoteManualTestRun> newList = this._spiraSoapClient.TestRun_Save(new List<RemoteManualTestRun>() { newRun }, newRun.EndDate.Value);
						//Should be only one, loop thorugh the steps in it and get the new values for mapping.
						if (newList[0].TestRunSteps != null && stepList.Count == newList[0].TestRunSteps.Count)
						{
							List<RemoteTestRunStep> newStepListTemp = newList[0].TestRunSteps.OrderBy(trs => trs.TestRunStepId).ToList();

							int count = Math.Min(stepOldIDs.Count, newStepListTemp.Count);
							for (int i = 0; i < count; i++)
							{
								this.mapTestRunSteps.Add(stepOldIDs[i], newStepListTemp[i].TestRunStepId.Value);
							}
						}

						//Update Mapping
						this.mapTestRuns.Add(oldId, newList[0].TestRunId.Value);

						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
				else
					Logger.LogTrace(METHOD, "No Manual Test Runs to import, skipping.");

				//Create Automated Test Runs..
				Logger.LogTrace(METHOD, "Importing Automated.");
				if (this.Import.TestRuns_Automated != null && this.Import.TestRuns_Automated.Count > 0)
				{
					List<RemoteAutomatedTestRun> toRecord = new List<RemoteAutomatedTestRun>();
					List<int> oldIDs = new List<int>();
					foreach (RemoteAutomatedTestRun testrun in this.Import.TestRuns_Automated.OrderBy(tr => tr.TestRunId))
					{
						//Create a new Automated run..
						RemoteAutomatedTestRun newRun = new RemoteAutomatedTestRun();
						if (testrun.AutomationHostId.HasValue) newRun.AutomationHostId = this.FindMapping(this.mapAutoHosts, testrun.AutomationHostId.Value);
						if (testrun.AutomationEngineId.HasValue) newRun.AutomationEngineId = this.FindMapping(this.mapAutoEngines, testrun.AutomationEngineId.Value);
						if (testrun.BuildId.HasValue) newRun.BuildId = this.FindMapping(this.mapBuilds, testrun.BuildId.Value);
						newRun.EndDate = testrun.EndDate;
						newRun.ExecutionStatusId = testrun.ExecutionStatusId;
						newRun.Name = testrun.Name;
						newRun.Parameters = testrun.Parameters;
						newRun.ProjectId = this.mapProject;
						if (testrun.ReleaseId.HasValue) testrun.ReleaseId = this.FindMapping(this.mapReleases, testrun.ReleaseId.Value);
						newRun.RunnerAssertCount = testrun.RunnerAssertCount;
						newRun.RunnerMessage = testrun.RunnerMessage;
						newRun.RunnerName = testrun.RunnerName;
						newRun.RunnerStackTrace = testrun.RunnerStackTrace;
						newRun.RunnerTestName = testrun.RunnerTestName;
						newRun.ScheduledDate = testrun.ScheduledDate;
						newRun.StartDate = testrun.StartDate;
						newRun.TestCaseId = this.FindMapping(this.mapTestCases, testrun.TestCaseId).Value;
						if (testrun.TesterId.HasValue) newRun.TesterId = this.FindMapping(this.mapUsers, testrun.TesterId.Value);
						newRun.TestRunTypeId = testrun.TestRunTypeId;
						if (testrun.TestSetId.HasValue) newRun.TestSetId = this.FindMapping(this.mapTestSets, testrun.TestSetId.Value);
						if (testrun.TestSetTestCaseId.HasValue) newRun.TestSetTestCaseId = this.FindMapping(this.mapTestSetTestCases, testrun.TestSetTestCaseId.Value);
						this.copyCustomProperties(testrun, newRun);

						//Get mapping.
						oldIDs.Add(testrun.TestRunId.Value);

						toRecord.Add(newRun);

						//Record it and get the mapping.
						//RemoteAutomatedTestRun recRun = this._client1.TestRun_RecordAutomated1(newRun);
						//this.mapTestRuns.Add(testrun.TestRunId.Value, recRun.TestRunId.Value);
						++this._client1Num;
					}
					List<RemoteAutomatedTestRun> newAutoList = new List<RemoteAutomatedTestRun>();
					if (toRecord.Count > 0)
					{
						Logger.LogTrace(METHOD, "Starting RecordAutomated3...");
						DateTime start = DateTime.Now;
						newAutoList = this._spiraSoapClient.TestRun_RecordAutomated3(toRecord);
						DateTime stop = DateTime.Now;
						Logger.LogTrace(METHOD, "...Finished! Time took for " + toRecord.Count.ToString() + " records: " + stop.Subtract(start).ToString());
					}

					//Get mappings..
					for (int i = 0; i < newAutoList.Count; i++)
						this.mapTestRuns.Add(oldIDs[i], newAutoList[i].TestRunId.Value);

					this.RaiseProgress(++this._client1Num / this.maxValue);
				}
				else
					Logger.LogTrace(METHOD, "No Automated Test Runs to import, skipping.");
			}

			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Test Runs");
				this.RaiseError(ex, "Error importing Test Runs.");
				return false;
			}

			return true;
		}

		/// <summary>Step 13 - Requirements</summary>
		/// <returns>Success or Fail</returns>
		private bool stp13_Requirements()
		{
			const string METHOD = CLASS_NAME + "stp13_Requirements()";
			Logger.LogTrace(METHOD, "Importing Requirements:");

			try
			{
				if (this.Import.Requirements != null && this.Import.Requirements.Count > 0)
				{
					//Need to make dictionary of test case folders and their indent levels..
					Logger.LogTrace(METHOD, "Generating mapping.");
					Dictionary<string, int> folderMapping = new Dictionary<string, int>();
					foreach (RemoteRequirement req in this.Import.Requirements.Where(r => r.Summary == true))
					{
						//If the item is a folder, add it to the dictionary..
						if (req.Summary)
							folderMapping.Add(req.IndentLevel, req.RequirementId.Value);
					}

					//Now import all the folders..
					Logger.LogTrace(METHOD, "Importing folders.");
					foreach (RemoteRequirement req in this.Import.Requirements.Where(r => r.Summary == true).OrderBy(r => r.IndentLevel))
					{
						//Create the Requirement
						RemoteRequirement newReq = new RemoteRequirement();
						if (req.AuthorId.HasValue) newReq.AuthorId = this.FindMapping(this.mapUsers, req.AuthorId.Value);
						newReq.CreationDate = req.CreationDate;
						newReq.Description = req.Description;
						newReq.ImportanceId = req.ImportanceId;
						newReq.IndentLevel = req.IndentLevel;
						newReq.LastUpdateDate = req.LastUpdateDate;
                        //RequirementTypeId == -1 is a package type, cannot use this as an actual ID, default to type = 1
                        newReq.RequirementTypeId = (req.RequirementTypeId < 1) ? 1 : req.RequirementTypeId;
						newReq.Name = req.Name;
						if (req.OwnerId.HasValue) newReq.OwnerId = this.FindMapping(this.mapUsers, req.OwnerId.Value);
                        newReq.EstimatePoints = req.EstimatePoints;
						newReq.ProjectId = this.mapProject;
						if (req.ReleaseId.HasValue) newReq.ReleaseId = this.FindMapping(this.mapReleases, req.ReleaseId.Value);
						newReq.StatusId = req.StatusId;
						newReq.Summary = req.Summary;
                        MapComponents(newReq, req);
						this.copyCustomProperties(req, newReq);

						//If it has a parent, it better be created already..
						string parentIndent = req.IndentLevel.Substring(0, req.IndentLevel.Length - 3);
						int? folderId = null;
						if (parentIndent.Length > 0 && folderMapping.ContainsKey(parentIndent))
							folderId = folderMapping[parentIndent];

						//Create the requirement..
						RemoteRequirement creReq = this._spiraSoapClient.Requirement_Create2(newReq, folderId);

						//Add it to the mapping & update the folder mapping..
						this.mapRequirements.Add(req.RequirementId.Value, creReq.RequirementId.Value);
						folderMapping[req.IndentLevel] = creReq.RequirementId.Value;

						//Advance counter..
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
					//Now import the individual requirements..
					Logger.LogTrace(METHOD, "Importing Requirements.");
					foreach (RemoteRequirement req in this.Import.Requirements.Where(r => r.Summary == false))
					{
						//Create the new Requirement
						RemoteRequirement newReq = new RemoteRequirement();
						if (req.AuthorId.HasValue) newReq.AuthorId = this.FindMapping(this.mapUsers, req.AuthorId.Value);
						newReq.CoverageCountBlocked = req.CoverageCountBlocked;
						newReq.CoverageCountCaution = req.CoverageCountCaution;
						newReq.CoverageCountFailed = req.CoverageCountFailed;
						newReq.CoverageCountPassed = req.CoverageCountPassed;
						newReq.CoverageCountTotal = req.CoverageCountTotal;
						newReq.CreationDate = req.CreationDate;
						newReq.Description = req.Description;
						newReq.ImportanceId = req.ImportanceId;
						newReq.IndentLevel = req.IndentLevel;
						newReq.LastUpdateDate = req.LastUpdateDate;
                        //RequirementTypeId == -1 is a package type, cannot use this as an actual ID, default to type = 1
                        newReq.RequirementTypeId = (req.RequirementTypeId < 1) ? 1 : req.RequirementTypeId;
						newReq.Name = req.Name;
						if (req.OwnerId.HasValue) newReq.OwnerId = this.FindMapping(this.mapUsers, req.OwnerId.Value);
						newReq.EstimatePoints = req.EstimatePoints;
						newReq.ProjectId = this.mapProject;
						if (req.ReleaseId.HasValue) newReq.ReleaseId = this.FindMapping(this.mapReleases, req.ReleaseId.Value);
						newReq.StatusId = req.StatusId;
						newReq.Summary = req.Summary;
                        MapComponents(newReq, req);
                        this.copyCustomProperties(req, newReq);

						//If it has a parent, it better be created already..
						string parentIndent = req.IndentLevel.Substring(0, req.IndentLevel.Length - 3);
						int? folderId = null;
						if (parentIndent.Length > 0 && folderMapping.ContainsKey(parentIndent))
							folderId = folderMapping[parentIndent];

						//See if we already created this requirement.
						if (!this.mapRequirements.ContainsKey(req.RequirementId.Value))
						{
							RemoteRequirement creReq = this._spiraSoapClient.Requirement_Create2(newReq, folderId);

							//Add it to the mapping & update the folder mapping..
							this.mapRequirements.Add(req.RequirementId.Value, creReq.RequirementId.Value);
						}
						//Advance counter..
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}

					//Now import comments..
					if (this.Import.RequirementComments != null && this.Import.RequirementComments.Count > 0)
					{
						Logger.LogTrace(METHOD, "Importing Comments.");
						foreach (RemoteComment comment in this.Import.RequirementComments)
						{
							int oldId = comment.CommentId.Value;
							comment.CommentId = null;
							if (comment.UserId.HasValue) comment.UserId = this.FindMapping(this.mapUsers, comment.UserId.Value);
							int? newArtId = this.FindMapping(this.mapRequirements, comment.ArtifactId);
							if (newArtId.HasValue)
							{
								comment.ArtifactId = newArtId.Value;
								this._spiraSoapClient.Requirement_CreateComment(comment);
							}
							else
								Logger.LogTrace(METHOD, "-- Could not import Comment ID #" + oldId.ToString() + ". No mapping found for artifact #" + comment.ArtifactId.ToString());

							//Advance counter..
							this.RaiseProgress(++this._client1Num / this.maxValue);
						}
					}

                    //Add any requirement steps
                    if (this.Import.RequirementSteps != null && this.Import.RequirementSteps.Count > 0)
                    {
                        Logger.LogTrace(METHOD, "Importing Requirement Steps.");
                        foreach (RemoteRequirementStep requirementStep in this.Import.RequirementSteps)
                        {
                            requirementStep.RequirementId = this.FindMapping(this.mapRequirements, requirementStep.RequirementId).Value;

                            this._spiraSoapClient.Requirement_AddStep(requirementStep, null, null);
                            this.RaiseProgress(++this._client1Num / this.maxValue);
                        }
                    }

					//Now link up test cases..
					if (this.Import.RequirementTestCases != null && this.Import.RequirementTestCases.Count > 0)
					{
						Logger.LogTrace(METHOD, "Importing Test Case Mappings.");
						foreach (RemoteRequirementTestCaseMapping testmapping in this.Import.RequirementTestCases)
						{
							testmapping.RequirementId = this.FindMapping(this.mapRequirements, testmapping.RequirementId).Value;
							testmapping.TestCaseId = this.FindMapping(this.mapTestCases, testmapping.TestCaseId).Value;

							this._spiraSoapClient.Requirement_AddTestCoverage(testmapping);

							this.RaiseProgress(++this._client1Num / this.maxValue);
						}
					}
				}
				else
					Logger.LogTrace(METHOD, "No Requirements to import, skipping.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Requirements");
				this.RaiseError(ex, "Error importing Requirements.");
				return false;
			}

			return true;
		}

		/// <summary>Step 14 - Tasks</summary>
		/// <returns>Success or Fail</returns>
		private bool stp14_Tasks()
		{
			const string METHOD = CLASS_NAME + "stp14_Tasks()";
			Logger.LogTrace(METHOD, "Importing Tasks.");

			try
			{
				if (this.Import.Tasks != null && this.Import.Tasks.Count > 0)
				{
					foreach (RemoteTask task in this.Import.Tasks)
					{
						//Update mappings..
						int oldId = task.TaskId.Value;
						task.TaskId = null;
						if (task.CreatorId.HasValue) task.CreatorId = this.FindMapping(this.mapUsers, task.CreatorId.Value);
						if (task.OwnerId.HasValue) task.OwnerId = this.FindMapping(this.mapUsers, task.OwnerId.Value);
						task.ProjectId = this.mapProject;
						if (task.ReleaseId.HasValue) task.ReleaseId = this.FindMapping(this.mapReleases, task.ReleaseId.Value);
						if (task.RequirementId.HasValue) task.RequirementId = this.FindMapping(this.mapRequirements, task.RequirementId.Value);
						this.MapCustomIdPropertiesInArtifact(task);

						RemoteTask newTask = this._spiraSoapClient.Task_Create(task);

						//Add to mapping.
						this.mapTasks.Add(oldId, newTask.TaskId.Value);

						this.RaiseProgress(++this._client1Num / this.maxValue);
					}

					//Now import comments..
					if (this.Import.TaskComments != null && this.Import.TaskComments.Count > 0)
					{
						Logger.LogTrace(METHOD, "Importing Comments.");
						foreach (RemoteComment comment in this.Import.TaskComments)
						{
							int oldId = comment.CommentId.Value;
							comment.CommentId = null;
							if (comment.UserId.HasValue) comment.UserId = this.FindMapping(this.mapUsers, comment.UserId.Value);
							int? newArtId = this.FindMapping(this.mapTasks, comment.ArtifactId);
							if (newArtId.HasValue)
							{
								comment.ArtifactId = newArtId.Value;
								this._spiraSoapClient.Task_CreateComment(comment);
							}
							else
								Logger.LogTrace(METHOD, "-- Could not import Comment ID #" + oldId.ToString() + ". No mapping found for artifact #" + comment.ArtifactId.ToString());

							//Advance counter..
							this.RaiseProgress(++this._client1Num / this.maxValue);
						}
					}
				}
				else
					Logger.LogTrace(METHOD, "No Tasks to import, skipping.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Tasks");
				this.RaiseError(ex, "Error importing Tasks.");
				return false;
			}

			return true;
		}

		/// <summary>Step 15 - Incidents</summary>
		/// <returns>Success or Fail</returns>
		private bool stp15_Incidents()
		{
			const string METHOD = CLASS_NAME + "stp15_Incidents()";
			Logger.LogTrace(METHOD, "Importing Incidents:");
			try
			{
				Logger.LogTrace(METHOD, "Getting existing variables.");
				//Load up the Existing items first..
				List<RemoteIncidentStatus> newStatus = this._spiraSoapClient.Incident_RetrieveStatuses();
				List<RemoteIncidentPriority> newPri = this._spiraSoapClient.Incident_RetrievePriorities();
				List<RemoteIncidentSeverity> newSev = this._spiraSoapClient.Incident_RetrieveSeverities();
				List<RemoteIncidentType> newTyp = this._spiraSoapClient.Incident_RetrieveTypes();
				this.RaiseProgress(++this._client1Num / this.maxValue);
				//Load up Statuses..
				if (this.Import.IncidentStatuses != null && this.Import.IncidentStatuses.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing statuses.");
					foreach (RemoteIncidentStatus existStatus in this.Import.IncidentStatuses)
					{
						//If it exists..
						if (newStatus.Where(ins => ins.Name == existStatus.Name).Count() == 1)
						{
							RemoteIncidentStatus newStat = newStatus.Where(ins => ins.Name == existStatus.Name).Single();
							//Add mapping.
							this.mapIncStatuses.Add(existStatus.IncidentStatusId.Value, newStat.IncidentStatusId.Value);
						}
						else
						{
							//Create the new IncidentStatus.
							//Get mapping.
							int oldId = existStatus.IncidentStatusId.Value;

							RemoteIncidentStatus creatStatus = this._spiraSoapClient.Incident_AddStatus(existStatus);

							//Add mapping.
							this.mapIncStatuses.Add(oldId, creatStatus.IncidentStatusId.Value);
						}
					}
				}
				this.RaiseProgress(++this._client1Num / this.maxValue);

				//Load up the Priorities...
				if (this.Import.IncidentPriorities != null && this.Import.IncidentPriorities.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing priorities.");
					foreach (RemoteIncidentPriority existPri in this.Import.IncidentPriorities)
					{
						//If it exists..
						if (newPri.Where(ins => ins.Name == existPri.Name).Count() == 1)
						{
							RemoteIncidentPriority newPriority = newPri.Where(ins => ins.Name == existPri.Name).Single();
							//Add mapping.
							this.mapIncPriorities.Add(existPri.PriorityId.Value, newPriority.PriorityId.Value);
						}
						else
						{
							//Create the new IncidentPriority.
							//Get mapping.
							int oldId = existPri.PriorityId.Value;

							RemoteIncidentPriority creatStatus = this._spiraSoapClient.Incident_AddPriority(existPri);

							//Add mapping.
							this.mapIncPriorities.Add(oldId, creatStatus.PriorityId.Value);
						}
					}
				}
				this.RaiseProgress(++this._client1Num / this.maxValue);

				//Load up the Severities...
				if (this.Import.IncidentSeverities != null && this.Import.IncidentSeverities.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing severities.");
					foreach (RemoteIncidentSeverity existSev in this.Import.IncidentSeverities)
					{
						//If it exists..
						if (newSev.Where(ins => ins.Name == existSev.Name).Count() == 1)
						{
							RemoteIncidentSeverity newSeverity = newSev.Where(ins => ins.Name == existSev.Name).Single();
							//Add mapping.
							this.mapIncSeverities.Add(existSev.SeverityId.Value, newSeverity.SeverityId.Value);
						}
						else
						{
							//Create the new IncidentSeverity.
							//Get mapping.
							int oldId = existSev.SeverityId.Value;

							RemoteIncidentSeverity creatSeverity = this._spiraSoapClient.Incident_AddSeverity(existSev);

							//Add mapping.
							this.mapIncSeverities.Add(oldId, creatSeverity.SeverityId.Value);
						}
					}
				}
				this.RaiseProgress(++this._client1Num / this.maxValue);

				//Load up the Types...
				if (this.Import.IncidentTypes != null && this.Import.IncidentTypes.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing types.");
					foreach (RemoteIncidentType existTyp in this.Import.IncidentTypes)
					{
						//If it exists..
						if (newTyp.Where(ins => ins.Name == existTyp.Name).Count() == 1)
						{
							RemoteIncidentType newSeverity = newTyp.Where(ins => ins.Name == existTyp.Name).Single();
							//Add mapping.
							this.mapIncTypes.Add(existTyp.IncidentTypeId.Value, newSeverity.IncidentTypeId.Value);
						}
						else
						{
							//Create the new IncidentSeverity.
							//Get mapping.
							int oldId = existTyp.IncidentTypeId.Value;

							RemoteIncidentType creatType = this._spiraSoapClient.Incident_AddType(existTyp);

							//Add mapping.
							this.mapIncTypes.Add(oldId, creatType.IncidentTypeId.Value);
						}
					}
				}
				this.RaiseProgress(++this._client1Num / this.maxValue);

				if (this.Import.Incidents != null && this.Import.Incidents.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing Incidents.");
					foreach (RemoteIncident inc in this.Import.Incidents)
					{
						//Update mappings..
						int oldId = inc.IncidentId.Value;
						inc.IncidentId = null;
						if (inc.DetectedReleaseId.HasValue) inc.DetectedReleaseId = this.FindMapping(this.mapReleases, inc.DetectedReleaseId.Value);
						if (inc.FixedBuildId.HasValue) inc.FixedBuildId = this.FindMapping(this.mapBuilds, inc.FixedBuildId.Value);
						if (inc.IncidentStatusId.HasValue) inc.IncidentStatusId = this.FindMapping(this.mapIncStatuses, inc.IncidentStatusId.Value);
						if (inc.IncidentTypeId.HasValue) inc.IncidentTypeId = this.FindMapping(this.mapIncTypes, inc.IncidentTypeId.Value);
						if (inc.OpenerId.HasValue) inc.OpenerId = this.FindMapping(this.mapUsers, inc.OpenerId.Value);
						if (inc.OwnerId.HasValue) inc.OwnerId = this.FindMapping(this.mapUsers, inc.OwnerId.Value);
						if (inc.PriorityId.HasValue) inc.PriorityId = this.FindMapping(this.mapIncPriorities, inc.PriorityId.Value);
						inc.ProjectId = this.mapProject;
						if (inc.ResolvedReleaseId.HasValue) inc.ResolvedReleaseId = this.FindMapping(this.mapReleases, inc.ResolvedReleaseId.Value);
						if (inc.SeverityId.HasValue) inc.SeverityId = this.FindMapping(this.mapIncSeverities, inc.SeverityId.Value);
						if (inc.VerifiedReleaseId.HasValue) inc.VerifiedReleaseId = this.FindMapping(this.mapReleases, inc.VerifiedReleaseId.Value);
						this.MapCustomIdPropertiesInArtifact(inc);

                        //Linked Test Run Steps
                        if (inc.TestRunStepIds != null && inc.TestRunStepIds.Count > 0)
                        {
                            List<int> oldTestRunStepIds = inc.TestRunStepIds;
                            inc.TestRunStepIds = new List<int>();
                            foreach (int oldTestRunStepId in oldTestRunStepIds)
                            {
                                int? newTestRunStepId = this.FindMapping(this.mapTestRunSteps, oldTestRunStepId);
                                if (newTestRunStepId.HasValue)
                                {
                                    inc.TestRunStepIds.Add(newTestRunStepId.Value);
                                }
                            }
                        }

						//Check that we didn't add it already..
						if (!this.mapIncidents.ContainsKey(oldId))
						{
							RemoteIncident newInc = this._spiraSoapClient.Incident_Create(inc);

							//Add to mapping.
							this.mapIncidents.Add(oldId, newInc.IncidentId.Value);
						}
						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}

				//Now import comments..
				if (this.Import.IncidentComments != null && this.Import.IncidentComments.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing Incident comments.");
					foreach (RemoteComment comment in this.Import.IncidentComments)
					{
						//Update comment mappings..
						if (comment.UserId.HasValue) comment.UserId = this.FindMapping(this.mapUsers, comment.UserId.Value);
						comment.ArtifactId = this.FindMapping(this.mapIncidents, comment.ArtifactId).Value;

						this._spiraSoapClient.Incident_AddComments(new List<RemoteComment>() { comment });

						this.RaiseProgress(++this._client1Num / this.maxValue);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Incidents");
				this.RaiseError(ex, "Error importing Incidents.");
				return false;
			}

			return true;
		}

        /// <summary>Step 16 - Data Sync Mappings</summary>
        /// <returns>Success or Fail</returns>
        private bool stp16_DataSyncMappings()
        {
            const string METHOD = CLASS_NAME + "stp16_DataSyncMappings()";
            Logger.LogTrace(METHOD, "Importing data sync mappings:");

            try
            {
                //Get a list of data sync systems in the destination instance
                List<RemoteDataSyncSystem> existingDataSyncSystems = this._spiraSoapClient.DataSyncSystem_Retrieve();

                if (this.Import.UserDataMappings != null && this.Import.UserDataMappings.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Importing user data sync mappings.");
                    foreach (ExportFile.DataSyncDataMapping dataMapping in this.Import.UserDataMappings)
                    {
                        int dataSyncSystemId;
                        if (existingDataSyncSystems.Any(d => d.Name == dataMapping.DataSyncSystemName))
                        {
                            dataSyncSystemId = existingDataSyncSystems.FirstOrDefault(d => d.Name == dataMapping.DataSyncSystemName).DataSyncSystemId;
                        }
                        else
                        {
                            //Add the data sync itself
                            dataSyncSystemId = CreateNewDataSync(dataMapping.DataSyncSystemName, existingDataSyncSystems);
                        }
                        this._spiraSoapClient.DataMapping_AddUserMappings(dataSyncSystemId, dataMapping.DataSyncMappings);
                    }
                }

                //TODO: Uncomment when Project, Artifact Field and Custom Property Field/Value Mappings can be imported
                /*
                if (this.Import.ProjectDataMappings != null && this.Import.ProjectDataMappings.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Importing project data sync mappings.");
                    foreach (ExportFile.DataSyncDataMapping dataMapping in this.Import.ProjectDataMappings)
                    {
                        int dataSyncSystemId;
                        if (existingDataSyncSystems.Any(d => d.Name == dataMapping.DataSyncSystemName))
                        {
                            dataSyncSystemId = existingDataSyncSystems.FirstOrDefault(d => d.Name == dataMapping.DataSyncSystemName).DataSyncSystemId;
                        }
                        else
                        {
                            //Add the data sync itself
                            dataSyncSystemId = CreateNewDataSync(dataMapping.DataSyncSystemName, existingDataSyncSystems);
                        }
                        this._spiraSoapClient.DataMapping_AddProjectMappings(dataSyncSystemId, dataMapping.DataSyncMappings);
                    }
                }

                if (this.Import.ArtifactFieldDataMappings != null && this.Import.ArtifactFieldDataMappings.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Importing artifact field data sync mappings.");
                }

                if (this.Import.CustomPropertyDataMappings != null && this.Import.CustomPropertyDataMappings.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Importing custom property data sync mappings.");

                }

                if (this.Import.CustomPropertyValueDataMappings != null && this.Import.CustomPropertyValueDataMappings.Count > 0)
                {
                    Logger.LogTrace(METHOD, "Importing custom property value data sync mappings.");
                }*/

                //Now the artifact mappings
                //Uncomment when Project Mappings can be imported as there is a dependency on that
                //if (this.Import.RequirementDataMappings != null && this.Import.RequirementDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.RequirementDataMappings, /*Requirement*/1, existingDataSyncSystems);
                //}
                //if (this.Import.ReleaseDataMappings != null && this.Import.ReleaseDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.ReleaseDataMappings, /*Release*/4, existingDataSyncSystems);
                //}
                //if (this.Import.TestCaseDataMappings != null && this.Import.TestCaseDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.TestCaseDataMappings, /*TestCase*/2, existingDataSyncSystems);
                //}
                //if (this.Import.TestSetDataMappings != null && this.Import.TestSetDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.TestSetDataMappings, /*TestSet*/8, existingDataSyncSystems);
                //}
                //if (this.Import.TaskDataMappings != null && this.Import.TaskDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.TaskDataMappings, /*Task*/6, existingDataSyncSystems);
                //}
                //if (this.Import.IncidentDataMappings != null && this.Import.IncidentDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.IncidentDataMappings, /*Incident*/3, existingDataSyncSystems);
                //}
                //if (this.Import.TestRunDataMappings != null && this.Import.TestRunDataMappings.Count > 0)
                //{
                //    ImportArtifactDataMappings(this.Import.TestRunDataMappings, /*TestRun*/5, existingDataSyncSystems);
                //}
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex, "Loading data sync mappings");
                this.RaiseError(ex, "Error importing/uploading data sync mappings.");
                return false;
            }

            return true;
        }

		/// <summary>Step 17 - Documents</summary>
		/// <returns>Success or Fail</returns>
		private bool stp17_Documents()
		{
            const string METHOD = CLASS_NAME + "stp17_Documents()";
			Logger.LogTrace(METHOD, "Importing documents:");

			try
			{
				if (this.Import.DocumentFolders != null)
				{
					Logger.LogTrace(METHOD, "Importing folders.");

					//First folders..
					foreach (RemoteDocumentFolder docFolder in this.Import.DocumentFolders.OrderBy(df => df.IndentLevel))
					{
						//First, check for the root folder.
						if (docFolder.IndentLevel == "AAA")
						{
							//Get the folders from the current project, to find the existing root folder.
							List<RemoteDocumentFolder> rootFolders = this._spiraSoapClient.Document_RetrieveFolders();
							if (rootFolders.Where(rf => rf.IndentLevel == "AAA").Count() == 1)
							{
								RemoteDocumentFolder rootFolder = rootFolders.Where(rf => rf.IndentLevel == "AAA").Single();
								if (rootFolder != null)
									this.mapDocFolders.Add(docFolder.ProjectAttachmentFolderId.Value, rootFolder.ProjectAttachmentFolderId.Value);
							}
						}
						else
						{
							//Update Mappings..
							int oldId = docFolder.ProjectAttachmentFolderId.Value;
							docFolder.ProjectAttachmentFolderId = null;
							if (docFolder.ParentProjectAttachmentFolderId.HasValue) docFolder.ParentProjectAttachmentFolderId = this.FindMapping(this.mapDocFolders, docFolder.ParentProjectAttachmentFolderId.Value);
							docFolder.ProjectId = this.mapProject;

							RemoteDocumentFolder newFolder = this._spiraSoapClient.Document_AddFolder(docFolder);

							//Add it to the mapping.
							this.mapDocFolders.Add(oldId, newFolder.ProjectAttachmentFolderId.Value);
						}
					}
				}
				this.RaiseProgress(++this._client1Num / this.maxValue);

				//Now types..
                //TODO: Until we can create types.
                //foreach (RemoteDocumentType docType in this.Import.DocumentTypes.OrderBy(dt => dt.ProjectAttachmentTypeId))
                //{
                //    
                //}
				this.RaiseProgress(++this._client1Num / this.maxValue);

				//Now the attachment records themselves..
				if (this.Import.Documents != null && this.Import.Documents.Count > 0)
				{
					Logger.LogTrace(METHOD, "Importing documents.");
					int counter = 0;
					foreach (RemoteDocument doc in this.Import.Documents)
					{
						this.RaiseProgress(++this._client1Num / this.maxValue, "Uploading Document #" + (++counter).ToString() + "/" + this.Import.Documents.Count.ToString() + "...");
						//Fix mappings..
						int oldId = doc.AttachmentId.Value;
						doc.AttachmentId = null;
						if (doc.ProjectAttachmentFolderId.HasValue) doc.ProjectAttachmentFolderId = this.FindMapping(this.mapDocFolders, doc.ProjectAttachmentFolderId.Value);
						doc.ProjectAttachmentTypeId = null; //TODO: Until we can create types.
						if (doc.AuthorId.HasValue) doc.AuthorId = this.FindMapping(this.mapUsers, doc.AuthorId.Value);
						if (doc.EditorId.HasValue) doc.EditorId = this.FindMapping(this.mapUsers, doc.EditorId.Value);

						if (doc.AttachmentTypeId == 1)
						{
							Logger.LogTrace(METHOD, "Uploading attachment #" + oldId.ToString());
							//Upload a file.
							byte[] fileContents = null;
							try
							{
								string filename = this._tempDirectory + "Attachments\\" + oldId + ".dat";
								fileContents = File.ReadAllBytes(filename);
							}
							catch (Exception ex)
							{
								Logger.LogError(METHOD, ex, "Reading file ID#" + oldId);
							}
							if (fileContents != null)
							{
								try
								{
									RemoteDocument newDoc = this._spiraSoapClient.Document_AddFile(doc, fileContents);
									this.mapDocs.Add(oldId, newDoc.AttachmentId.Value);
								}
								catch (Exception ex)
								{
									//If it's a 404 error, then set a flag. Otherwise, re-throw an error.
									if (ex.GetType() == typeof(EndpointNotFoundException) &&
										ex.InnerException != null &&
										ex.InnerException.GetType() == typeof(WebException) &&
										ex.InnerException.Message.Contains("404"))
									{
										this.docSize = true;
									}
									else
										throw;
								}
							}
						}
						else if (doc.AttachmentTypeId == 2)
						{
							RemoteDocument newDoc = this._spiraSoapClient.Document_AddUrl(doc);
							this.mapDocs.Add(oldId, newDoc.AttachmentId.Value);
						}
					}
				}

				//Now hook up connections..
				if (this.Import.DocumentMappings != null && this.Import.DocumentMappings.Count > 0)
				{
					Logger.LogTrace(METHOD, "Mapping attachments.");
					foreach (ExportFile.DocumentMapping docMap in this.Import.DocumentMappings)
					{
						//Fix mapping.
						int? newAttachmentId = this.FindMapping(this.mapDocs, docMap.AttachmentId);
						if (newAttachmentId.HasValue)
						{
							docMap.AttachmentId = newAttachmentId.Value;
							switch (docMap.ArtifactTypeId)
							{
								case 1:
									docMap.ArtifactId = this.FindMapping(this.mapRequirements, docMap.ArtifactId).Value;
									break;

								case 2:
									docMap.ArtifactId = this.FindMapping(this.mapTestCases, docMap.ArtifactId).Value;
									break;

								case 3:
									docMap.ArtifactId = this.FindMapping(this.mapIncidents, docMap.ArtifactId).Value;
									break;

								case 4:
									docMap.ArtifactId = this.FindMapping(this.mapReleases, docMap.ArtifactId).Value;
									break;

								case 5:
									docMap.ArtifactId = this.FindMapping(this.mapTestRuns, docMap.ArtifactId).Value;
									break;

								case 6:
									docMap.ArtifactId = this.FindMapping(this.mapTasks, docMap.ArtifactId).Value;
									break;

								case 7:
									docMap.ArtifactId = this.FindMapping(this.mapTestSteps, docMap.ArtifactId).Value;
									break;

								case 8:
									docMap.ArtifactId = this.FindMapping(this.mapTestSets, docMap.ArtifactId).Value;
									break;
							}

							//Now create the association.
							this._spiraSoapClient.Document_AddToArtifactId(docMap.ArtifactTypeId, docMap.ArtifactId, docMap.AttachmentId);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex, "Loading Documents");
				this.RaiseError(ex, "Error importing/uploading Attachments.");
				return false;
			}

			return true;
		}

		/// <summary>Starts the export process.</summary>
		public void Run()
		{
			const string METHOD = CLASS_NAME + "Run()";

			//Load the project & XML.
			this.RaiseProgress(-1, "Loading project file...");
			if (!this.stp01_ZipFile()) return;
			if (!this.stp02_LoadXML()) return;

			//Connect to the server.
			if (!this.stp03_ConnectToServer()) return;

			//Create Project..
			if (!this.stp04_Project()) return;

			//Create Project Roles..
			//TODO: Import roles?

			//Create Users..
			this.RaiseProgress(++this._client1Num / this.maxValue, "Creating Users...");
			if (!this.stp05_Users()) return;

			//Custom Lists & Properties..
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Custom Property Records...");
			if (!this.stp06_CustomLists()) return;

            //Components
            this.RaiseProgress(this._client1Num / this.maxValue, "Creating Components...");
            if (!this.stp06a_Components()) return;

			//Releases..
			this.RaiseProgress(++this._client1Num / this.maxValue, "Creating Releases...");
			if (!this.stp07_Releases()) return;

			//Automation Hosts & Engines..
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Automation Records...");
			if (!this.stp08_AutomationEngines()) return;

			//Test Cases..
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Test Cases...");
			if (!this.stp09_TestCases()) return;

			//Test Steps..
			if (!this.stp10_TestSteps()) return;

			//Test Sets..
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Test Sets...");
			if (!this.stp11_TestSets()) return;

			//Test Runs..
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Test Runs...");
			if (!this.stp12_TestRuns()) return;

			//Requirements
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Requirements...");
			if (!this.stp13_Requirements()) return;

			//Tasks
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Tasks...");
			if (!this.stp14_Tasks()) return;

			//Incidents
			this.RaiseProgress(this._client1Num / this.maxValue, "Creating Incidents...");
			if (!this.stp15_Incidents()) return;

			//Data Sync DataMapping
            this.RaiseProgress(this._client1Num / this.maxValue, "Creating Incidents...");
            if (!this.stp16_DataSyncMappings()) return;

			//Documents
			this.RaiseProgress(this._client1Num / this.maxValue, "Uploading Documents...");
			if (!this.stp17_Documents()) return;

            //Any finalization updates - can be ignored if it fails
            this.RaiseProgress(this._client1Num / this.maxValue, "Finalizing Settings...");
            this.stp18_FinalizeImport();

			//Close the client.
			this._spiraSoapClient.Connection_Disconnect();

			//Finished!
			if (this.ProgressFinished != null)
			{
				if (this.docSize)
					this.RaiseError(new Exception("Some documents/attachments were not uploaded because their filesize exceeded IIS's set limits. You may want to try increasing the max upload file size and try again."), "Import completed.");
				else
					this.ProgressFinished(this, new FinishedArgs(null));
			}
		}

		#region Event Functions

		/// <summary>Collected lines for firing a ProgressReport.</summary>
		/// <param name="percentage">The percentage to display.</param>
		/// <param name="message">The activity to display. If NULL = No Change.</param>
		private void RaiseProgress(float percentage, string message = null)
		{
			//Raise event..
			ProgressArgs report = new ProgressArgs();
			report.PercentageDone = percentage;
			report.ActivityMessage = message;
			if (this.ProgressUpdate != null) this.ProgressUpdate(this, report);
		}

		/// <summary>Raises an error event!</summary>
		/// <param name="ex">The exception that was thrown.</param>
		/// <param name="message">The friendly message for display.</param>
		private void RaiseError(Exception ex, string message)
		{
			this._canceled = true;

			if (this.ProgressFinished != null)
			{
				FinishedArgs finArgs = new FinishedArgs(ex, message);
				this.ProgressFinished(this, finArgs);
			}

			string filename = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\" + Common.APP_NAME + "_" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".log";
			Logger.SaveLogToFile(filename);
		}

		#endregion

		#region Utility Functions

        /// <summary>
        /// Creates a new data sync
        /// </summary>
        /// <returns>The id of the data sync</returns>
        private int CreateNewDataSync(string dataSyncSystemName, List<RemoteDataSyncSystem> existingDataSyncSystems)
        {
            RemoteDataSyncSystem newDataSyncSystem = new RemoteDataSyncSystem();
            newDataSyncSystem.Name = dataSyncSystemName;
            newDataSyncSystem.ConnectionString = "http://tempuri.org";  //Dummy
            newDataSyncSystem.DataSyncStatusId = 1; /* Not Run */
            int dataSyncSystemId = this._spiraSoapClient.DataSyncSystem_Create(newDataSyncSystem).DataSyncSystemId;
            existingDataSyncSystems.Add(newDataSyncSystem);

            return dataSyncSystemId;
        }

        /// <summary>
        /// Imports the artifact data mapping
        /// </summary>
        private void ImportArtifactDataMappings(List<ExportFile.DataSyncDataMapping> artifactDataMappings, int artifactTypeId, List<RemoteDataSyncSystem> existingDataSyncSystems)
        {
            const string METHOD = CLASS_NAME + "ImportArtifactDataMappings()";

            Logger.LogTrace(METHOD, "Importing artifact data sync mappings for artifact type ID=" + artifactTypeId);
            foreach (ExportFile.DataSyncDataMapping dataMapping in artifactDataMappings)
            {
                int dataSyncSystemId;
                if (existingDataSyncSystems.Any(d => d.Name == dataMapping.DataSyncSystemName))
                {
                    dataSyncSystemId = existingDataSyncSystems.FirstOrDefault(d => d.Name == dataMapping.DataSyncSystemName).DataSyncSystemId;
                }
                else
                {
                    //Add the data sync itself
                    dataSyncSystemId = CreateNewDataSync(dataMapping.DataSyncSystemName, existingDataSyncSystems);
                }
                this._spiraSoapClient.DataMapping_AddArtifactMappings(dataSyncSystemId, artifactTypeId, dataMapping.DataSyncMappings);
            }
        }

		/// <summary>Finds a mapping.</summary>
		/// <param name="dict">The dictionary to search.</param>
		/// <param name="key">The key to search for.</param>
		/// <returns>The # of thje mapping, or null.</returns>
		private int? FindMapping(Dictionary<int, int> dict, int key)
		{
			int? retValue = null;
			if (dict.ContainsKey(key))
				retValue = dict[key];

			return retValue;
		}

        /// <summary>
        /// Maps the components field
        /// </summary>
        /// <param name="testCase">The test case</param>
        private void MapComponents(RemoteTestCase testCase)
        {
            if (testCase.ComponentIds != null && testCase.ComponentIds.Count > 0)
            {
                //Map the component ids
                List<int> oldComponentIds = testCase.ComponentIds;
                testCase.ComponentIds = new List<int>();
                foreach (int componentId in oldComponentIds)
                {
                    if (this.mapComponents.ContainsKey(componentId))
                    {
                        testCase.ComponentIds.Add(this.mapComponents[componentId]);
                    }
                }
            }
        }

        /// <summary>
        /// Maps the components field
        /// </summary>
        /// <param name="incident">The incident</param>
        private void MapComponents(RemoteIncident incident)
        {
            if (incident.ComponentIds != null && incident.ComponentIds.Count > 0)
            {
                //Map the component ids
                List<int> oldComponentIds = incident.ComponentIds;
                incident.ComponentIds = new List<int>();
                foreach (int componentId in oldComponentIds)
                {
                    if (this.mapComponents.ContainsKey(componentId))
                    {
                        incident.ComponentIds.Add(this.mapComponents[componentId]);
                    }
                }
            }
        }

        /// <summary>
        /// Maps the components field
        /// </summary>
        /// <param name="newRequirement">The new requirement</param>
        /// <param name="oldRequirement">The old requirement</param>
        private void MapComponents(RemoteRequirement newRequirement, RemoteRequirement oldRequirement)
        {
            if (oldRequirement.ComponentId.HasValue && this.mapComponents.ContainsKey(oldRequirement.ComponentId.Value))
            {
                newRequirement.ComponentId = this.mapComponents[oldRequirement.ComponentId.Value];
            }
        }

		/// <summary>Maps the custom list values..</summary>
		/// <param name="artifact">The artifact to map.</param>
		private void MapCustomIdPropertiesInArtifact(RemoteArtifact artifact)
		{
			foreach (RemoteArtifactCustomProperty custProp in artifact.CustomProperties)
			{
				//Map the integer value of a List
				if (custProp.Definition.CustomPropertyTypeId == 6 && custProp.IntegerValue.HasValue && this.mapCustListValues.ContainsKey(custProp.IntegerValue.Value))
				{
					try
					{
						custProp.IntegerValue = this.mapCustListValues[custProp.IntegerValue.Value];
					}
					catch (Exception ex)
					{
                        Logger.LogError("Warning: Unable to map custom list values.", ex);
					}
				}
				//Loop through and map all the items in a multi list.
				if (custProp.Definition.CustomPropertyTypeId == 7 && custProp.IntegerListValue != null)
				{
					for (int i = 0; i < custProp.IntegerListValue.Count; i++)
					{
						try
						{
							if (this.mapCustListValues.ContainsKey(custProp.IntegerListValue[i]))
							{
								custProp.IntegerListValue[i] = this.mapCustListValues[custProp.IntegerListValue[i]];
							}
						}
						catch (Exception ex)
						{
                            Logger.LogError("Warning: Unable to map custom list values.", ex);
                        }
					}
				}
				//Loop through and fix user mappings..
				if (custProp.Definition.CustomPropertyTypeId == 8 && custProp.IntegerValue.HasValue && this.mapUsers.ContainsKey(custProp.IntegerValue.Value))
				{
					try
					{
						custProp.IntegerValue = this.mapUsers[custProp.IntegerValue.Value];
					}
					catch (Exception ex)
					{
                        Logger.LogError("Warning: Unable to map user list values.", ex);
					}

				}
			}
		}

		private void copyCustomProperties(RemoteArtifact from, RemoteArtifact to)
		{
			//This is now simple, thanks to the underlying class.
			to.CustomProperties = from.CustomProperties;

			//Update list values..
			this.MapCustomIdPropertiesInArtifact(to);
		}

		#endregion

		/// <summary>Enumeration of the SpiraTest version.</summary>
		private enum SpiraVersionEnum : int
		{
			VER_32 = 0,
			VER_40 = 1,
            VER_50 = 2
		}
	}
}
