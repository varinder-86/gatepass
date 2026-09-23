using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;


namespace Gatepaswebapi.Controllers
{

    [ApiController]
  public class BirthdayController : Controller
  {
        private readonly SMSService _smsService;

        public BirthdayController(SMSService smsService)
        {
            _smsService = smsService;
        }

        [HttpGet]
        [Route("api/BirthMsg")]
    public async Task<IActionResult> BirthdayMsg()
    {
        try
        {
             var result = await _smsService.BirthdayMsg();

                if (result == null || result.Count == 0)
                {
                 return Ok(new { message = "No birthdays today." });
                 }

                return Ok(new
             {
           // message = "Birthday message data fetched successfully.",
            count = result.Count,
            data = result
             });
        }
             catch (Exception ex)
                 {
                     return StatusCode(500, new { error = ex.Message });
                }
     }


        public class BirthRequest
        {
            public string EMPCODE { get; set; }
            public string Name { get; set; }
            public string DESIGNATION { get; set; }
            public string DEPT { get; set; }


        }    
   }
    
}
