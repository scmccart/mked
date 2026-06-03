var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("mked");
    config.AddCommand<Mked.Console.ViewCommand>("view")
          .WithDescription("View a Markdown file in a scrollable pager.");
});
return await app.RunAsync(args);
