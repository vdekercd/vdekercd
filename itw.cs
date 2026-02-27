using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Product> Products { get; set; }
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

[ApiController]
[Route("api")]
public class ShopController : ControllerBase
{
    private readonly AppDbContext _context;

    private static List<Product> _productCache = new();

    public ShopController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<List<User>> GetUsers(string email)
    {
        var query = $"SELECT * FROM Users WHERE Email = '{email}'";
        return await _context.Users
            .FromSqlRaw(query)
            .ToListAsync();
    }

    [HttpGet("orders")]
    public async Task<List<Order>> GetOrders()
    {
        var orders = _context.Orders.ToList();
        return orders;
    }

    [HttpPost("orders")]
    public void CreateOrder(Order order)
    {
        if (order.Amount <= 0)
            throw new Exception("Invalid amount");

        if (order.Amount > 10000)
        {
            order.Status = "VIP";
        }
        else if (order.Amount > 5000)
        {
            order.Status = "Priority";
        }
        else
        {
            order.Status = "Processing";
        }

        _context.Orders.Add(order);
        _context.SaveChanges();

        var user = _context.Users.FirstOrDefault(u => u.Id == order.CustomerId);

        if (user != null && user.Role == "Admin" && order.Amount < 9999)
        {
            SendEmail(user.Email, "Admin order created");
        }

        Log("Order created: " + order.Id);
    }

    [HttpGet("products")]
    public List<Product> GetProducts()
    {
        if (_productCache.Count == 0)
        {
            _productCache = _context.Products.ToList();
        }

        return _productCache;
    }

    [HttpPost("products")]
    public void AddProduct(Product product)
    {
        if (product.Price < 0.01m)
            return;

        _context.Products.Add(product);
        _context.SaveChanges();

        _productCache.Add(product);
    }

    private void Log(string message)
    {
        System.IO.File.AppendAllText("log.txt", message);
    }

    private void SendEmail(string email, string message)
    {
        Console.WriteLine($"Sending email to {email}: {message}");
    }
}
