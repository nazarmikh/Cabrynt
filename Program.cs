using Project.Data;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<AppDbContext>(options => 
options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

var app = builder.Build();


app.MapGet("/", () => "Hello World!");

app.Run();
