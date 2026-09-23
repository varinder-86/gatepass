using System;
using System.Drawing;

namespace Gatepaswebapi.Model
{
    public class SMS
    {
        public string CaseNo { get; set; }
        public string SerAmt { get; set; }
        public string Balance { get; set; }
        public string MemoNo { get; set; }

        public string SMSMobile { get; set; }
        public string SMSText { get; set; }
        public DateTime Stamp { get; set; }
        public int PduSms { get; set; }
        public string PriorityIndex { get; set; }
        public string SmsMethod { get; set; }
        public string SmsIoInd { get; set; }
        public string TransactionId { get; set; }
        //public string CaseNo { get; set; }
        public string MsgId { get; set; }
    }

}
