using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class ProjectStageDataAccess(string connectionString) : DataAccess<ProjectStage>(connectionString, ProjectStage.Metadata)
{
}