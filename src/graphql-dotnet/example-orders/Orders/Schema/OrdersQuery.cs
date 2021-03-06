﻿using GraphQL.Types;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Orders.Services;
using P7Core.GraphQLCore;

namespace Orders.Schema
{
    public class OrdersQuery : IQueryFieldRegistration
    {
        private ICustomerService _customers;
        private IOrderService _orders;

        public OrdersQuery(IOrderService orders, ICustomerService customers)
        {
            _orders = orders;
            _customers = customers;
        }

        public void AddGraphTypeFields(QueryCore queryCore)
        {
            queryCore.FieldAsync<ListGraphType<OrderType>>(
                "orders",
                resolve: async context => await _orders.GetOrdersAsync()
            );

            queryCore.FieldAsync<OrderType>(
                "orderById",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "orderId" }),
                resolve: async context => {
                    return await context.TryAsyncResolve(
                        async c => await _orders.GetOrderByIdAsync(c.GetArgument<String>("orderId"))
                    );
                }
            );

            queryCore.FieldAsync<ListGraphType<CustomerType>>(
                "customers",
                resolve: async context => await _customers.GetCustomersAsync()
            );
        }
    }
}
