var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("mked");
    config.AddCommand<Mked.Console.ViewCommand>("view")
          .WithDescription("View a Markdown file in a scrollable pager.");
    config.AddCommand<Mked.Console.EditCommand>("edit")
          .WithDescription("Edit a Markdown file in an interactive editor.");
});
return await app.RunAsync(args);
