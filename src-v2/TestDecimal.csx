using System;

var maxValue = 99.99m;
var quantity = 1_000_000;
var totalCost = maxValue * quantity;
var totalPrice = maxValue * quantity;

Console.WriteLine($"Max UnitCost/Price: {maxValue}");
Console.WriteLine($"Max Quantity: {quantity}");
Console.WriteLine($"TotalCost: {totalCost}");
Console.WriteLine($"TotalPrice: {totalPrice}");
Console.WriteLine($"Result type max: {decimal.MaxValue}");
