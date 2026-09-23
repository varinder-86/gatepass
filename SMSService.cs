using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using Gatepaswebapi.Model;
using System.Collections.Generic;
//using System.Data.SqlClient;

namespace Gatepaswebapi
{
    public class SMSService
    {
        private readonly IConfiguration _configuration;

        public SMSService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<string> SendSMS(string caseno, string serAmt, string balance, string memoNo, string msgid)
        {
            string msg = string.Empty;

            try
            {
                using (var con = new OdbcConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    var query = $"SELECT * FROM PATOOLSLIB.VWSMSCASES WHERE CASENO='{caseno}'";
                    var cmd = new OdbcCommand(query, con);
                    var da = new OdbcDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(dt.Rows[0]["PMOB"].ToString()))
                        {
                            var dtMsg = new DataTable();
                            var strsql = "SELECT * FROM PATOOLSLIB.TBLSMSMSGS WHERE MSGID='" + msgid + "'";
                            cmd = new OdbcCommand(strsql, con);
                            da = new OdbcDataAdapter(cmd);
                            da.Fill(dtMsg);

                            if (dtMsg.Rows.Count > 0)
                            {
                                msg = dtMsg.Rows[0]["MSGDESC"].ToString();
                                msg = msg.Replace("@1", serAmt);
                                msg = msg.Replace("@2", caseno);
                                msg = (decimal.Parse(balance) < 0) ? msg.Replace("@3", balance + " DR") : msg.Replace("@3", balance + " CR");
                                msg = msg.Replace("@4", memoNo);

                                // Insert the SMS log into a different database
                                var dbConnectionString = _configuration.GetConnectionString("SmsDbConnectionString");

                                //using (var dbConnection = new OleDbConnection(dbConnectionString))
                                //{
                                using (var dbConnection = new SqlConnection(dbConnectionString))
                                {
                                    var insertQuery = $@"INSERT INTO SMS_IO_LOG_MASTER(SMS_MOBILE, SMS_TEXT, STAMP, PDU_SMS, PRIORITY_INDEX, SMS_METHOD, SMS_IO_IND, TRANSACTION_ID, CASE_NO, MSGID) VALUES ('{dt.Rows[0]["PMOB"]}', '{msg}', GETDATE(), 0, '{dtMsg.Rows[0]["priority"]}', '2', '3', '{memoNo}', '{caseno}', '{msgid}')";

                                    var cmdSql = new SqlCommand(insertQuery, dbConnection);
                                    await dbConnection.OpenAsync();
                                    await cmdSql.ExecuteNonQueryAsync();
                                }

                                //string url = "https://intranet.tmc.gov.in/smsapi/api/sms/";

                            }
                        }
                    }
                }
                return msg;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while sending SMS: " + ex.Message);
            }
            // return msg;
        }

        //Birthday MSG

        // [HttpPost]
        //
        public async Task<List<Birthday>> BirthdayMsg()
        {
            var birthdayList = new List<Birthday>();

            try
            {
                using (var con = new OdbcConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    var query = @"SELECT EMPCODE, 
                                 TRIM(TITLE) || ' ' || TRIM(FNAME) || ' ' || TRIM(LNAME) AS FULLNAME, 
                                 TRIM(DESGSHDESC) AS DESIGNATION, 
                                 TRIM(DEPT_NAME) AS DEPT 
                          FROM EMPLIB.EMPDTLSLF 
                          WHERE VARCHAR_FORMAT(DOB, 'MM-DD') = VARCHAR_FORMAT(CURRENT_DATE, 'MM-DD') 
                            AND CESSYN = '4' 
                            AND ORGCODE IN(10, 7) and EMPCODE<>'115812'";

                    var cmd = new OdbcCommand(query, con);
                    var da = new OdbcDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            birthdayList.Add(new Birthday
                            {
                                EMPCODE = row["EMPCODE"].ToString(),
                                Name = row["FULLNAME"].ToString(),
                                DESIGNATION = row["DESIGNATION"].ToString(),
                                DEPT = row["DEPT"].ToString()
                            });
                        }
                    }
                }

                return birthdayList;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching birthday data: " + ex.Message);
            }
        }

     public async Task<List<Login>> Empdetail()
        {
            var empList = new List<Login>();

            try
            {
                using (var con = new OdbcConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await con.OpenAsync();

                    var query = @"SELECt empcode,tmhpwd,catcode FROM patoolslib.emplogpt 
                          WHERE CESSYN = '4' AND ORGCODE IN(10, 7) ";

                    var cmd = new OdbcCommand(query, con);
                    var da = new OdbcDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            empList.Add(new Login
                            {
                                Empcode = row["empcode"].ToString(),
                                Tmhpwd = row["tmhpwd"].ToString().Trim(),
                                catcode = row["catcode"].ToString().Trim()
                            });
                        }
                    }
                }

                return empList;
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching data: " + ex.Message);
            }
        }



    }

}
