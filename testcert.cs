using System;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;

class Program
{
    static void Main()
    {
        string connStr = "Server=WPT-DEVSERVER;Database=WPTcomercial;User Id=sa;Password=zeus;TrustServerCertificate=True;";
        try
        {
            using (var conn = new SqlConnection(connStr))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT NucleoCertificadoDigital, NucleoPasswordDigital FROM Nucleo WHERE NucleoRNC = '131215912'", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        byte[] bytes = (byte[])reader[0];
                        string pass = reader[1].ToString();
                        Console.WriteLine("Leyendo bytes: " + bytes.Length + ", Pass: " + pass);
                        
                        var cert = new X509Certificate2(bytes, pass, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.MachineKeySet);
                        Console.WriteLine("Exito! Subject: " + cert.Subject);
                    }
                }
            }
        }
        catch(Exception e)
        {
            Console.WriteLine("ERROR REAL: " + e.Message);
        }
    }
}
