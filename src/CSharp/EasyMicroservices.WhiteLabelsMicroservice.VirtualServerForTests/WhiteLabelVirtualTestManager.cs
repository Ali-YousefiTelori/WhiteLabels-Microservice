﻿using EasyMicroservices.Laboratory.Constants;
using EasyMicroservices.Laboratory.Engine;
using EasyMicroservices.Laboratory.Engine.Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace EasyMicroservices.WhiteLabelsMicroservice.VirtualServerForTests
{
    public class WhiteLabelVirtualTestManager
    {
        static ConcurrentDictionary<int, ResourceManager> InitializedPorts = new ConcurrentDictionary<int, ResourceManager>();
        public int CurrentPortNumber { get; set; }

        public async Task<bool> OnInitialize(int portNumber, BaseHandler handler = null)
        {
            CurrentPortNumber = portNumber;
            if (InitializedPorts.ContainsKey(portNumber))
                return false;
            var resource = InitializedPorts[portNumber] = new ResourceManager();
            handler ??= BaseHandler.CreateOSHandler(resource);
            await handler.Start(portNumber);
            return true;
        }

        public void AppendService(int port, string request, string body)
        {
            if (InitializedPorts.TryGetValue(port, out ResourceManager resource))
                resource.Append(request, body);
            else
                throw new KeyNotFoundException(port.ToString());
        }

        public async Task<string> GetLastResponse(int port)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(RequestTypeHeaderConstants.RequestTypeHeader, RequestTypeHeaderConstants.GiveMeLastFullRequestHeaderValue);
            var httpResponse = await httpClient.GetAsync($"http://localhost:{port}");
            return await httpResponse.Content.ReadAsStringAsync();
        }

        public async Task<T> HandleResponse<T>(int port, Func<Task<T>> task)
        {
            try
            {
                return await task();
            }
            catch (Exception ex)
            {
                try
                {
                    var response = await GetLastResponse(port);
                    throw new Exception(response, ex);
                }
                catch
                {

                }
                throw;
            }
        }
    }
}
