var normalized = (string?)null;
var orders = new[] { new { Status = new { Name = "Created" } } };
var query = orders.Where(x => x.Status.Name == normalized);
Console.WriteLine("Query created successfully");
var result = query.ToList();
Console.WriteLine("Result count: " + result.Count);
