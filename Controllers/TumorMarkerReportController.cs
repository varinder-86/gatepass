using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using NuGet.Protocol.Plugins;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure.Interception;
using System.Data.Odbc;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Transactions;

namespace Gatepaswebapi.Controllers
{
    [ApiController]
    public class TumorMarkerReportController : ControllerBase
    {
        private readonly string _connectionString;

        public TumorMarkerReportController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("The DefaultConnection connection string is not configured.");
            }
        }

        [HttpGet]
        [Route("api/tm/dept")]
        public async Task<IActionResult> GetDepartments()
        {
            const string sql = @"SELECT dept_name, dept_code
                                 FROM PATOOLSLIB.ma106
                                 WHERE DEPT_CODE IN ('FZZ', 'FEC', 'GZZ', 'CAZ', 'CBZ', 'UZZ', 'NZZ', 'LZZ')
                                 ORDER BY dept_name";

            try
            {
                await using var connection = new OdbcConnection(_connectionString);
                await connection.OpenAsync();
                var departments = await ReadRowsAsync(connection, sql);
                //return Ok(new {departments  });
                return Ok(departments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unable to retrieve departments.", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/tm/modality")]
        public async Task<IActionResult> GetModalities([FromQuery] string departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                return BadRequest(new { error = "departmentCode is required." });
            }

            const string sql = @"SELECT modlitydes, modalitycd
                                 FROM RISLIB.rmodality
                                 WHERE DEPT_CODE = ?
                                 ORDER BY modlitydes";

            try
            {
                await using var connection = new OdbcConnection(_connectionString);
                await connection.OpenAsync();
                var modalities = await ReadRowsAsync(connection, sql, departmentCode);
                return Ok( modalities );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unable to retrieve modalities.", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/tumor-marker-report")]
        [Route("api/tumar-marker-report")]
        public async Task<IActionResult> GetReport([FromQuery] string caseNo, [FromQuery] string modalityCode, [FromQuery] string REQNSTAT)
        {
            if (string.IsNullOrWhiteSpace(caseNo) || string.IsNullOrWhiteSpace(modalityCode))
            {
                return BadRequest(new { error = "caseNo and modalityCode AND reqstat are required." });
            }

            const string patientSql = "SELECT * FROM PATOOLSLIB.ta101 WHERE case_no = ?";
            const string bedSql = "SELECT * FROM PATOOLSLIB.ma104 WHERE case_no = ?";
            const string requisitionSql = @"SELECT * FROM RISLIB.RREQUISITN
                                            WHERE caseno = ?  AND MODALITYCD = ? AND REQNSTAT = ?
                                            ORDER BY REQNDT DESC";
            const string referringDoctorSql = @"SELECT * FROM RISLIB.RREQUISITN
                                                WHERE CASENO = ? AND REQNNO = ?
                                                ORDER BY REQNDT DESC";
            const string multiProcedureSql = "SELECT pro_code FROM DISLIB.DMULPROCE WHERE REQNO = ?";
            const string procedureSql = "SELECT * FROM DISLIB.DMPROCEDUR WHERE REQNO = ?";
            const string procedureSql1 = "SELECT * FROM DISLIB.DREQUISITN WHERE REQNO = ?";
            const string electroSql1 = "SELECT * FROM DISLIB.DIMBIOCHEL WHERE REQNO = ?";

            try
            {
                await using var connection = new OdbcConnection(_connectionString);
                await connection.OpenAsync();

                var patients = await ReadRowsAsync(connection, patientSql, caseNo);
                var beds = await ReadRowsAsync(connection, bedSql, caseNo);
                var requisitions = await ReadRowsAsync(connection, requisitionSql, caseNo, modalityCode, REQNSTAT);
                var reportRequisitions = new List<object>();

                foreach (var requisition in requisitions)
                {
                    requisition.TryGetValue("REQNNO", out var requisitionNumber);
                    var requestNumber = requisitionNumber?.ToString();

                    var referringDoctor = string.IsNullOrWhiteSpace(requestNumber)
                        ? new List<Dictionary<string, object>>()
                        : await ReadRowsAsync(connection, referringDoctorSql, caseNo, requestNumber);
                    var multiProcedures = string.IsNullOrWhiteSpace(requestNumber)
                        ? new List<Dictionary<string, object>>()
                        : await ReadRowsAsync(connection, multiProcedureSql, requestNumber);
                    var procedures = string.IsNullOrWhiteSpace(requestNumber)
                        ? new List<Dictionary<string, object>>()
                        : await ReadRowsAsync(connection, procedureSql, requestNumber);
                    var labno = string.IsNullOrWhiteSpace(requestNumber)
                        ? new List<Dictionary<string, object>>()
                        : await ReadRowsAsync(connection, procedureSql1, requestNumber);
                    var electro = string.IsNullOrWhiteSpace(requestNumber)
                        ? new List<Dictionary<string, object>>()
                        : await ReadRowsAsync(connection, electroSql1, requestNumber);

                    reportRequisitions.Add(new
                    {
                        requisition,
                        referringDoctor,
                        multiProcedures,
                        procedures,
                        labno,
                        electro

                    });
                }

                return Ok(new
                {
                    caseNo,
                    modalityCode,
                   REQNSTAT,
                    patient = patients,
                    bedWard = beds,
                    requisitionCount = reportRequisitions.Count,
                    requisitions = reportRequisitions
                });
              //  return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unable to generate the tumor marker report.", detail = ex.Message });
            }
        }

        private static async Task<List<Dictionary<string, object>>> ReadRowsAsync(OdbcConnection connection, string sql, params object[] parameters)
        {
            await using var command = new OdbcCommand(sql, connection);
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue("@parameter", parameter ?? DBNull.Value);
            }

            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[reader.GetName(index)] = reader.IsDBNull(index) ? null : reader.GetValue(index);
                }

                rows.Add(row);
            }

            return rows;
        }


        [HttpPost]
        [Route("api/tumar-marker-report/save")]
    public async Task<IActionResult> SaveTmreport( [FromBody] List<MarkerDto> markers )
    {       
        
           // Example status update query
            const string updateStatusSql = @"UPDATE RISLIB.RREQUISITN  SET REQNSTAT = 'T' WHERE REQNO = ?";

            if(markers==null || markers.Count == 0)
            {
                BadRequest(new { success= false,message="no data received"});
            }

            const string sqlqry= @"INSERT INTO DISLIB.DMPROCEDUR
                    (
                        REQNO,
                        PRO_CODE,
                        OBSERVALUE,
                        OBSER_TYPE,
                        UOM,
                        NRANGE,
                        PVALUE,
                        SOURCE,
                        REMARKS
                    )
                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)";
        try
        {
         await using var con= new OdbcConnection(_connectionString);
          await con.OpenAsync();

          await using var transaction= await con.BeginTransactionAsync();
            try
            {
              
             
              

               foreach (var marker in markers)
                {
                    await using var command= new OdbcCommand(sqlqry,con, (OdbcTransaction)transaction);

                    command.Parameters.AddWithValue("@REQNO", marker.REQNO ?? "");
                    command.Parameters.AddWithValue("@PRO_CODE", marker.PRO_CODE ?? "");
                    command.Parameters.AddWithValue("@OBSERVALUE", marker.OBSERVALUE ?? "");
                    command.Parameters.AddWithValue("@OBSER_TYPE", marker.OBSER_TYPE ?? "");
                    command.Parameters.AddWithValue("@UOM", marker.UOM ?? "");
                    command.Parameters.AddWithValue("@NRANGE", marker.NRANGE ?? "");
                    command.Parameters.AddWithValue("@PVALUE", marker.PVALUE ?? "");
                    command.Parameters.AddWithValue("@SOURCE", marker.SOURCE ?? "");
                    command.Parameters.AddWithValue("@REMARKS", marker.REMARKS ?? "");
                      // VERY IMPORTANT
                    await command.ExecuteNonQueryAsync();
                }

                ////-----------------update query-------
                string reqno= markers[0].REQNO ?? "";
                await using var statuscomm=new OdbcCommand( updateStatusSql, con, (OdbcTransaction)transaction);

                statuscomm.Parameters.AddWithValue("@REQNSTAT", "T");
                statuscomm.Parameters.AddWithValue("@REQNO",reqno ?? "");
            
                 await statuscomm.ExecuteNonQueryAsync();

                //////////----------------
               await transaction.CommitAsync();

             return Ok(new
                {
                    success = true,
                    message = "Markers saved successfully",
                    count = markers.Count
                });
              
            }
          
            catch (Exception ex)
                {
                    await transaction.RollbackAsync();  
                    return StatusCode(500, new { success=false, message="save falied, and all changes rollback", error=ex.Message}) ;             

                }
        }
         catch(Exception ex)
            {
                return StatusCode(500, new { success= false, Message=" database connection falied" , error=ex.Message});
                
            }
    }

