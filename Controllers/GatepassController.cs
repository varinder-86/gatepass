using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Odbc;
using Microsoft.Extensions.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gatepaswebapi.Controllers
{
    [ApiController]
   // [Route("[controller]")]
    public class GatepassController : ControllerBase
    {

      
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

       
             public GatepassController( IConfiguration configuration)
        {
          
            _configuration = configuration; // Inject configuration
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new Exception("Connection string is not configured properly");
            }
        }

               
        // [HttpGet("{tranno}")]
        [HttpGet]
        [Route("api/gatepass")]
        public IActionResult GetData(string tranno)
        {
            try
            {
                // string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new OdbcConnection(_connectionString))
                {
                    connection.Open();
                    var command = new OdbcCommand("select * from GATEPASLIB.Gptran where TRANno='" + tranno + "'", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<object>();
                        while (reader.Read())
                        {
                            // Process each row(example: adding to a list)
                            results.Add(new
                            {
                                Column1 = reader["gatepasno"].ToString(),
                                 Column2 = reader["Status"].ToString(),
                                //Add more columns as needed

                            });
                            if (reader["Status"].ToString() == "G")
                            {
                                Console.WriteLine($"gatepas is generated: {reader["gatepasno"].ToString()}");
                            }
                        }
                      
                        return Ok(new { Message = "gatepas is generated: ",  results });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        // [HttpGet]
        //public IActionResult GetData()
        //{
        //    try
        //    {


        //        using (var connection = new OdbcConnection(_connectionString))
        //        {
        //            connection.Open();
        //            var command = new OdbcCommand("select * from GATEPASLIB.Gptran where TRANno=''", connection);

        //            using (var reader = command.ExecuteReader())
        //            {
        //                var results = new List<object>();
        //                while (reader.Read())
        //                {

        //                    results.Add(new
        //                    {
        //                        Column1 = reader["case_no"].ToString(),
        //                       // Column2 = reader["Column2"].ToString(),
        //                        //Add more columns as needed
        //                    });
        //                }
        //                return Ok(results);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}
        //

    }
    
}

