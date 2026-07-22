using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class Repository<T, TId> : IRepository<T, TId> where T : BaseEntity<TId>
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(TId id) => await _dbSet.FindAsync(id);
    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public virtual async Task<T> AddAsync(T entity) { await _dbSet.AddAsync(entity); return entity; }
    public virtual Task UpdateAsync(T entity) { _dbSet.Update(entity); return Task.CompletedTask; }
    public virtual Task DeleteAsync(T entity) { _dbSet.Remove(entity); return Task.CompletedTask; }
    public virtual async Task<bool> ExistsAsync(TId id) => await _dbSet.FindAsync(id) != null;
}
