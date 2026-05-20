using System.CommandLine;
using Aqua.Admin.Cli.Commands;

var root = new RootCommand("aqua-admin — operational CLI for aqua-backend-ng");
root.AddCommand(new TenantCreateCommand().Build());
return await root.InvokeAsync(args);
