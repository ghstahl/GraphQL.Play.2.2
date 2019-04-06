﻿using B2BPublisher.Contracts;
using B2BPublisher.Contracts.Models;
using B2BPublisher.Extensions;
using B2BPublisher.Models;
using GraphQL.Types;
using P7Core.GraphQLCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace B2BPublisher.Mutation
{
    public class PublishStateMutation : IMutationFieldRegistration
    {
        private IB2BPlublisherStore _b2BPlublisherStore;

        public PublishStateMutation(IB2BPlublisherStore b2BPlublisherStore)
        {
            _b2BPlublisherStore = b2BPlublisherStore;
        }
         

        public void AddGraphTypeFields(MutationCore mutationCore)
        {
            mutationCore.FieldAsync<PublishStateResultType>(name: "publishState",
               description: null,
               arguments: new QueryArguments(new QueryArgument<PublishStateInputType> { Name = "input" }),
               resolve: async context =>
               {
                   try
                   {
                       // Authorization worked, but who is calling us
                       var graphQLUserContext = context.UserContext as GraphQLUserContext;
                       var principal = graphQLUserContext.HttpContextAccessor.HttpContext.User;

                       // Since this is a B2B api, there probably will not be a subject, hence no real user
                       // What we have here is an organization, so we have to find out the clientId, and the client_namespace

                       var authContext = principal.ToAuthContext();

                       var requestedFields = (from item in context.SubFields
                                              select item.Key).ToList();

                       var input = context.GetArgument<PublishStateModel>("input");

                       var result = await _b2BPlublisherStore.PublishStateAsync(
                           authContext, new Contracts.Models.RequestedFields
                           {
                               Fields = requestedFields
                           }, 
                           input);


                       return result;

                   }
                   catch (Exception e)
                   {

                   }
                   return false;
                   //                    return await Task.Run(() => { return ""; });

               },
               deprecationReason: null
           );
        }
    }
}
