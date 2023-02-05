using Drk.AspNetCore.MinimalApiKit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseFileServer(new FileServerOptions
{
    RequestPath = "",
    FileProvider = new Microsoft.Extensions.FileProviders
                    .ManifestEmbeddedFileProvider(typeof(Program).Assembly, "ui")
});

app.MapPost("/aes", (HttpContext ctx) => {
    var mode = ctx.Request.Form["mode"];
    var key = ctx.Request.Form["key"];
    var data = ctx.Request.Form["data"];
    var encMode = mode != "decrypt";
    var resProp = encMode ? "Encrypted" : "Plain";
    var message = string.Empty;
    var result = string.Empty;
    try
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(data))
            throw new ArgumentException("parameter missing");
        result = encMode ? CodecNetFx.AesEncrypt(key, data) : CodecNetFx.AesDecrypt(key, data);
    }
    catch (Exception ex) { message = ex.Message; }
    return Results.Content(@$"<script>
    parent.vm.{resProp} = {JsonSerializer.Serialize(result)};
    parent.vm.Message = {JsonSerializer.Serialize(message)};
    parent.vm.Highlight('{resProp}');
    </script>", "text/html");
});

app.RunAsDesktopTool();

public class CodecNetFx
{
    private class AesKeyIV
    {
        public Byte[] Key = new Byte[16], IV = new Byte[16];
        public AesKeyIV(string strKey)
        {
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.ASCII.GetBytes(strKey));
            Array.Copy(hash, 0, Key, 0, 16);
            Array.Copy(hash, 16, IV, 0, 16);
        }
    }
    public static string AesEncrypt(string key, string rawString)
    {
        var keyIv = new AesKeyIV(key);
        var aes = Aes.Create();
        aes.Key = keyIv.Key;
        aes.IV = keyIv.IV;
        var rawData = Encoding.UTF8.GetBytes(rawString);
        using (var ms = new MemoryStream())
        {
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write))
            {
                cs.Write(rawData, 0, rawData.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string AesDecrypt(string key, string encString)
    {
        var keyIv = new AesKeyIV(key);
        var aes = Aes.Create();
        aes.Key = keyIv.Key;
        aes.IV = keyIv.IV;
        var encData = Convert.FromBase64String(encString);
        byte[] buffer = new byte[8192];
        using (var ms = new MemoryStream(encData))
        {
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(aes.Key, aes.IV), CryptoStreamMode.Read))
            {
                using (var sr = new StreamReader(cs))
                {
                    using (var dec = new MemoryStream())
                    {
                        cs.CopyTo(dec);
                        return Encoding.UTF8.GetString(dec.ToArray());
                    }
                }
            }
        }
    }
}
