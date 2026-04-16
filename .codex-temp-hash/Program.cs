using System;
using System.Security.Cryptography;

var password = "Admin@123";
var iterations = 100000;
var salt = Convert.FromBase64String("c3BvcnRzYmljeWNsZXNfYWRtbg==");
var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
Console.WriteLine($"{iterations}.c3BvcnRzYmljeWNsZXNfYWRtbg==.{Convert.ToBase64String(hash)}");
