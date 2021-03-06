﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Http;
using GraphQL.Instrumentation;
using GraphQL.Types;
using GraphQL.Validation;
using GraphQL.Validation.Complexity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using P7Core.GraphQLCore;
using P7Core.GraphQLCore.Validators;

namespace GraphQLCoreController
{


    [Produces("application/json")]
    [Route("api/v1/[controller]")]
    public class GraphQLController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ISession Session => _httpContextAccessor.HttpContext.Session;
        private ILogger Logger { get; set; }
        private IDocumentExecuter _executer { get; set; }
        private IDocumentWriter _writer { get; set; }
        private ISchema _schema { get; set; }
        private readonly IDictionary<string, string> _namedQueries;
        private List<IPluginValidationRule> _pluginValidationRules;

        public GraphQLController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<GraphQLController> logger,
            IDocumentExecuter executer,
            IDocumentWriter writer,
            ISchema schema,
            IEnumerable<IPluginValidationRule> pluginValidationRules)
        {
            _httpContextAccessor = httpContextAccessor;
            Logger = logger;
            _executer = executer;
            _writer = writer;
            _schema = schema;
            _namedQueries = new Dictionary<string, string>
            {
               
            };
            _pluginValidationRules = pluginValidationRules.ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetAsync(
            [FromQuery] string operationName,
            [FromQuery] string query,
            [FromQuery] string variables)
        {
            return await ProcessQueryAsync(new GraphQLQuery
            {
                Query = query,
                OperationName = operationName,
                Variables = variables == null ? new JObject() : JsonConvert.DeserializeObject<JObject>(variables)
            });
        }

        private async Task<IActionResult> ProcessQueryAsync(GraphQLQuery query)
        {
            try
            {
                var inputs = query.Variables.ToInputs();
                var queryToExecute = query.Query;
                var result = await _executer.ExecuteAsync(_ =>
                {
                    _.UserContext = new GraphQLUserContext(_httpContextAccessor);
                    _.Schema = _schema;
                    _.Query = queryToExecute;
                    _.OperationName = query.OperationName;
                    _.Inputs = inputs;
                    _.ComplexityConfiguration = new ComplexityConfiguration { MaxDepth = 15 };
                    _.FieldMiddleware.Use<InstrumentFieldsMiddleware>();
                    _.ValidationRules = _pluginValidationRules.Concat(DocumentValidator.CoreRules());

                }).ConfigureAwait(false);

                var httpResult = result.Errors?.Count > 0
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.OK;

                var json = _writer.Write(result);
                dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

                var objectResult = new ObjectResult(obj) { StatusCode = (int)httpResult };
                return objectResult;

            }
            catch (Exception e)
            {
                return MakeObjectResult("Unhandled GraphQL Exception", HttpStatusCode.InternalServerError);
            }
        }
        public static ObjectResult MakeObjectResult(string msg, HttpStatusCode httpStatusCode)
        {
            var errorResult = new ExecutionResult
            {
                Errors = new ExecutionErrors()
            };
            errorResult.Errors.Add(new ExecutionError(msg));
            return new ObjectResult(errorResult) { StatusCode = (int)httpStatusCode };
        }
        HttpStatusCode HttpStatusCodeFromResult(ExecutionResult result)
        {
            var httpStatusCode = HttpStatusCode.OK;
            if ((result?.Errors?.Count ?? 0) == 0)
            {
                return httpStatusCode;
            }

            httpStatusCode = HttpStatusCode.BadRequest;
            foreach (var error in result.Errors)
            {
                var exception = error.InnerException;
                while (exception != null)
                {
                    exception = exception.InnerException;
                }
            }
            return httpStatusCode;
        }
        [HttpPost]
        public async Task<IActionResult> PostAsync()
        {
            string body;
            using (var streamReader = new StreamReader(Request.Body))
            {
                body = await streamReader.ReadToEndAsync().ConfigureAwait(true);
            }

            var query = JsonConvert.DeserializeObject<GraphQLQuery>(body);
            return await ProcessQueryAsync(query);
        }
    }
}
