﻿// Copyright (c) Alex Maitland. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Chromium.AspNetCore.Bridge
{
    //Shorthand for Owin pipeline func
    using AppFunc = Func<IDictionary<string, object>, Task>;

    /// <summary>
    /// An <see cref="IServer"/> implementation that allows for using an OWIN pipeline
    /// for fulfilling requests/responses.
    /// </summary>
    public class OwinServer : IServer
    {
        private IFeatureCollection _features = new FeatureCollection();
        private Action<AppFunc> useOwin;

        IFeatureCollection IServer.Features
        {
            get { return _features; }
        }

        void IDisposable.Dispose()
        {
            
        }

        /// <summary>
        /// The <paramref name="action"/> will be called when the OWIN <see cref="AppFunc"/> 
        /// is ready for use. 
        /// </summary>
        /// <param name="action">called when OWIN <see cref="AppFunc"/> is ready for use.</param>
        public void UseOwin(Action<AppFunc> action)
        {
            useOwin = action;
        }

        Task IServer.StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            AppFunc appFunc = async env =>
            {
                var features = new OwinFeatureCollection(env);

                var context = application.CreateContext(features);

                try
                {
                    await application.ProcessRequestAsync(context);
                    await features.Get<IHttpResponseBodyFeature>().StartAsync();
                }
                catch (Exception ex)
                {
                    application.DisposeContext(context, ex);
                    throw;
                }

                application.DisposeContext(context, null);
            };

            useOwin?.Invoke(appFunc);

            return Task.CompletedTask;
        }

        Task IServer.StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}