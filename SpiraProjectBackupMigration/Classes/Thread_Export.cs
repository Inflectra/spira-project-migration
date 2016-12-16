using Inflectra.SpiraTest.Utilities.ProjectMigration.Classes;
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	internal class Thread_Export : Thread_Events
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
		private const string CLASS_NAME = "Thread_Export.";

		private string _password = "";
		private string _file = "";
		private string _tempDirectory = "";
		private Uri _server;
		private RemoteProject _project;
		private bool _canceled = false;

		//For progress information..
		private float maxValue = 25;
		private float curValue = 0;

		//File storage..
		private ExportFile _Export = null;

		//Our client and tracking data.
        private SpiraSoapService.SoapServiceClient _spiraSoapClient;
		private int _client1Act = 0;
		private float _client1Num = 0;

		#endregion

		/// <summary>Creates a new instance of the thread class.</summary>
		/// <param name="spiraProject">The project to export.</param>
		/// <param name="exportFile">The file that we're saving the data to.</param>
		/// <param name="userPassword">The administrator's password.</param>
		/// <param name="serverUrl">The URL to connect to the SpiraTeam installation.</param>
		public Thread_Export(RemoteProject spiraProject, string userPassword, string exportFile, Uri serverUrl)
		{
			const string METHOD = CLASS_NAME + ".ctor()";

			this._project = spiraProject;
			this._password = userPassword;
			this._server = serverUrl;
			this._file = exportFile;

			//Generate the temp directory..
			this._tempDirectory = System.IO.Path.GetTempPath() + Common.APP_NAME + "_" + DateTime.Now.ToString("yyyy-MM-dd-HHmmt") + "\\";
			if (!Directory.Exists(this._tempDirectory))
				Directory.CreateDirectory(this._tempDirectory);

			//Create the class holding it all.
			this._Export = new ExportFile();
		}

		/// <summary>Starts the export process.</summary>
		public void StartProcess()
		{
			const string METHOD = CLASS_NAME + "StartProcess()";

			//Update screen..
			//Raise event..
			this.RaiseProgress(-1, "Logging in to server...");

			//Set up the client..
			this._spiraSoapClient = SpiraClientFactory.CreateClient_Spira5(this._server);
			this._spiraSoapClient.AutomationEngine_RetrieveCompleted += new EventHandler<AutomationEngine_RetrieveCompletedEventArgs>(AutomationEngine_RetrieveCompleted);
			this._spiraSoapClient.AutomationHost_RetrieveCompleted += new EventHandler<AutomationHost_RetrieveCompletedEventArgs>(AutomationHost_RetrieveCompleted);
			this._spiraSoapClient.CustomProperty_RetrieveCustomListsCompleted += new EventHandler<CustomProperty_RetrieveCustomListsCompletedEventArgs>(CustomProperty_RetrieveCustomListsCompleted);
			this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeCompleted += new EventHandler<CustomProperty_RetrieveForArtifactTypeCompletedEventArgs>(CustomProperty_RetrieveForArtifactTypeCompleted);
			this._spiraSoapClient.Document_RetrieveTypesCompleted += new EventHandler<Document_RetrieveTypesCompletedEventArgs>(Document_RetrieveTypesCompleted);
			this._spiraSoapClient.Document_RetrieveFoldersCompleted += new EventHandler<Document_RetrieveFoldersCompletedEventArgs>(Document_RetrieveFoldersCompleted);
			this._spiraSoapClient.Project_RetrieveByIdCompleted += new EventHandler<Project_RetrieveByIdCompletedEventArgs>(Project_RetrieveByIdCompleted);
            this._spiraSoapClient.Component_RetrieveCompleted += _client1_Component_RetrieveCompleted;
            this._spiraSoapClient.TestCase_RetrieveFoldersCompleted += _client1_TestCase_RetrieveFoldersCompleted;
            this._spiraSoapClient.TestSet_RetrieveFoldersCompleted += _client1_TestSet_RetrieveFoldersCompleted;
            this._spiraSoapClient.Task_RetrieveFoldersCompleted += _client1_Task_RetrieveFoldersCompleted;

			//Now log in..
			Logger.LogTrace(METHOD, "Logging into server..");
			bool loginSucc = this._spiraSoapClient.Connection_Authenticate2(Common.USER_NAME, this._password, Common.APP_NAME);
			if (loginSucc)
			{
				//Get the current version number..
				RemoteVersion ver = this._spiraSoapClient.System_GetProductVersion();

				//Check the version number..
				bool verOk = Common.CheckVersionNum(ver);
				if (verOk)
				{
					Logger.LogTrace(METHOD, "Logging into project..");
					//Save version to Export file..
					this._Export.ExportInfo.SpiraVersion = ver;
					bool projSucc = this._spiraSoapClient.Connection_ConnectToProject(this._project.ProjectId.Value);
					if (projSucc)
					{
						//Get counts..
						Logger.LogTrace(METHOD, "Getting project counts..");
						long numInc = this._spiraSoapClient.Incident_Count(null);
						long numReq = this._spiraSoapClient.Requirement_Count(null);
						long numRel = this._spiraSoapClient.Release_Count(null);
						long numTsk = this._spiraSoapClient.Task_Count(null);
						long numTC = this._spiraSoapClient.TestCase_Count(null, null);
						long numTS = this._spiraSoapClient.TestSet_Count(null, null);
						long numTR = this._spiraSoapClient.TestRun_Count(null);
						this.maxValue += ((float)Math.Ceiling((float)numInc / (float)Common.PAGE_NUM) + (numInc * 3)) +
							((float)Math.Ceiling((float)numReq / (float)Common.PAGE_NUM) + (numReq * 4)) +
							((float)Math.Ceiling((float)numRel / (float)Common.PAGE_NUM) + (numRel * 5)) +
							((float)Math.Ceiling((float)numTsk / (float)Common.PAGE_NUM) + (numTsk * 3)) +
							((float)Math.Ceiling((float)numTC / (float)Common.PAGE_NUM) + (numTC * 4)) +
							((float)Math.Ceiling((float)numTS / (float)Common.PAGE_NUM) + (numTS * 4)) +
							((float)Math.Ceiling((float)numTR / (float)Common.PAGE_NUM) + (numTR * 2));

						//Raise event..
						this.RaiseProgress(this.curValue / this.maxValue, "Downloading Automation & Custom Properties...");
						Logger.LogTrace(METHOD, "Downloading: Automation Engines, Automation Hosts, Custom Lists, Document Types, and Document Folders.");

						//Now fire off ASYNC hits..
						this._client1Act = 6;
						this._spiraSoapClient.AutomationEngine_RetrieveAsync(false, this._client1Num++);
						this._spiraSoapClient.AutomationHost_RetrieveAsync(null, new RemoteSort(), 1, Int32.MaxValue, this._client1Num++);
						this._spiraSoapClient.CustomProperty_RetrieveCustomListsAsync(this._client1Num++);
						this._spiraSoapClient.Document_RetrieveTypesAsync(false, this._client1Num++);
						this._spiraSoapClient.Document_RetrieveFoldersAsync(this._client1Num++);
						this._spiraSoapClient.Project_RetrieveByIdAsync(this._project.ProjectId.Value, this._client1Num++);
                        this._spiraSoapClient.Component_RetrieveAsync(false, false, this._client1Num++);
                        this._spiraSoapClient.TestCase_RetrieveFoldersAsync(this._client1Num++);
                        this._spiraSoapClient.TestSet_RetrieveFoldersAsync(this._client1Num++);
                        this._spiraSoapClient.Task_RetrieveFoldersAsync(this._client1Num++);
                    }
					else
					{
						Logger.LogError(METHOD, null, "Could not log onto Project.");
						this.RaiseError(null, "Could not log into server.");
						this._canceled = true;
						return;
					}
				}
				else
				{
					Exception ex = new Exception("Your version of SpiraTeam is too old. You need to be running v5.0.0.7 or later. You are running: " + ver.Version + " (patch " + ver.Patch + ")");
                    Logger.LogError(METHOD, ex, "Invalid version of SpiraTeam running. Needs v5.0.0.7. Running: " + ver.Version + " (patch " + ver.Patch + ")");
					this.RaiseError(ex, ex.Message);
					this._canceled = true;
					return;
				}
			}
			else
			{
				Logger.LogError(METHOD, null, "Could not log onto system.");
				this.RaiseError(null, "Could not log on to system.");
				this._canceled = true;
				return;
			}
		}

        /// <summary>
        /// Called when the list of folders is retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _client1_Task_RetrieveFoldersCompleted(object sender, Task_RetrieveFoldersCompletedEventArgs e)
        {
            const string METHOD = CLASS_NAME + "Document_RetrieveFoldersCompleted()";

            try
            {
                if (this._canceled) return;

                //Subtrack one & Log.
                this._client1Act--;
                this.RaiseProgress(++this.curValue / this.maxValue);

                //Make sure we're connected.
                if (e.Error == null)
                {
                    //Save results..
                    this._Export.TaskFolders = e.Result;

                    //Log it.
                    Logger.LogTrace(METHOD, "Finished downloading Task Folders. " + e.Result.Count() + " downloaded.");

                    //Check to see if we can go to next step..
                    this.CheckForStepTwo();
                }
                else
                {
                    Logger.LogError(METHOD, e.Error);
                    this.RaiseError(e.Error, "Could not download data from the server.");
                    this._canceled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex);
                this.RaiseError(ex, "Could not download data from the server.");
                this._canceled = true;
                return;
            }
        }

        /// <summary>
        /// Called when the list of folders is retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _client1_TestSet_RetrieveFoldersCompleted(object sender, TestSet_RetrieveFoldersCompletedEventArgs e)
        {
            const string METHOD = CLASS_NAME + "Document_RetrieveFoldersCompleted()";

            try
            {
                if (this._canceled) return;

                //Subtrack one & Log.
                this._client1Act--;
                this.RaiseProgress(++this.curValue / this.maxValue);

                //Make sure we're connected.
                if (e.Error == null)
                {
                    //Save results..
                    this._Export.TestSetFolders = e.Result;

                    //Log it.
                    Logger.LogTrace(METHOD, "Finished downloading Test Set Folders. " + e.Result.Count() + " downloaded.");

                    //Check to see if we can go to next step..
                    this.CheckForStepTwo();
                }
                else
                {
                    Logger.LogError(METHOD, e.Error);
                    this.RaiseError(e.Error, "Could not download data from the server.");
                    this._canceled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex);
                this.RaiseError(ex, "Could not download data from the server.");
                this._canceled = true;
                return;
            }
        }

        /// <summary>
        /// Called when the list of folders is retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _client1_TestCase_RetrieveFoldersCompleted(object sender, TestCase_RetrieveFoldersCompletedEventArgs e)
        {
            const string METHOD = CLASS_NAME + "Document_RetrieveFoldersCompleted()";

            try
            {
                if (this._canceled) return;

                //Subtrack one & Log.
                this._client1Act--;
                this.RaiseProgress(++this.curValue / this.maxValue);

                //Make sure we're connected.
                if (e.Error == null)
                {
                    //Save results..
                    this._Export.TestCaseFolders = e.Result;

                    //Log it.
                    Logger.LogTrace(METHOD, "Finished downloading Test Case Folders. " + e.Result.Count() + " downloaded.");

                    //Check to see if we can go to next step..
                    this.CheckForStepTwo();
                }
                else
                {
                    Logger.LogError(METHOD, e.Error);
                    this.RaiseError(e.Error, "Could not download data from the server.");
                    this._canceled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex);
                this.RaiseError(ex, "Could not download data from the server.");
                this._canceled = true;
                return;
            }
        }

        /// <summary>
        /// Called when the list of components is retrieved
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _client1_Component_RetrieveCompleted(object sender, Component_RetrieveCompletedEventArgs e)
        {
            const string METHOD = CLASS_NAME + "_client1_Component_RetrieveCompleted()";

            try
            {
                if (this._canceled) return;

                //Subtrack one & Log.
                this._client1Act--;
                this.RaiseProgress(++this.curValue / this.maxValue);

                //Make sure we're connected.
                if (e.Error == null)
                {
                    this._Export.Components = e.Result;

                    //Log it.
                    Logger.LogTrace(METHOD, "Finished downloading Components. " + e.Result.Count() + " downloaded.");

                    //Check to see if we can go to next step..
                    this.CheckForStepTwo();
                }
                else
                {
                    Logger.LogError(METHOD, e.Error);
                    this.RaiseError(e.Error, "Could not download data from the server.");
                    this._canceled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(METHOD, ex);
                this.RaiseError(ex, "Could not download data from the server.");
                this._canceled = true;
                return;
            }
        }

		#region Client Events
		#region Run #1
		/// <summary>Hit when the client is finished with getting AutomationEngines.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">AutomationEngine_RetrieveCompletedEventArgs</param>
		private void AutomationEngine_RetrieveCompleted(object sender, AutomationEngine_RetrieveCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "AutomationEngine_RetrieveCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Make sure we're connected.
				if (e.Error == null)
				{
					this._Export.AutomationEngines = e.Result;

					//Log it.
					Logger.LogTrace(METHOD, "Finished downloading Automation Engines. " + e.Result.Count() + " downloaded.");

					//Check to see if we can go to next step..
					this.CheckForStepTwo();
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}

		/// <summary>Hit when the client returns.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">AutomationHost_RetrieveCompletedEventArgs</param>
		private void AutomationHost_RetrieveCompleted(object sender, AutomationHost_RetrieveCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "AutomationHost_RetrieveCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Make sure we're connected.
				if (e.Error == null)
				{
					this._Export.AutomationHosts = e.Result;

					//Log it.
					Logger.LogTrace(METHOD, "Finished downloading Automation Hosts. " + e.Result.Count() + " downloaded.");

					//Check to see if we can go to next step..
					this.CheckForStepTwo();
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}

		/// <summary>Hit when the client returned.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">CustomProperty_RetrieveCustomListsCompletedEventArgs</param>
		private void CustomProperty_RetrieveCustomListsCompleted(object sender, CustomProperty_RetrieveCustomListsCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "CustomProperty_RetrieveCustomListsCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Raise event..

				//Make sure we're connected.
				if (e.Error == null)
				{
					//Log it.
					Logger.LogTrace(METHOD, "Finished downloading Custom Lists. " + e.Result.Count() + " downloaded.");

					//Check to see if we can go to next step..
					this.CheckForStepTwo();

					//Loop through each one and get the list & values.
					Logger.LogTrace(METHOD, "Downloading custom list values.");
					foreach (RemoteCustomList list in e.Result)
					{
						RemoteCustomList fullList = this._spiraSoapClient.CustomProperty_RetrieveCustomListById(list.CustomPropertyListId.Value);
						this._Export.CustomLists.Add(fullList);
					}
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}

		/// <summary>Hit when the client returned.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Document_RetrieveTypesCompletedEventArgs</param>
		private void Document_RetrieveTypesCompleted(object sender, Document_RetrieveTypesCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "Document_RetrieveTypesCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Make sure we're connected.
				if (e.Error == null)
				{
					this._Export.DocumentTypes = e.Result;

					//Log it.
					Logger.LogTrace(METHOD, "Finished downloading Document Types. " + e.Result.Count() + " downloaded.");

					//Check to see if we can go to next step..
					this.CheckForStepTwo();
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}

		/// <summary>Hit when the client returns.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Document_RetrieveFoldersCompletedEventArgs</param>
		private void Document_RetrieveFoldersCompleted(object sender, Document_RetrieveFoldersCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "Document_RetrieveFoldersCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Make sure we're connected.
				if (e.Error == null)
				{
					//Save results..
					this._Export.DocumentFolders = e.Result;

					//Log it.
					Logger.LogTrace(METHOD, "Finished downloading Document Folders. " + e.Result.Count() + " downloaded.");

					//Check to see if we can go to next step..
					this.CheckForStepTwo();

					//Now launch eachone in succession to get folder contents..
					//These can be run while step 2 is going, since they have no dependancies.
					Logger.LogTrace(METHOD, "Downloading Attachments in Folders.");

					foreach (RemoteDocumentFolder folder in e.Result)
					{
						//Now pull documents for this folder.
						List<RemoteDocument> docs = this._spiraSoapClient.Document_RetrieveForFolder(folder.ProjectAttachmentFolderId.Value, null, new RemoteSort(), 1, Common.PAGE_NUM);

						this._Export.Documents.AddRange(docs);
						int count = docs.Count;

						//Loop in case there're more than Common.PAGE_NUM/page.
						while (docs.Count == Common.PAGE_NUM)
						{
							docs = this._spiraSoapClient.Document_RetrieveForFolder(folder.ProjectAttachmentFolderId.Value, null, new RemoteSort(), count + 1, Common.PAGE_NUM);

							//Manually check to see if we should add these.
							if (docs.Count < Common.PAGE_NUM || !this._Export.Documents.Any(d => d.AttachmentId == docs[0].AttachmentId))
							{
								count += docs.Count;
								this._Export.Documents.AddRange(docs);
							}
							else //Reset count to get out of the loop.
								docs.Clear();
						}
					}
					Logger.LogTrace(METHOD, "Finished downloading Attachments. " + this._Export.Documents.Count + " downloaded.");

					//Add the documents to our max count because we're going to be downloading each one later..
					this.maxValue += this._Export.Documents.Count;
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}

		/// <summary>Hit when the call to get the project details is returned.</summary>
		/// <param name="sender">SoapServiceClient</param>
		/// <param name="e">Project_RetrieveByIdCompletedEventArgs</param>
		private void Project_RetrieveByIdCompleted(object sender, Project_RetrieveByIdCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "Project_RetrieveByIdCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Make sure we're connected.
				if (e.Error == null)
				{
					this._Export.Project = e.Result;

					//Log it.
					Logger.LogTrace(METHOD, "Finished downloading the Project Definition.");

					//Check to see if we can go to next step..
					this.CheckForStepTwo();
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}

		#endregion

		#region Run #2
		private void CustomProperty_RetrieveForArtifactTypeCompleted(object sender, CustomProperty_RetrieveForArtifactTypeCompletedEventArgs e)
		{
			const string METHOD = CLASS_NAME + "CustomProperty_RetrieveForArtifactTypeCompleted()";

			try
			{
				if (this._canceled) return;

				//Subtrack one & Log.
				this._client1Act--;
				Logger.LogTrace(METHOD, "Client Returned. # still running: " + this._client1Act.ToString());

				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Make sure we're connected.
				if (e.Error == null)
				{
					if (!e.Cancelled)
					{
						this._Export.CustomProperties.AddRange(e.Result);

						//Check to see if we can go to next step..
						this.CheckForStepThree();
					}
				}
				else
				{
					Logger.LogError(METHOD, e.Error);
					this.RaiseError(e.Error, "Could not download data from the server.");
					this._canceled = true;
					return;
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				this._canceled = true;
				return;
			}
		}
		#endregion
		#endregion

		#region Step Functions
		/// <summary>Checks to see if we can continue to step #2..</summary>
		private void CheckForStepTwo()
		{
			const string METHOD = CLASS_NAME + "CheckForStepTwo()";
			Logger.LogTrace(METHOD, "Checking if we can continue to step 2: # Clients Running: " + this._client1Act.ToString());

			if (this._client1Act == 0)
			{
				Logger.LogTrace(METHOD, "Starting Stage 2...");

				//Fire off 9 counts for custom properties for each artifact type.
				this._client1Act += 9;
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(1, false, this._client1Num++); //Requirement
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(2, false, this._client1Num++); //Test Case
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(3, false, this._client1Num++); //Incident
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(4, false, this._client1Num++); //Release
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(5, false, this._client1Num++); //Test Run
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(6, false, this._client1Num++); //Task
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(7, false, this._client1Num++); //Test Step
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(8, false, this._client1Num++); //Test Set
				this._spiraSoapClient.CustomProperty_RetrieveForArtifactTypeAsync(9, false, this._client1Num++); //Automation Host
			}
		}

		/// <summary>Checks to see if we can continue to step #3..</summary>
		private void CheckForStepThree()
		{
			const string METHOD = CLASS_NAME + "CheckForStepThree()";
			Logger.LogTrace(METHOD, "Checking if we can continue to step 3: # Clients Running: " + this._client1Act.ToString());

			if (this._client1Act == 0)
			{
				Logger.LogTrace(METHOD, "Starting Stage 3...");

				this.Step3();
			}
		}

		private void Step3()
		{
			const string METHOD = CLASS_NAME + "Step3()";

            #region Incidents:

            if (this._canceled) return;

			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Incidents...");
				Logger.LogTrace(METHOD, "Downloading Incidents.");

				//Need to get Incidnt Statuses
				this._Export.IncidentStatuses = this._spiraSoapClient.Incident_RetrieveStatuses();
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Need to get Incidnt Types
				this._Export.IncidentTypes = this._spiraSoapClient.Incident_RetrieveTypes();
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Need to get Incidnt Severities
				this._Export.IncidentSeverities = this._spiraSoapClient.Incident_RetrieveSeverities();
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Need to get Incidnt Priorities
				this._Export.IncidentPriorities = this._spiraSoapClient.Incident_RetrievePriorities();
				this.RaiseProgress(++this.curValue / this.maxValue);

				//Get all incidents.
				List<RemoteIncident> incidents = this._spiraSoapClient.Incident_Retrieve(null, new RemoteSort(), 1, Common.PAGE_NUM);
				this._Export.Incidents.AddRange(incidents);
				int count = incidents.Count;
				//Loop in case there're more than 50/page.
				while (incidents.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
					incidents = this._spiraSoapClient.Incident_Retrieve(null, new RemoteSort(), count + 1, Common.PAGE_NUM);

					//Manually check to see if we should add these.
					if (incidents.Count < Common.PAGE_NUM || !this._Export.Incidents.Any(i => i.IncidentId == incidents[0].IncidentId))
					{
						this._Export.Incidents.AddRange(incidents);
						count += incidents.Count;
					}
					else //Reset count to get out of the loop.
						incidents.Clear();

				}
				Logger.LogTrace(METHOD, "Finished downloading Incidents. " + this._Export.Incidents.Count + " downloaded. Downloading Incident data: Resolutions; Associations.");

				//Now for each incident, get it's resolutions and associations..
				for (int i = 0; i < this._Export.Incidents.Count; i++)
				{
					//Update counter and raise event for Resolutions..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.IncidentComments.AddRange(this._spiraSoapClient.Incident_RetrieveComments(this._Export.Incidents[i].IncidentId.Value));

					//Update counter and raise event for Associations..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(3, this._Export.Incidents[i].IncidentId.Value, null, new RemoteSort()));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(3, this._Export.Incidents[i].IncidentId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(3, this._Export.Incidents[i].IncidentId.Value, doc.AttachmentId.Value));
				}
				Logger.LogTrace(METHOD, "Finished downloading Incident data.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download incident data from the server.");
				return;
			}
			#endregion

			#region Requirements:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Requirements...");
				Logger.LogTrace(METHOD, "Downloading Requirements.");

				//Get all incidents.
				List<RemoteRequirement> requirements = this._spiraSoapClient.Requirement_Retrieve(null, 1, Common.PAGE_NUM);
				this._Export.Requirements.AddRange(requirements);
				int count = requirements.Count;
				//Loop in case there're more than 50/page.
				while (requirements.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
					requirements = this._spiraSoapClient.Requirement_Retrieve(null, count + 1, Common.PAGE_NUM);

					//Manually check to see if we should add these.
					if (requirements.Count < Common.PAGE_NUM || !this._Export.Requirements.Any(r => r.RequirementId == requirements[0].RequirementId))
					{
						this._Export.Requirements.AddRange(requirements);
						count += requirements.Count;
					}
					else //Reset count to get out of the loop.
						requirements.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Requirements. " + this._Export.Requirements.Count + " downloaded. Downloading Requirement data: Comments; Associations; Test Cases.");

				//Now for each requirment, get: Comments, Associations, Scenario Steps and Test Cases.
				foreach (RemoteRequirement requirement in this._Export.Requirements)
				{
					//Update counter and raise event for Resolutions..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.RequirementComments.AddRange(this._spiraSoapClient.Requirement_RetrieveComments(requirement.RequirementId.Value));

					//Update counter and raise event for Associations..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(1, requirement.RequirementId.Value, null, new RemoteSort()));

					//Update counter and raise event. for Test Cases..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.RequirementTestCases.AddRange(this._spiraSoapClient.Requirement_RetrieveTestCoverage(requirement.RequirementId.Value));

                    //Update counter and raise event for Use Case Steps
                    this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.RequirementSteps.AddRange(this._spiraSoapClient.Requirement_RetrieveSteps(requirement.RequirementId.Value));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(1, requirement.RequirementId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(1, requirement.RequirementId.Value, doc.AttachmentId.Value));
				}
				Logger.LogTrace(METHOD, "Finished downloading Requirement data.");

			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download requirement data from the server.");
				return;
			}
			#endregion

			#region Releases:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Releases...");
				Logger.LogTrace(METHOD, "Downloading Releases.");

				//Get all releases.
				List<RemoteRelease> releases = this._spiraSoapClient.Release_Retrieve2(null, 1, Common.PAGE_NUM);
				this._Export.Releases.AddRange(releases);
				int count = releases.Count;
				//Loop in case there're more than 50/page.
				while (releases.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
					releases = this._spiraSoapClient.Release_Retrieve2(null, count + 1, Common.PAGE_NUM);

					//Manually check to see if we should add these.
					if (releases.Count < Common.PAGE_NUM || !this._Export.Releases.Any(r => r.ReleaseId == releases[0].ReleaseId))
					{
						this._Export.Releases.AddRange(releases);
						count += releases.Count;
					}
					else //Reset count to get out of the loop.
						releases.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Releases. " + this._Export.Releases.Count + " downloaded. Downloading Release data: Comments; Associations; Test Cases; Builds.");

				//Now for each release, get: Comments, Associations, and Test Cases.
				for (int i = 0; i < this._Export.Releases.Count; i++)
				{
					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.ReleaseComments.AddRange(this._spiraSoapClient.Release_RetrieveComments(this._Export.Releases[i].ReleaseId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(4, this._Export.Releases[i].ReleaseId.Value, null, new RemoteSort()));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.ReleaseTestCases.AddRange(this._spiraSoapClient.Release_RetrieveTestMapping(this._Export.Releases[i].ReleaseId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					if (!this._Export.Releases[i].Summary)
					{
						List<RemoteBuild> builds = this._spiraSoapClient.Build_RetrieveByReleaseId(this._Export.Releases[i].ReleaseId.Value, null, null, 1, Common.PAGE_NUM);
						this._Export.ReleaseBuilds.AddRange(builds);
						int count2 = builds.Count;
						//Loop in case there're more than 50/page.
						while (builds.Count == Common.PAGE_NUM)
						{
							//Update counter & Raise event..
							this.RaiseProgress(++this.curValue / ++this.maxValue);

							//Get the next page of builds..
							builds = this._spiraSoapClient.Build_RetrieveByReleaseId(this._Export.Releases[i].ReleaseId.Value, null, null, count2 + 1, Common.PAGE_NUM);

							//Manually check to see if we should add these.
							if (builds.Count < Common.PAGE_NUM || !this._Export.ReleaseBuilds.Any(b => b.BuildId == builds[0].BuildId))
							{
								this._Export.ReleaseBuilds.AddRange(builds);
								count += builds.Count;
							}
							else //Reset count to get out of the loop.
								builds.Clear();
						}
					}

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(4, this._Export.Releases[i].ReleaseId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(4, this._Export.Releases[i].ReleaseId.Value, doc.AttachmentId.Value));

				}
				Logger.LogTrace(METHOD, "Finished downloading Release data.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download release data from the server.");
				return;
			}
			#endregion

			#region Tasks:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Tasks...");
				Logger.LogTrace(METHOD, "Downloading Tasks.");

				//Get all incidents.
				List<RemoteTask> tasks = this._spiraSoapClient.Task_Retrieve(null, new RemoteSort(), 1, Common.PAGE_NUM);
				this._Export.Tasks.AddRange(tasks);
				int count = tasks.Count;
				//Loop in case there're more than 50/page.
				while (tasks.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
					tasks = this._spiraSoapClient.Task_Retrieve(null, new RemoteSort(), count + 1, Common.PAGE_NUM);
					//Manually check to see if we should add these.
					if (tasks.Count < Common.PAGE_NUM || !this._Export.Tasks.Any(t => t.TaskId == tasks[0].TaskId))
					{
						this._Export.Tasks.AddRange(tasks);
						count += tasks.Count;
					}
					else //Reset count to get out of the loop.
						tasks.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Tasks. " + this._Export.Tasks.Count + " downloaded. Downloading Task data: Comments; Associations.");

				//Now for each requirment, get: Comments, Associations, and Test Cases.
				foreach (RemoteTask task in this._Export.Tasks)
				{
					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.TaskComments.AddRange(this._spiraSoapClient.Task_RetrieveComments(task.TaskId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(6, task.TaskId.Value, null, new RemoteSort()));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(6, task.TaskId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(6, task.TaskId.Value, doc.AttachmentId.Value));
				}
				Logger.LogTrace(METHOD, "Finished downloading Task data.");

			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download task data from the server.");
				return;
			}
			#endregion

			#region Test Cases:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Test Cases...");
				Logger.LogTrace(METHOD, "Downloading Test Cases.");

				//Get all test cases.
                RemoteSort sort = new RemoteSort();
                sort.PropertyName = "Name";
                sort.SortAscending = true;
				List<RemoteTestCase> testcases = this._spiraSoapClient.TestCase_Retrieve(null, sort, 1, Common.PAGE_NUM, null);
				this._Export.TestCases.AddRange(testcases);
				int count = testcases.Count;
				//Loop in case there're more than 50/page.
				while (testcases.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
                    testcases = this._spiraSoapClient.TestCase_Retrieve(null, sort, count + 1, Common.PAGE_NUM, null);

					//Manually check to see if we should add these.
					if (testcases.Count < Common.PAGE_NUM || !this._Export.TestCases.Any(t => t.TestCaseId == testcases[0].TestCaseId))
					{
						this._Export.TestCases.AddRange(testcases);
						count += testcases.Count;
					}
					else //Reset count to get out of the loop.
						testcases.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Test Cases. " + this._Export.TestCases.Count + " downloaded. Downloading Test Case data: Comments; Associations.");

				//Now for each requirment, get: Comments, Associations, and Test Cases.
				foreach (RemoteTestCase testcase in this._Export.TestCases)
				{
					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.TestCaseComments.AddRange(this._spiraSoapClient.TestCase_RetrieveComments(testcase.TestCaseId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(2, testcase.TestCaseId.Value, null, new RemoteSort()));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(2, testcase.TestCaseId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(2, testcase.TestCaseId.Value, doc.AttachmentId.Value));
				}

				//Get the steps for the test cases
				for (int i = 0; i < this._Export.TestCases.Count; i++)
				{
					this.RaiseProgress(++this.curValue / this.maxValue);
					RemoteTestCase newTC = null;
					try
					{
						newTC = this._spiraSoapClient.TestCase_RetrieveById(this._Export.TestCases[i].TestCaseId.Value);

						//Looop through and get documents for each test step..
						foreach (RemoteTestStep step in newTC.TestSteps)
						{
							List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(7, step.TestStepId.Value, null, new RemoteSort());
							foreach (RemoteDocument doc in incDocs)
								this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(7, step.TestStepId.Value, doc.AttachmentId.Value));
						}
					}
					catch (Exception ex)
					{
                        Logger.LogError(METHOD, ex);
                        throw;
                    }

					if (newTC != null)
						this._Export.TestCases[i] = newTC;
				}

				Logger.LogTrace(METHOD, "Finished downloading Test Case data.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download test case data from the server.");
				return;
			}
			#endregion

			#region Test Sets:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Test Sets...");
				Logger.LogTrace(METHOD, "Downloading Test Sets.");

				//Re-authenticate (to make sure)
				this._spiraSoapClient.Connection_Authenticate2(Common.USER_NAME, this._password, Common.APP_NAME);
				this._spiraSoapClient.Connection_ConnectToProject(this._project.ProjectId.Value);

				//Get all the test sets
                RemoteSort sort = new RemoteSort();
                sort.PropertyName = "Name";
                sort.SortAscending = true;
				List<RemoteTestSet> testsets = this._spiraSoapClient.TestSet_Retrieve(null, sort, 1, Common.PAGE_NUM, null);
				this._Export.TestSets.AddRange(testsets);
				int count = testsets.Count;
				//Loop in case there're more than 50/page.
				while (testsets.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of test sets..
                    testsets = this._spiraSoapClient.TestSet_Retrieve(null, sort, count + 1, Common.PAGE_NUM, null);

					//Manually check to see if we should add these.
					if (testsets.Count < Common.PAGE_NUM || !this._Export.TestSets.Any(t => t.TestSetId == testsets[0].TestSetId))
					{
						this._Export.TestSets.AddRange(testsets);
						count += testsets.Count;
					}
					else //Reset count to get out of the loop.
						testsets.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Test Sets. " + this._Export.TestSets.Count + " downloaded. Downloading Test Set data: Comments; Associations; Test Cases.");

				//Now for each test set, get: parameters Comments, Associations, and Test Cases.
				foreach (RemoteTestSet testset in this._Export.TestSets)
				{
                    //Update counter and raise event..
                    this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.TestSetParameters.AddRange(this._spiraSoapClient.TestSet_RetrieveParameters(testset.TestSetId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.TestCaseComments.AddRange(this._spiraSoapClient.TestCase_RetrieveComments(testset.TestSetId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(8, testset.TestSetId.Value, null, new RemoteSort()));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(8, testset.TestSetId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(8, testset.TestSetId.Value, doc.AttachmentId.Value));

					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					int pos = 0;
					foreach (RemoteTestSetTestCaseMapping tstcMap in this._spiraSoapClient.TestSet_RetrieveTestCaseMapping(testset.TestSetId.Value))
					{
						pos++;
						tstcMap.Position = pos;
						this._Export.TestSetTestCases.Add(tstcMap);
					}
				}
				Logger.LogTrace(METHOD, "Finished downloading Test Set data.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download test set data from the server.");
				return;
			}
			#endregion

			#region Test Runs:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Test Runs...");
				Logger.LogTrace(METHOD, "Downloading Test Runs.");

				//Re-authenticate (to make sure)
				this._spiraSoapClient.Connection_Authenticate2(Common.USER_NAME, this._password, Common.APP_NAME);
				this._spiraSoapClient.Connection_ConnectToProject(this._project.ProjectId.Value);

				//Get Automated Test Runs..
				List<RemoteAutomatedTestRun> testAruns = this._spiraSoapClient.TestRun_RetrieveAutomated(null, new RemoteSort(), 1, Common.PAGE_NUM);
				this._Export.TestRuns_Automated.AddRange(testAruns);
				int countA = testAruns.Count;
				//Loop in case there're more than 50/page.
				while (testAruns.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
					testAruns = this._spiraSoapClient.TestRun_RetrieveAutomated(null, new RemoteSort(), countA + 1, Common.PAGE_NUM);

					//Manually check to see if we should add these.
					if (testAruns.Count < Common.PAGE_NUM || !this._Export.TestRuns_Automated.Any(t => t.TestRunId == testAruns[0].TestRunId))
					{
						this._Export.TestRuns_Automated.AddRange(testAruns);
						countA += testAruns.Count;
					}
					else //Reset count to get out of the loop.
						testAruns.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Automated Test Runs. " + countA + " downloaded. Downloading Manual Test Runs.");

				//Get Manual Test Runs..
				List<RemoteManualTestRun> testMruns = this._spiraSoapClient.TestRun_RetrieveManual(null, new RemoteSort(), 1, Common.PAGE_NUM);
				this._Export.TestRuns_Manual.AddRange(testMruns);
				int countM = testMruns.Count;
				//Loop in case there're more than 50/page.
				while (testMruns.Count == Common.PAGE_NUM)
				{
					//Update counter & Raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);

					//Get the next page of incidents..
					testMruns = this._spiraSoapClient.TestRun_RetrieveManual(null, new RemoteSort(), countM + 1, Common.PAGE_NUM);
					//Manually check to see if we should add these.
					if (testMruns.Count < Common.PAGE_NUM || !this._Export.TestRuns_Manual.Any(t => t.TestRunId == testMruns[0].TestRunId))
					{
						this._Export.TestRuns_Manual.AddRange(testMruns);
						countM += testMruns.Count;
					}
					else //Reset count to get out of the loop.
						testMruns.Clear();
				}
				Logger.LogTrace(METHOD, "Finished downloading Manual Test Runs. " + countM + " downloaded. Downloading Test Run data: Associations.");

				//Now for each test runs, get: Associations
				foreach (RemoteTestRun testrun in this._Export.TestRuns_Manual)
				{
					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(5, testrun.TestRunId.Value, null, new RemoteSort()));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(5, testrun.TestRunId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(5, testrun.TestRunId.Value, doc.AttachmentId.Value));
				}
				foreach (RemoteTestRun testrun in this._Export.TestRuns_Automated)
				{
					//Update counter and raise event..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.Associations.AddRange(this._spiraSoapClient.Association_RetrieveForArtifact(5, testrun.TestRunId.Value, null, new RemoteSort()));

					//Update counter and raise event for Documents..
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDocument> incDocs = this._spiraSoapClient.Document_RetrieveForArtifact(5, testrun.TestRunId.Value, null, new RemoteSort());
					foreach (RemoteDocument doc in incDocs)
						this._Export.DocumentMappings.Add(new ExportFile.DocumentMapping(5, testrun.TestRunId.Value, doc.AttachmentId.Value));
				}
				Logger.LogTrace(METHOD, "Finished downloading Test Run data.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download test run data from the server.");
				return;
			}
			#endregion

			#region Data Mappings:
			try
			{
				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading DataSync Mappings...");

                //Re-authenticate (to make sure)
                this._spiraSoapClient.Connection_Authenticate2(Common.USER_NAME, this._password, Common.APP_NAME);
                this._spiraSoapClient.Connection_ConnectToProject(this._project.ProjectId.Value);

				//Raise event..
				this.RaiseProgress(++this.curValue / this.maxValue);
                List<RemoteDataSyncSystem> dataSyncs = _spiraSoapClient.DataSyncSystem_Retrieve();

				this.maxValue += (dataSyncs.Count * (15 + (this._Export.CustomProperties.Count * 2)));
				//Now for each datasync, we need to get mappings.
				foreach (RemoteDataSyncSystem dataSync in dataSyncs)
				{
					Logger.LogTrace(METHOD, "Downloading DataSync Mappings for DataSync #" + dataSync.DataSyncSystemId + ".");

					//Get artifact mappings..
					this.RaiseProgress(++this.curValue / this.maxValue);
					this._Export.RequirementDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 1) }); //Requirement
					this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.TestCaseDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 2) }); //Test Case
					this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.IncidentDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 3) }); //Incident
					this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.ReleaseDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 4) }); //Release
					this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.TestRunDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 5) }); //Test Run
					this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.TaskDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 6) }); //Task
					this.RaiseProgress(++this.curValue / this.maxValue);
                    this._Export.TestSetDataMappings.Add(new ExportFile.DataSyncDataMapping() { DataSyncSystemName = dataSync.Name, DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveArtifactMappings(dataSync.DataSyncSystemId, 8) }); //Test Set

					//Get custom property mappings.
					foreach (RemoteCustomProperty custProp in this._Export.CustomProperties)
					{
						ExportFile.DataSyncFieldDataMapping custMapping = new ExportFile.DataSyncFieldDataMapping();
						ExportFile.DataSyncFieldDataMapping custValueMapping = new ExportFile.DataSyncFieldDataMapping();
						custMapping.DataSyncSystemId = dataSync.DataSyncSystemId;
						custMapping.ItemId1 = custProp.CustomPropertyId.Value;
						custMapping.ItemId2 = custProp.ArtifactTypeId;

						custValueMapping.DataSyncSystemId = dataSync.DataSyncSystemId;
						custValueMapping.ItemId1 = custProp.CustomPropertyId.Value;
						custValueMapping.ItemId2 = custProp.ArtifactTypeId;

						this.RaiseProgress(++this.curValue / this.maxValue);
						custMapping.DataSyncMappings.Add(this._spiraSoapClient.DataMapping_RetrieveCustomPropertyMapping(dataSync.DataSyncSystemId, custProp.ArtifactTypeId, custProp.CustomPropertyId.Value));

						this.RaiseProgress(++this.curValue / this.maxValue);
						custValueMapping.DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveCustomPropertyValueMappings(dataSync.DataSyncSystemId, custProp.ArtifactTypeId, custProp.CustomPropertyId.Value);

						//Add it to the export..
						this._Export.CustomPropertyDataMappings.Add(custMapping);
						this._Export.CustomPropertyValueDataMappings.Add(custValueMapping);
					}

					//Get standard field mappings..
					foreach (int fieldId in Thread_Export.FieldDataMapping) // 6 fields.
					{
						this.RaiseProgress(++this.curValue / this.maxValue);
						this._Export.ArtifactFieldDataMappings.Add(new ExportFile.DataSyncFieldDataMapping()
						{
							DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveFieldValueMappings(dataSync.DataSyncSystemId, fieldId),
							DataSyncSystemId = dataSync.DataSyncSystemId,
							ItemId1 = fieldId
						});
					}

					//Get the main project mappings for this datasync.
					this.RaiseProgress(++this.curValue / this.maxValue);
					List<RemoteDataMapping> projMappings = this._spiraSoapClient.DataMapping_RetrieveProjectMappings(dataSync.DataSyncSystemId);
                    RemoteDataMapping projectMapping = projMappings.FirstOrDefault(pm => pm.ProjectId == this._project.ProjectId.Value);
                    if (projectMapping != null)
                    {
                        ExportFile.DataSyncDataMapping projectDataMapping = new ExportFile.DataSyncDataMapping();
                        projectDataMapping.DataSyncSystemName = dataSync.Name;
                        projectDataMapping.DataSyncMappings = new List<RemoteDataMapping>() { projectMapping };
                        this._Export.ProjectDataMappings.Add(projectDataMapping);
                    }

					//Get user mappings..
					this.RaiseProgress(++this.curValue / this.maxValue);
					ExportFile.DataSyncDataMapping userDataMapping = new ExportFile.DataSyncDataMapping();
                    userDataMapping.DataSyncSystemName = dataSync.Name;
					userDataMapping.DataSyncMappings = this._spiraSoapClient.DataMapping_RetrieveUserMappings(dataSync.DataSyncSystemId);
					this._Export.UserDataMappings.Add(userDataMapping);
				}
				Logger.LogTrace(METHOD, "Finished downloading DataSync Mappings.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download data from the server.");
				return;
			}
			#endregion

			#region Project Users:
			try
			{
				Logger.LogTrace(METHOD, "Downloading Users & Roles.");

				//Get users in the project..
				this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Project Users...");
				this._Export.Users = this._spiraSoapClient.Project_RetrieveUserMembership();

				//Get defined project roles.
				this.RaiseProgress(++this.curValue / this.maxValue);
				this._Export.ProjectRoles = this._spiraSoapClient.ProjectRole_Retrieve();

				Logger.LogTrace(METHOD, "Finished downlading Users & Roles. " + this._Export.Users.Count + " users downloaded, " + this._Export.ProjectRoles.Count + " roles downloaded.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
			}
			#endregion

			#region Download Attachments:
			try
			{
				Logger.LogTrace(METHOD, "Downloading attachment files.");

				//Create the attachments directory, if needed..
				string tempDir = this._tempDirectory + "Attachments\\";
				if (!Directory.Exists(tempDir))
					Directory.CreateDirectory(tempDir);

				//Donload each document..
				int counter = 0;
				List<int> docsToRemove = new List<int>();
				foreach (RemoteDocument doc in this._Export.Documents)
				{
					Logger.LogTrace(METHOD, "Downloading attachment #" + doc.AttachmentId.ToString());
					string filename = tempDir + doc.AttachmentId.ToString() + ".dat";
					this.RaiseProgress(++this.curValue / this.maxValue, "Downloading Attachment Files: " + (++counter).ToString() + "/" + this._Export.Documents.Count.ToString());
					byte[] fileContents = null;
					try
					{
						fileContents = this._spiraSoapClient.Document_OpenFile(doc.AttachmentId.Value);
					}
					catch (Exception ex)
					{
						//If we got here, there's a problem with the attachment file. Remove it from our record.
						docsToRemove.Add(doc.AttachmentId.Value);
						Logger.LogError(METHOD, ex, "While downloading file #" + doc.AttachmentId.ToString());
					}

					if (fileContents != null)
					{
						try
						{
							using (BinaryWriter binWriter = new BinaryWriter(File.Open(filename, FileMode.Create)))
							{
								binWriter.Write(fileContents);
							}
						}
						catch (Exception ex)
						{
							Logger.LogError(METHOD, ex, "Saving data to " + filename + ":");
						}
					}
				}
				//Now remove the items that threw an error..
				foreach (int numToRemove in docsToRemove)
				{
					this._Export.Documents.Remove(this._Export.Documents.Where(rd => rd.AttachmentId == numToRemove).Single());
				}

				Logger.LogTrace(METHOD, "Finished downloading attachment files.");
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not download attachment file(s) from the server.");
				return;
			}
			#endregion

			#region Export XML File & Log
			try
			{
				Logger.LogTrace(METHOD, "Extracting XML and saving Log..");
				if (!string.IsNullOrWhiteSpace(this._file))
					this.RaiseProgress(++this.curValue / this.maxValue, "Saving to output file...");
				else
					this.RaiseProgress(++this.curValue / this.maxValue, "Starting import...");

				//Save the other metadata information..
				this._Export.ExportInfo.DateExported = DateTime.UtcNow;
				this._Export.ExportInfo.OriginalFilename = Path.GetFileName(this._file);
				this._Export.ExportInfo.OriginalServerUri = this._server.ToString().Replace(Common.SERVICE_SPIRA_5, "");

				XmlSerializer ser = new XmlSerializer(this._Export.GetType());
				using (FileStream fs = File.Open(this._tempDirectory + Common.PROJECT_FILE, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					CleanXMLWriter writer = new CleanXMLWriter(fs);
					ser.Serialize(writer, this._Export);
				}

				Logger.SaveLogToFile(this._tempDirectory + Common.LOG_OUTPUT);
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, new Exception("Could Not Serialize object to " + this._tempDirectory + Common.PROJECT_FILE, ex));
				this.RaiseError(ex, "Could not save project export to a local file.");
				return;
			}
			#endregion

			#region Zip up Files
			try
			{
				if (!string.IsNullOrWhiteSpace(this._file))
				{
					using (ZipFile zip = new ZipFile())
					{
						zip.UseZip64WhenSaving = Zip64Option.Always;
						zip.ZipErrorAction = ZipErrorAction.Throw;
						zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
						zip.AddDirectory(this._tempDirectory, "\\");
						zip.Comment = "";
						zip.Save(this._file);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
				this.RaiseError(ex, "Could not create project file.");
				return;
			}
			#endregion

			#region Delete Temporary Directory
			try
			{
				if (!string.IsNullOrWhiteSpace(this._file))
				{
					Directory.Delete(this._tempDirectory, true);
				}
			}
			catch (Exception ex)
			{
				Logger.LogError(METHOD, ex);
			}
			#endregion

			//Finished!
			if (this.ProgressFinished != null)
			{
				Inflectra.SpiraTest.Utilities.ProjectMigration.Thread_Events.FinishedArgs retClass = new FinishedArgs(null);
				retClass.Message = this._tempDirectory;
				this.ProgressFinished(this, retClass);
			}
		}

		#endregion

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

		/// <summary>A list of the field ID's to get mappings for.</summary>
		private static List<int> FieldDataMapping
		{
			get
			{
				return new List<int>() {
				1,		// Incident: Severity
				2,		// Incident: Priority
				3,		// Incident: Status
				4,		// Incident: Type
				//16,		// Requirement: Status
				//18,		// Requirement: Importance
				//24,		// Test Case: Priorty
				//35,		// Test Run: Type
				//37,		// Test Run: Execution Status
				//44,		// Test Case: Execution Status
				//52,		// Test Set: Status
				57,		// Task: Status
				59		// Task: Priority
				//103,	// Test Step: Status
				//118,	// Test Set: Status
				//135		// Test Set: Recurrance
				};
			}
		}
	}
}
