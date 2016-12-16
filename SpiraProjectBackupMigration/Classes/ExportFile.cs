using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inflectra.SpiraTest.Utilities.ProjectMigration.SpiraSoapService;
using System.Reflection;

namespace Inflectra.SpiraTest.Utilities.ProjectMigration
{
	[Serializable()]
	public class ExportFile
	{
		public const string VERSION = "4.0.0.0";

		/// <summary>Creates an instance of the class.</summary>
		public ExportFile()
		{
			this.ArtifactFieldDataMappings = new List<DataSyncFieldDataMapping>();
			this.AutomationEngines = new List<RemoteAutomationEngine>();
			this.AutomationHosts = new List<RemoteAutomationHost>();
			this.CustomLists = new List<RemoteCustomList>();
			this.CustomProperties = new List<RemoteCustomProperty>();
			this.CustomPropertyDataMappings = new List<DataSyncFieldDataMapping>();
			this.CustomPropertyValueDataMappings = new List<DataSyncFieldDataMapping>();
			this.DocumentFolders = new List<RemoteDocumentFolder>();
			this.DocumentTypes = new List<RemoteDocumentType>();
			this.Documents = new List<RemoteDocument>();
			this.DocumentMappings = new List<DocumentMapping>();
			this.Associations = new List<RemoteAssociation>();
			this.Incidents = new List<RemoteIncident>();
			this.IncidentPriorities = new List<RemoteIncidentPriority>();
			this.IncidentSeverities = new List<RemoteIncidentSeverity>();
			this.IncidentStatuses = new List<RemoteIncidentStatus>();
			this.IncidentTypes = new List<RemoteIncidentType>();
			this.IncidentResolutions2 = new List<RemoteIncidentResolution>();
			this.IncidentComments = new List<RemoteComment>();
			this.IncidentDataMappings = new List<DataSyncDataMapping>();
			this.Requirements = new List<RemoteRequirement>();
			this.RequirementComments = new List<RemoteComment>();
            this.RequirementSteps = new List<RemoteRequirementStep>();
			this.RequirementTestCases = new List<RemoteRequirementTestCaseMapping>();
			this.RequirementDataMappings = new List<DataSyncDataMapping>();
			this.Releases = new List<RemoteRelease>();
			this.ReleaseComments = new List<RemoteComment>();
			this.ReleaseTestCases = new List<RemoteReleaseTestCaseMapping>();
			this.ReleaseBuilds = new List<RemoteBuild>();
			this.ReleaseDataMappings = new List<DataSyncDataMapping>();
			this.Tasks = new List<RemoteTask>();
			this.TaskComments = new List<RemoteComment>();
			this.TaskDataMappings = new List<DataSyncDataMapping>();
			this.TestCases = new List<RemoteTestCase>();
			this.TestCaseComments = new List<RemoteComment>();
			this.TestCaseDataMappings = new List<DataSyncDataMapping>();
			this.TestSets = new List<RemoteTestSet>();
			this.TestSetComments = new List<RemoteComment>();
			this.TestSetTestCases = new List<RemoteTestSetTestCaseMapping>();
			this.TestSetDataMappings = new List<DataSyncDataMapping>();
			this.TestRuns_Automated = new List<RemoteAutomatedTestRun>();
			this.TestRuns_Manual = new List<RemoteManualTestRun>();
			this.TestRunDataMappings = new List<DataSyncDataMapping>();
            this.ProjectDataMappings = new List<DataSyncDataMapping>();
			this.ProjectRoles = new List<RemoteProjectRole>();
			this.Users = new List<RemoteProjectUser>();
			this.UserDataMappings = new List<DataSyncDataMapping>();
			this.Project = null;
            this.Components = new List<RemoteComponent>();
            this.TaskFolders = new List<RemoteTaskFolder>();
            this.TestCaseFolders = new List<RemoteTestCaseFolder>();
            this.TestSetFolders = new List<RemoteTestSetFolder>();
            this.TestSetParameters = new List<RemoteTestSetParameter>();

			//Metadata Information
			this.ExportInfo = new MetadataInformation();
			this.ExportInfo.FileVersion = new Version(ExportFile.VERSION).ToString();
			this.ExportInfo.AppVersion = Assembly.GetEntryAssembly().GetName().Version.ToString();
		}

		public List<DataSyncFieldDataMapping> ArtifactFieldDataMappings
		{ get; set; }

		public List<RemoteAutomationEngine> AutomationEngines
		{ get; set; }

		public List<RemoteAutomationHost> AutomationHosts
		{ get; set; }

        public List<RemoteComponent> Components
        { get; set; }

		public List<RemoteCustomList> CustomLists
		{ get; set; }

		public List<RemoteCustomProperty> CustomProperties
		{ get; set; }

		public List<DataSyncFieldDataMapping> CustomPropertyDataMappings
		{ get; set; }

