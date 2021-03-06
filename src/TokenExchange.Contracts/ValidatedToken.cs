﻿using System.Security.Claims;

namespace TokenExchange.Contracts
{
    public class ValidatedToken: TokenWithScheme
    {
        public ClaimsPrincipal Principal { get; set; }
    }
    public class TokenWithScheme
    {
        /// <summary>
        /// Any accepted token [id_token, etc] 
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// The type of token, i.e oidc
        /// </summary>
        public string TokenScheme { get; set; }
    }
}