[HttpPut]
         [Route("api/tumar-marker-report/modify")]
        public async Task<IActionResult> ModifyTmreport( [FromBody] List<MarkerDto> markers )
        {
            
            const string sqlqry= @"update DISLIB.DMPROCEDUR set                   
                        OBSERVALUE=?,
                        OBSER_TYPE=?,
                        UOM=?,
                        NRANGE=?,
                        PVALUE=?,
                        SOURCE=?,
                        REMARKS=?
                 where REQNO =?";
        
            try
            {
              await using var con= new OdbcConnection(_connectionString);
              await con.OpenAsync();
               foreach (var marker in markers)
                {
                    await using var command= new OdbcCommand(sqlqry,con);

                  command.Parameters.AddWithValue("@OBSERVALUE", (marker.OBSERVALUE ?? "").Trim());
                    command.Parameters.AddWithValue("@OBSER_TYPE", (marker.OBSER_TYPE ?? "").Trim());
                    command.Parameters.AddWithValue("@UOM", (marker.UOM ?? "").Trim());
                    command.Parameters.AddWithValue("@NRANGE", (marker.NRANGE ?? "").Trim());
                    command.Parameters.AddWithValue("@PVALUE", (marker.PVALUE ?? "").Trim());
                    command.Parameters.AddWithValue("@SOURCE", (marker.SOURCE ?? "").Trim());
                    command.Parameters.AddWithValue("@REMARKS", (marker.REMARKS ?? "").Trim());

                    // REQNO is the last ? in your SQL
                    command.Parameters.AddWithValue("@REQNO", (marker.REQNO ?? "").Trim());
                                        // VERY IMPORTANT
                                await command.ExecuteNonQueryAsync();
                }
            return Ok(new
        {
            success = true,
            message = "Markers modify successfully",
            count = markers.Count
        });
            }
            catch (System.Exception)
            {
                
                throw;
            }
       // return Ok();
        }

    
        [HttpPost]
        [Route("api/tumar-marker-report/saveElectro")]
        public async Task<IActionResult> SaveElectroReport([FromBody] List<ElectroDto> electroReports)
        {
            const string sqlqry = @"INSERT INTO DISLIB.DIMBIOCHEL
                    (
                    REQNO,
                    ELEPHPATT,
                    MONBAND,
                    LOCNBAND,
                    CONMBAND,
                    IGA,
                    IGM,
                    IGG,
                    REMMBAND,
                    UIMELPHO,
                    BJP,
                    KALA,
                    TMONPIMFIX,
                    KAPPA,
                    LAMBDA)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            try
            {
                await using var con = new OdbcConnection(_connectionString);
                await con.OpenAsync();
                foreach (var report in electroReports)
                {
                    await using var command = new OdbcCommand(sqlqry, con);
                    command.Parameters.AddWithValue("@REQNO", report.REQNO ?? "");
                    command.Parameters.AddWithValue("@ELEPHPATT", report.ELEPHPATT  ?? "");
                    command.Parameters.AddWithValue("@MONBAND", report.MONBAND ?? "");
                    command.Parameters.AddWithValue("@LOCNBAND", report.LOCNBAND ?? "");     
                    command.Parameters.AddWithValue("@CONMBAND", report.CONMBAND ?? "");
                    command.Parameters.AddWithValue("@IGA", report.IGG?? "");
                    command.Parameters.AddWithValue("@IGM", report.IGM ?? "");
                    command.Parameters.AddWithValue("@IGG", report.IGG ?? ""); 
                    command.Parameters.AddWithValue("@REMMBAND", report.REMMBAND?? "");
                    command.Parameters.AddWithValue("@UIMELPHO", report.UIMELPHO ?? "");
                    command.Parameters.AddWithValue("@BJP", report.BJP ?? "");  
                    command.Parameters.AddWithValue("@KALA", report.KALA ?? "");
                    command.Parameters.AddWithValue("@TMONPIMFIX", report.TMONPIMFIX ?? "");
                    command.Parameters.AddWithValue("@KAPPA", report.KAPPA?? "");
                    command.Parameters.AddWithValue("@LAMBDA", report.LAMBDA ?? "");   

                    // Add other parameters as needed

                    await command.ExecuteNonQueryAsync();
                }   
                return Ok();
            }
                    catch (Exception ex)
                    {
                        return StatusCode(500, new { error = "Unable to save electro reports.", detail = ex.Message });
                    }
        }
        [HttpPut]
        [Route("api/tumar-marker-report/modifyElectro")]
        public async Task<IActionResult> ModifyElectroReport([FromBody] List<ElectroDto> electroReports)
        {
            const string sqlqry = @"UPDATE DISLIB.DIMBIOCHEL SET
                    ELEPHPATT = ?,
                    MONBAND = ?,
                    LOCNBAND = ?,
                    CONMBAND = ?,
                    IGA = ?,
                    IGM = ?,
                    IGG = ?,
                    REMMBAND = ?,
                    UIMELPHO = ?,
                    BJP = ?,
                    KALA = ?,
                    TMONPIMFIX = ?,
                    KAPPA = ?,
                    LAMBDA = ?
                WHERE REQNO = ?";

            try
            {
                await using var con = new OdbcConnection(_connectionString);
                await con.OpenAsync();
                foreach (var report in electroReports)
                {
                    await using var command = new OdbcCommand(sqlqry, con);
                    command.Parameters.AddWithValue("@ELEPHPATT", report.ELEPHPATT  ?? "");
                    command.Parameters.AddWithValue("@MONBAND", report.MONBAND ?? "");
                    command.Parameters.AddWithValue("@LOCNBAND", report.LOCNBAND ?? "");     
                    command.Parameters.AddWithValue("@CONMBAND", report.CONMBAND ?? "");
                    command.Parameters.AddWithValue("@IGA", report.IGA?? "");
                    command.Parameters.AddWithValue("@IGM", report.IGM ?? "");
                    command.Parameters.AddWithValue("@IGG", report.IGG ?? ""); 
                    command.Parameters.AddWithValue("@REMMBAND", report.REMMBAND?? "");
                    command.Parameters.AddWithValue("@UIMELPHO", report.UIMELPHO ?? "");
                    command.Parameters.AddWithValue("@BJP", report.BJP ?? "");  
                    command.Parameters.AddWithValue("@KALA", report.KALA ?? "");
                    command.Parameters.AddWithValue("@TMONPIMFIX", report.TMONPIMFIX ?? "");
                    command.Parameters.AddWithValue("@KAPPA", report.KAPPA?? "");
                    command.Parameters.AddWithValue("@LAMBDA", report.LAMBDA ?? "");   
                 
                    // Add other parameters as needed

                    // REQNO is the last parameter for the WHERE clause
                    command.Parameters.AddWithValue("@REQNO", report.REQNO ?? "");
                    await command.ExecuteNonQueryAsync();
                }
                   return Ok(new{
                    success = true,
                    message = "electro modify successfully",
                    count = electroReports.Count
                });
        
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Unable to modify electro reports.", detail = ex.Message });
            }
        }

            [HttpPut]
            [Route("api/tumar-marker-report/commit")]
        public async Task<IActionResult> CommitReq([FromBody] List<RequisitionDto> requisition)
        {
            if(requisition == null)
            {
                BadRequest(new{ message="no requisition find"});
            }

            const string updateStatusSql = @"UPDATE RISLIB.RREQUISITN  SET REQNSTAT = 'C',COMMITFLAG='Y',
            ENTERBY=?,PREPORTBY=?, PREPORTDT=?,FREPORTBY=?,FREPORTDT=?  WHERE REQNO = ?";

            try
            {
                await using var con = new OdbcConnection(_connectionString);
                await con.OpenAsync();
                await using var com= new OdbcCommand(updateStatusSql,con);
                try
                {
                    foreach(var req in requisition)
                    {
                    com.Parameters.AddWithValue("@REQNSTAT", req.REQNSTAT ?? "" );
                    com.Parameters.AddWithValue("@COMMITFLAG",req.COMMITFLAG ?? "");
                    com.Parameters.AddWithValue("@REQNSTAT", req.REQNSTAT );
                    com.Parameters.AddWithValue("@PREPORTBY",req.PREPORTBY ?? "");
                    com.Parameters.AddWithValue("@PREPORTDT", req.PREPORTDT );
                    com.Parameters.AddWithValue("@FREPORTBY",req.FREPORTBY ?? "");
                    com.Parameters.AddWithValue("@FREPORTDT",req.FREPORTDT ?? "");

                    com.Parameters.AddWithValue("@REQNNO",req.REQNNO);
                      await com.ExecuteNonQueryAsync();
                    }
                   return Ok(new{
                    success = true,
                    message = "COMMITED successfully",
                  
                });
                }
                  
                 catch(Exception ex)
                    {
                          return StatusCode(500, new { error = "Unable to  commit.", detail = ex.Message });   
                    }
            }
              catch(Exception ex)
                    {
                           return StatusCode(500, new { error = "Unable to commit", detail = ex.Message });  
                    }

        }


    }
}
