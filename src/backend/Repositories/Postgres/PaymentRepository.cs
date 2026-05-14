namespace Project.Repositories;

public interface IPaymentRepository
{
    Task AddPaymentAsync(Payment payment);
    Task<Payment?> GetPaymentByIdAsync(int id);
    Task SaveChangesAsync();
}

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddPaymentAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
    }

    public async Task<Payment?> GetPaymentByIdAsync(int id)
    {
        return await _context.Payments.FindAsync(id);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

}