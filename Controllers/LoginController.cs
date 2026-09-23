using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;


namespace Gatepaswebapi.Controllers
{

    [ApiController]
  public class LoginController : Controller
  {
        private readonly SMSService _smsService;

        public LoginController(SMSService smsService)
        {
            _smsService = smsService;
        }

        [HttpGet]
        [Route("api/empdetail")]
    public async Task<IActionResult> Empdetail()
    {
        try
        {
             var result = await _smsService.Empdetail();

                if (result == null || result.Count == 0)
                {
                 return Ok(new { message = "No data found" });
                 }

                return Ok(result);
        //         return Ok(new
        //      {
           
        //    count = result.Count,
        //    users = result
        
        //      });
        }
             catch (Exception ex)
                 {
                     return StatusCode(500, new { error = ex.Message });
                }
     }


        public class LoginRequest
        {
            public string Empcode { get; set; }
            public string Tmhpwd { get; set; }
      

        }    
   }
    
}
