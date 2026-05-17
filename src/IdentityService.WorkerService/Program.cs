// FILE: Program.cs
// VERSION: 2.0.0
// MODULE: M-WORKER
// PURPOSE: Worker host entry point
// START_MODULE M_WORKER

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IdentityService.WorkerService;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerService(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
// END_BLOCK_MODULE
