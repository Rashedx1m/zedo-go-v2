using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private ICustomerRepository? _customers;
    private IDriverRepository? _drivers;
    private IDriverLocationRepository? _driverLocations;
    private IRequestRepository? _requests;
    private IPaymentRepository? _payments;
    private IRidePricingRepository? _ridePricings;

    public UnitOfWork(AppDbContext context) => _context = context;

    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ICustomerRepository Customers => _customers ??= new CustomerRepository(_context);
    public IDriverRepository Drivers => _drivers ??= new DriverRepository(_context);
    public IDriverLocationRepository DriverLocations => _driverLocations ??= new DriverLocationRepository(_context);
    public IRequestRepository Requests => _requests ??= new RequestRepository(_context);
    public IPaymentRepository Payments => _payments ??= new PaymentRepository(_context);
    public IRidePricingRepository RidePricings => _ridePricings ??= new RidePricingRepository(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync() => _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
