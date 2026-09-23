using System;
using System.Security.AccessControl;
using Microsoft.VisualBasic;

public class Userpass
{
    public string Username { get; set; }
    public string Password { get; set; }

    public int Attempts { get; set; }

    public bool IsLocked { get; set; }
    
    public DateTime? LockoutTM { get; set; }

    public DateTime LASTDT { get; set; }
    public TimeSpan LASTTM { get; set; }
}