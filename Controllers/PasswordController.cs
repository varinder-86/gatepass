



using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Microsoft.Extensions.Configuration;
using NuGet.Protocol.Plugins;
using Gatepaswebapi.Model;
using System.Threading;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;

namespace Gatepaswebapi.Controllers
{
    [ApiController]
    public class PasswordController: Controller
    {
        private readonly string _connectionString;
        public PasswordController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        
         if(string.IsNullOrWhiteSpace(_connectionString))
         {
               throw new InvalidOperationException("The DefaultConnection connection string is not configured");
         }
        
        }

[HttpPost]
[Route("api/password/verifyPass")]
    public async Task<IActionResult> PassVerify([FromBody] PasswordRequest req)
    { 
        string ccno=req.Ccno.ToString().Trim();
        string password=req.Passwd.ToString().Trim();

            // var user = new List<Userpass>();
            //  string islock=string.Empty;
            //   string attmpt=string.Empty;
           const  string sqlqry=@"select * from patoolslib.EMRLOGIN WHERE CCNO=? ";
            
                await using var con = new OdbcConnection(_connectionString);
                await con.OpenAsync();
                   
                 await using var cmd = new OdbcCommand(sqlqry,con);
                
                    cmd.Parameters.AddWithValue("@ccno", ccno.Trim());
                   // cmd.Parameters.AddWithValue("password",password.Trim());
                    
                        using var reader = await cmd.ExecuteReaderAsync();

                            if (!await reader.ReadAsync())
                            {
                                        return Ok("invalid userid and password");
                            }
                     var user = new Userpass
                        {
                            Username = reader["CCNO"].ToString(),
                            Password = reader["PASSWORD"].ToString(),
                            IsLocked = reader["IsLocked"]?.ToString() == "Y",
                            Attempts = reader["ATTEMPTS"] == DBNull.Value
                                ? 0
                                : Convert.ToInt32(reader["ATTEMPTS"]),
                            LockoutTM = reader["LockoutTM"] == DBNull.Value
                                ? null
                                : Convert.ToDateTime(reader["LockoutTM"])
                        };     

                           
                // ------------------------------------
                    // 1. CHECK ACCOUNT LOCK
                    // ------------------------------------

                    if (user.IsLocked)
                    {
                        if (user.LockoutTM.HasValue &&
                            user.LockoutTM.Value > DateTime.Now)
                        {
                            return Ok("Account is temporarily locked. Try again later.");
                        }

                        // Lockout expired
                        user.IsLocked = false;
                        user.Attempts = 0;

                        await SaveUser(user);
                    }

                    // ------------------------------------
                    // 2. VERIFY PASSWORD
                    // ------------------------------------

                    if (user.Password.Trim() != password)
                    {
                        user.Attempts++;

                        // 5 failed attempts
                        if (user.Attempts >= 5)
                        {
                            user.IsLocked = true;
                            user.LockoutTM = DateTime.Now.AddHours(1);

                            await SaveUser(user);

                            return Ok("Account is temporarily locked for 1 hour.");
                        }

                        await SaveUser(user);

                        return Ok("Invalid username or password.");
                    }

                    // ------------------------------------
                    // 3. SUCCESSFUL LOGIN
                    // ------------------------------------

                    user.Attempts = 0;
                    user.IsLocked = false;
                    user.LockoutTM = null;

                    await SaveUser(user);           
                                
               return Ok("Login successful.");
        }

private async Task SaveUser(Userpass user)
{
    const string sql = @"
        UPDATE patoolslib.EMRLOGIN
        SET IsLocked = ?,
            attempts = ?,
            LockoutTM = ?
        WHERE CCNO = ?";

    await using var con = new OdbcConnection(_connectionString);
    await con.OpenAsync();

    using var cmd = new OdbcCommand(sql, con);

    // ODBC parameters are positional
    cmd.Parameters.AddWithValue("@IsLocked", user.IsLocked ? "Y" : "N");
    cmd.Parameters.AddWithValue("@attempts", user.Attempts);

    if (user.LockoutTM.HasValue)
        cmd.Parameters.AddWithValue("@LockoutTM", user.LockoutTM.Value);
    else
        cmd.Parameters.AddWithValue("@LockoutTM", DBNull.Value);

    cmd.Parameters.AddWithValue("@CCNO", user.Username);

    await cmd.ExecuteNonQueryAsync();
}


    }

    public class PasswordRequest
    {
       public string Ccno { get; set; }
       public string Passwd { get; set; }
    }
}

    

  

