﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TokenExchange.Contracts;
using TokenExchange.Contracts.Models;
using TokenExchange.Contracts.Services;
using Utils.Models;

namespace BriarRabbitTokenExchange
{
    [Route("api/token_exchange")]
    [ApiController]
    public class BriarRabbitController : ControllerBase
    {
        private readonly ILogger<BriarRabbitController> _logger;


        public BriarRabbitController(
        ILogger<BriarRabbitController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        [Route("briar_rabbit/pass-through-handler")]
        public Task<List<TokenExchangeResponse>> PostPassThroughHandlerExchangeAsync(TokenExchangeRequest tokenExchangeRequest)
        {
            var result = new List<TokenExchangeResponse>(){
                new TokenExchangeResponse()
                {
                    access_token = $"briar_rabbit_access_token_{Guid.NewGuid().ToString()}",
                    refresh_token = $"briar_rabbit_refresh_token_{Guid.NewGuid().ToString()}",
                    expires_in = 1234,
                    token_type = $"briar_rabbit_Type",
                    authority =$"https://briar.rabbit.com/authority",
                    HttpHeaders = new List<HttpHeader>
                    {
                        new HttpHeader()
                        {
                            Name = "x-bunnyAuthScheme",
                            Value = "BunnyAuthority"
                        }
                    }
                }
            };
            return Task.FromResult(result);
        }
        [HttpPost]
        [Authorize]
        [Route("briar_rabbit/token-exchange-validator")]
        public Task<List<ExternalExchangeResourceOwnerTokenRequest>> PostTokenExchangeValidatorAsync(ExternalExchangeTokenExchangeHandler.TokenExchangeRequestPackage tokenExchangeRequest)
        {
            var result = new List<ExternalExchangeResourceOwnerTokenRequest>()
            {
                new ExternalExchangeResourceOwnerTokenRequest()
                {
                    AccessTokenLifetime = 3600,
                    ArbitraryClaims = new Dictionary<string, List<string>>()
                    {
                        { "role", new List<string>{ "bigFluffy","fluffyAdmin"} }
                    },
                    Scope = "offline_access graphQLPlay",
                    Subject = "MrRabbit",
                    HttpHeaders = new List<HttpHeader>()
                    {
                        new HttpHeader()
                        {
                            Name = "x-bunnyAuthScheme",
                            Value = "BunnyAuthority"
                        }
                    }
                }
            };
            return Task.FromResult(result);
        }
    }
}
