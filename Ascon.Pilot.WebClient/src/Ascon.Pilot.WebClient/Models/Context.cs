﻿using Ascon.Pilot.Core;
using Ascon.Pilot.Server.Api;
using Ascon.Pilot.Server.Api.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace Ascon.Pilot.WebClient.Models
{
    public interface IContext : IDisposable
    {
        void Build(HttpContext http);
        IRepository Repository { get; }
        DDatabaseInfo Connect(Credentials credentials);
        bool IsInitialized { get; }
    }

    class Context : IContext
    {
        private Repository _repository;
        private HttpPilotClient _client;
        private readonly ServerCallback _serverCallback;

        public Context()
        {
            _serverCallback = new ServerCallback();
        }

        public DDatabaseInfo Connect(Credentials credentials)
        {
            _client = new HttpPilotClient(ApplicationConst.PilotServerUrl);
            _client.Connect();
            var serverApi = _client.GetServerApi(_serverCallback);
            var authApi = _client.GetAuthenticationApi();
            authApi.Login(credentials.DatabaseName, credentials.Username, credentials.ProtectedPassword, credentials.UseWindowsAuth, 100);
            var dbInfo = serverApi.OpenDatabase();
            _repository = new Repository(serverApi, _serverCallback);
            _repository.Initialize(credentials.Username);
            IsInitialized = true;
            return dbInfo;
        }

        public void Build(HttpContext context)
        {
            if (IsInitialized)
                return;

            var dbName = context.User.FindFirstValue(ClaimTypes.Surname);
            var login = context.User.FindFirstValue(ClaimTypes.Name);
            var protectedPassword = context.User.FindFirstValue(ClaimTypes.UserData);
            var clientIdString = context.User.FindFirstValue(ClaimTypes.Sid);
            var creds = Credentials.GetConnectionCredentials(dbName, login, protectedPassword.DecryptAes());
            Connect(creds);
        }

        public IRepository Repository
        {
            get { return _repository; }
        }

        public bool IsInitialized
        {
            get; private set;
        }

        public void Dispose()
        {
            _client?.Disconnect();
            _client?.Dispose();
        }
    }
}
