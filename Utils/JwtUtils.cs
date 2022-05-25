using System;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
namespace RecordingProxy.Utils
{
    public static class JwtUtils
    {
        const string plaintextKey = "MySecret12345678"; // At least 16 - length

        public static string generateToken()
        {
            string tokenStr = "";

            var tokenHandler = new JsonWebTokenHandler();

            var key = Encoding.ASCII.GetBytes(plaintextKey);

            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var payload = new JObject()
            {
                //{ JwtRegisteredClaimNames.Email, "anthony@gmail.com"},
                //{ JwtRegisteredClaimNames.GivenName, "Tony"},
                { JwtRegisteredClaimNames.Iss, "https://idb.com.vn" } //,
                //{ JwtRegisteredClaimNames.Nbf, "2017-03-18T18:33:37.080Z" },
                //{ JwtRegisteredClaimNames.Exp, "2023-03-17T18:33:37.080Z" }
            };

            var accessToken = tokenHandler.CreateToken(payload.ToString(), signingCredentials);

            tokenStr = accessToken;

            return tokenStr;
        }

        public static bool validateToken(string token)
        {
            if (token == null)
            {
                return false;
            }
            else
            {
                var key = Encoding.ASCII.GetBytes(plaintextKey);

                var tokenHandler = new JsonWebTokenHandler();

                var tokenValidationParameters = new TokenValidationParameters()
                {

                    ValidateIssuer = true,
                    ValidIssuer = "https://idb.com.vn",

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),

                    ValidateAudience = false
                };

                var tokenValidationResult = tokenHandler.ValidateToken(token, tokenValidationParameters);

                return tokenValidationResult.IsValid;
            }
        }
    }
}
