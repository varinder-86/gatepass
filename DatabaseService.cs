
    using System;
using System.Collections.Generic;
using System.Data;
    using System.Data.Odbc;
    //using Microsoft.Data.Odbc;
    using Microsoft.Extensions.Configuration;

namespace Gatepaswebapi
{
           public class DatabaseService 
        {
             private readonly IConfiguration _configuration;
        

             public DatabaseService(IConfiguration configuration)
             {
                     _configuration = configuration;
             }

                public OdbcConnection CreateDefaultConnection()
                {
                 var connectionString = _configuration.GetConnectionString("DefaultConnection");
                 return new OdbcConnection(connectionString);
              }

       
    }




}
