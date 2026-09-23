namespace Gatepaswebapi.Model
{
    public class Login
    {
        internal string username;
        internal string password;

        public string Empcode { get; internal set; }
        public string Tmhpwd { get; internal set; }
         public string catcode { get; internal set; }
    }
}