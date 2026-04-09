namespace Project.Models;
using Project.Enums;
using System.Net.Mail;
using System.ComponentModel.DataAnnotations;

public class User
{
    public int Id {get;set;}
    private string _email = null!;
    public string Email
    {
        get
        {
            return _email;
        }
        set 
        {   
            if(IsValidEmail(value) == false)
            {
                throw new ArgumentException("Provide correct email");
            }
            _email = value; 
        }
    }

    private string _passwordHash = string.Empty;
    public required string PasswordHash
    {
        get { return _passwordHash; }
        set { _passwordHash = value; }
    }

    private Role _role;
    public Role Role
    {
        get
        {
            return _role;
        }
        set
        {
            _role = value;
        }
    }

    private DateTime _lastLogin;
    public DateTime LastLogin 
    {
        get
        {
            return _lastLogin;
        }
        set
        {
            _lastLogin = value;
        }
    }

    private DateTime _accountCreated;
    public DateTime AccountCreated
    {
        get
        {
            return _accountCreated;
        }
        set
        {
            _accountCreated = value;
        }
    }

    public bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }


}


