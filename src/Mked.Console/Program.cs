using Microsoft.Extensions.DependencyInjection;
using Mked.Console;

var services = new ServiceCollection();

// Infrastructure → Domain port bindings
services.AddSingleton<IFileReader, FileSystemReader>();
services.AddSingleton<IFileWriter, FileSystemWriter>();
services.AddSingleton<IInputReader, StdinInputReader>();

// Application use cases
services.AddTransient<OpenFileUseCase>();
services.AddTransient<SaveFileUseCase>();
services.AddTransient<StreamInputUseCase>();

// Presentation commands — explicit factories keep construction statically known (AOT-safe)
services.AddTransient(sp => new ViewCommand(
    sp.GetRequiredService<OpenFileUseCase>(),
    sp.GetRequiredService<StreamInputUseCase>()));
services.AddTransient(sp => new EditCommand(
    sp.GetRequiredService<OpenFileUseCase>(),
    sp.GetRequiredService<SaveFileUseCase>()));

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.SetApplicationName("mked");
    config.AddCommand<ViewCommand>("view")
          .WithDescription("View a Markdown file in a scrollable pager.");
    config.AddCommand<EditCommand>("edit")
          .WithDescription("Edit a Markdown file in an interactive editor.");
});
return await app.RunAsync(args);
