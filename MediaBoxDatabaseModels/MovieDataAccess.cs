using DataManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MediaBoxDatabaseModels;

public class MovieDataAccess(string connectionString) : DataAccess<Movie>(connectionString, Movie.Metadata)
{
}