		public List<DataSyncFieldDataMapping> CustomPropertyValueDataMappings
		{ get; set; }

		public List<RemoteDocumentFolder> DocumentFolders
		{ get; set; }

        public List<RemoteTaskFolder> TaskFolders
        { get; set; }

        public List<RemoteTestCaseFolder> TestCaseFolders
        { get; set; }

        public List<RemoteTestSetFolder> TestSetFolders
        { get; set; }

        public List<RemoteTestSetParameter> TestSetParameters
        { get; set; }

		public List<RemoteDocumentType> DocumentTypes
		{ get; set; }

		public List<RemoteDocument> Documents
		{ get; set; }

		public List<DocumentMapping> DocumentMappings
		{ get; set; }

		public List<RemoteAssociation> Associations
		{ get; set; }

		public List<RemoteIncident> Incidents
		{ get; set; }

		public List<RemoteIncidentPriority> IncidentPriorities
		{ get; set; }

		public List<RemoteIncidentType> IncidentTypes
		{ get; set; }

		public List<RemoteIncidentStatus> IncidentStatuses
		{ get; set; }

		public List<RemoteIncidentSeverity> IncidentSeverities
		{ get; set; }

		public List<RemoteIncidentResolution> IncidentResolutions2
		{ get; set; }

		public List<RemoteComment> IncidentComments
		{ get; set; }

		public List<DataSyncDataMapping> IncidentDataMappings
		{ get; set; }

		public List<RemoteRequirement> Requirements
		{ get; set; }

		public List<RemoteComment> RequirementComments
		{ get; set; }

        public List<RemoteRequirementStep> RequirementSteps
        { get; set; }

		public List<RemoteRequirementTestCaseMapping> RequirementTestCases
		{ get; set; }

		public List<DataSyncDataMapping> RequirementDataMappings
		{ get; set; }

		public List<RemoteRelease> Releases
		{ get; set; }

		public List<RemoteComment> ReleaseComments
		{ get; set; }

		public List<RemoteReleaseTestCaseMapping> ReleaseTestCases
		{ get; set; }

		public List<RemoteBuild> ReleaseBuilds
		{ get; set; }

		public List<DataSyncDataMapping> ReleaseDataMappings
		{ get; set; }

		public List<RemoteTask> Tasks
		{ get; set; }

		public List<RemoteComment> TaskComments
		{ get; set; }

		public List<DataSyncDataMapping> TaskDataMappings
		{ get; set; }

		public List<RemoteTestCase> TestCases
		{ get; set; }

		public List<RemoteComment> TestCaseComments
		{ get; set; }

		public List<DataSyncDataMapping> TestCaseDataMappings
		{ get; set; }

		public List<RemoteTestSet> TestSets
		{ get; set; }

		public List<RemoteComment> TestSetComments
		{ get; set; }

		public List<RemoteTestSetTestCaseMapping> TestSetTestCases
		{ get; set; }

		public List<DataSyncDataMapping> TestSetDataMappings
		{ get; set; }

		public List<RemoteAutomatedTestRun> TestRuns_Automated
		{ get; set; }

		public List<RemoteManualTestRun> TestRuns_Manual
		{ get; set; }

		public List<DataSyncDataMapping> TestRunDataMappings
		{ get; set; }

        public List<DataSyncDataMapping> ProjectDataMappings
		{ get; set; }

		public List<RemoteProjectRole> ProjectRoles
		{ get; set; }

		public List<RemoteProjectUser> Users
		{ get; set; }

		public List<DataSyncDataMapping> UserDataMappings
		{ get; set; }

		public RemoteProject Project
		{ get; set; }

		public MetadataInformation ExportInfo
		{ get; set; }

		/// <summary>Class to hold the data sync definition and the resultant data mappings.</summary>
		public class DataSyncDataMapping
		{
			public DataSyncDataMapping()
			{
				this.DataSyncSystemName = "";
				this.DataSyncMappings = new List<RemoteDataMapping>();
			}

			public string DataSyncSystemName
			{ get; set; }

			public List<RemoteDataMapping> DataSyncMappings
			{ get; set; }
		}

		/// <summary>Class to hold the data sync definition and the resultant data mappings.</summary>
		public class DataSyncFieldDataMapping
		{
			public DataSyncFieldDataMapping()
			{
				this.DataSyncSystemId = -1;
				this.ItemId1 = -1;
				this.ItemId2 = -1;
				this.DataSyncMappings = new List<RemoteDataMapping>();
			}

			public int DataSyncSystemId
			{ get; set; }

			public int ItemId1
			{ get; set; }

			public int ItemId2
			{ get; set; }

			public List<RemoteDataMapping> DataSyncMappings
			{ get; set; }
		}

