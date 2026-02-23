using DataManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace NexusDatabaseModels;

public class MilestoneTemplateDataAccess(string connectionString) : DataAccess<MilestoneTemplate>(connectionString, MilestoneTemplate.Metadata)
{

}
