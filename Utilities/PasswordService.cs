using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace Utilities;

public class PasswordService
{
    private readonly PasswordHasher<string> _passwordHasher = new();

    public string HashPassword(string username, string plainPassword)
    {
        return _passwordHasher.HashPassword(username, plainPassword);
    }

    public bool VerifyPassword(string username, string hashedPassword, string enteredPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(username, hashedPassword, enteredPassword);
        return result == PasswordVerificationResult.Success;
    }
}