		/// <summary>Class to hold information about the export itself.</summary>
		public class MetadataInformation
		{
			public string AppVersion
			{ get; set; }

			public RemoteVersion SpiraVersion
			{ get; set; }

			public DateTime? DateExported
			{ get; set; }

			public string FileVersion
			{ get; set; }

			public string OriginalFilename
			{ get; set; }

			public string OriginalServerUri
			{ get; set; }
		}

		/// <summary>Class to hold mappings for Attachments and their artifacts.</summary>
		public class DocumentMapping
		{
			public DocumentMapping()
			{ }

			public DocumentMapping(int artifactTypeId, int artifactId, int attachmentId)
			{
				this.ArtifactTypeId = artifactTypeId;
				this.ArtifactId = artifactId;
				this.AttachmentId = attachmentId;
			}

			public int ArtifactTypeId
			{ get; set; }

			public int ArtifactId
			{ get; set; }

			public int AttachmentId
			{ get; set; }
		}

		#region Old Outdated Classes
		[System.Runtime.Serialization.DataContractAttribute(Name = "RemoteIncidentResolution", Namespace = "http://schemas.datacontract.org/2004/07/Inflectra.SpiraTest.Web.Services.v3_0.DataObjects")]
		[System.SerializableAttribute()]
		public partial class RemoteIncidentResolution : object, System.Runtime.Serialization.IExtensibleDataObject, System.ComponentModel.INotifyPropertyChanged
		{

			[System.NonSerializedAttribute()]
			private System.Runtime.Serialization.ExtensionDataObject extensionDataField;

			[System.Runtime.Serialization.OptionalFieldAttribute()]
			private System.DateTime CreationDateField;

			[System.Runtime.Serialization.OptionalFieldAttribute()]
			private System.Nullable<int> CreatorIdField;

			[System.Runtime.Serialization.OptionalFieldAttribute()]
			private string CreatorNameField;

			[System.Runtime.Serialization.OptionalFieldAttribute()]
			private int IncidentIdField;

			[System.Runtime.Serialization.OptionalFieldAttribute()]
			private System.Nullable<int> IncidentResolutionIdField;

			[System.Runtime.Serialization.OptionalFieldAttribute()]
			private string ResolutionField;

			[global::System.ComponentModel.BrowsableAttribute(false)]
			public System.Runtime.Serialization.ExtensionDataObject ExtensionData
			{
				get
				{
					return this.extensionDataField;
				}
				set
				{
					this.extensionDataField = value;
				}
			}

			[System.Runtime.Serialization.DataMemberAttribute()]
			public System.DateTime CreationDate
			{
				get
				{
					return this.CreationDateField;
				}
				set
				{
					if ((this.CreationDateField.Equals(value) != true))
					{
						this.CreationDateField = value;
						this.RaisePropertyChanged("CreationDate");
					}
				}
			}

			[System.Runtime.Serialization.DataMemberAttribute()]
			public System.Nullable<int> CreatorId
			{
				get
				{
					return this.CreatorIdField;
				}
				set
				{
					if ((this.CreatorIdField.Equals(value) != true))
					{
						this.CreatorIdField = value;
						this.RaisePropertyChanged("CreatorId");
					}
				}
			}

			[System.Runtime.Serialization.DataMemberAttribute()]
			public string CreatorName
			{
				get
				{
					return this.CreatorNameField;
				}
				set
				{
					if ((object.ReferenceEquals(this.CreatorNameField, value) != true))
					{
						this.CreatorNameField = value;
						this.RaisePropertyChanged("CreatorName");
					}
				}
			}

			[System.Runtime.Serialization.DataMemberAttribute()]
			public int IncidentId
			{
				get
				{
					return this.IncidentIdField;
				}
				set
				{
					if ((this.IncidentIdField.Equals(value) != true))
					{
						this.IncidentIdField = value;
						this.RaisePropertyChanged("IncidentId");
					}
				}
			}

			[System.Runtime.Serialization.DataMemberAttribute()]
			public System.Nullable<int> IncidentResolutionId
			{
				get
				{
					return this.IncidentResolutionIdField;
				}
				set
				{
					if ((this.IncidentResolutionIdField.Equals(value) != true))
					{
						this.IncidentResolutionIdField = value;
						this.RaisePropertyChanged("IncidentResolutionId");
					}
				}
			}

			[System.Runtime.Serialization.DataMemberAttribute()]
			public string Resolution
			{
				get
				{
					return this.ResolutionField;
				}
				set
				{
					if ((object.ReferenceEquals(this.ResolutionField, value) != true))
					{
						this.ResolutionField = value;
						this.RaisePropertyChanged("Resolution");
					}
				}
			}

			public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			protected void RaisePropertyChanged(string propertyName)
			{
				System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
				if ((propertyChanged != null))
				{
					propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
				}
			}
		}
		#endregion
	}
}
