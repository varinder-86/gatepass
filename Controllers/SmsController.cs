using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace Gatepaswebapi.Controllers
{
  
    [ApiController]
    public class SmsController : Controller
    {
        private readonly SMSService _smsService;

        public SmsController(SMSService smsService)
        {
            _smsService = smsService;
        }

        [HttpGet]
        [Route("api/Sms")]
        public async Task<IActionResult> SendSMS(string caseno, string SerAmt, string Balance, string MemoNo, string msgid)

        {
            if (string.IsNullOrEmpty(caseno) || string.IsNullOrEmpty(SerAmt) || string.IsNullOrEmpty(Balance) || string.IsNullOrEmpty(MemoNo) || string.IsNullOrEmpty(msgid))
            {
                return BadRequest("Invalid data.");
            }
            try
            {
               var result = await _smsService.SendSMS(caseno, SerAmt, Balance, MemoNo, msgid);
               
                return Ok(new { message = "SMS sent successfully", result });
                // return Ok();
            }

            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class SMSRequest
    {
        public string caseno { get; set; }
        public string SerAmt { get; set; }
        public string Balance { get; set; }
        public string MemoNo { get; set; }
        public string msgid { get; set; }

    }

}

